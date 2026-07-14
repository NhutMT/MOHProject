using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Application.Features.AdditionOfRider;

// Central orchestrator: UW makes a decision from a given entry substatus.
// Dispatches to the matching IEntryPointHandler, applies the directive,
// and — unless the directive overrides — hands off to IRemainingPlansEvaluator
// for the final substatus + main letter.
public sealed record UwDecisionCommand(long PolicyId, UwDecision Decision, string ActorUserId);

public sealed class UwDecisionCommandHandler
{
    public const string AuditEventType = "UwDecisionApplied";

    private readonly IPolicyRepository _policies;
    private readonly IEntryPointHandlerRegistry _handlers;
    private readonly IRemainingPlansEvaluator _evaluator;
    private readonly ILetterGenerator _letters;
    private readonly IAuditTrailWriter _audit;
    private readonly IUnitOfWork _uow;
    private readonly IReminderScheduler _reminders;

    public UwDecisionCommandHandler(
        IPolicyRepository policies,
        IEntryPointHandlerRegistry handlers,
        IRemainingPlansEvaluator evaluator,
        ILetterGenerator letters,
        IAuditTrailWriter audit,
        IUnitOfWork uow,
        IReminderScheduler reminders)
    {
        _policies = policies;
        _handlers = handlers;
        _evaluator = evaluator;
        _letters = letters;
        _audit = audit;
        _uow = uow;
        _reminders = reminders;
    }

    public Task HandleAsync(UwDecisionCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return _uow.ExecuteInTransactionAsync(HandleInner, ct);

        async Task HandleInner(CancellationToken innerCt)
        {

        var policy = await _policies.GetByIdAsync(command.PolicyId, innerCt)
            ?? throw new InvalidOperationException($"Policy {command.PolicyId} not found.");

        if (policy.UWState is null)
            throw new InvalidOperationException($"Policy {policy.Id} has no UWState.");

        var entrySubstatus = policy.Substatus;
        var handler = _handlers.ResolveFor(entrySubstatus);
        var directive = handler.Handle(policy, command.Decision);

        // 1. Pre-evaluator UW state modifications (e.g. 1.5.3 Sub clears AcceptCloa).
        policy.UWState = directive.UWStateBeforeEvaluator;

        // 2. Decision-specific letters (Medical Evidence / Decline / Postpone / NTU).
        foreach (var letterType in directive.DecisionSpecificLetters)
            await _letters.GenerateAsync(policy.Id, letterType, innerCt);

        // 3. IP-record side effects — Phase 4 will wire an ICpfIpFileService port.
        // For now these directives are surfaced only in the audit payload.
        // 4. SkipBasePremiumRecalc — Phase 4 premium recalculator will honor it.

        // 5. Substatus decision path.
        PolicySubstatus finalSubstatus;
        LetterType? mainLetter = null;
        RiskComposition? composition = null;

        if (directive.OverrideNextSubstatus is { } overriden)
        {
            finalSubstatus = overriden;
        }
        else
        {
            var context = BuildContext(policy);
            var result = _evaluator.EvaluateAfterAction(policy, context);
            finalSubstatus = result.NextSubstatus;
            policy.UWState = result.UpdatedUWState;
            mainLetter = result.LetterToGenerate;
            composition = result.Composition;

            if (mainLetter is { } main)
            {
                var mainLetterRow = await _letters.GenerateAsync(policy.Id, main, innerCt);

                // Schedule reminders for main LOA/CLOA if there's an outstanding
                // reason (shortfall > 0 for LOA; shortfall OR unaccepted CLOA
                // reply for CLOA). Doc §FR-AOR-040 (source lines 339-372).
                if (ShouldScheduleReminders(main, policy, updatedUwState: policy.UWState))
                    await _reminders.ScheduleFromAsync(mainLetterRow, innerCt);
            }
        }

        policy.Substatus = finalSubstatus;

        // Substatus → PendingUwAps stops ALL reminders per doc lines 363-365.
        if (finalSubstatus == PolicySubstatus.PendingUwAps)
            await _reminders.CancelAllForPolicyAsync(policy.Id, innerCt);

        await _policies.SaveAsync(policy, innerCt);

        await _audit.WriteAsync(policy.Id, AuditEventType, command.ActorUserId, new
        {
            EntrySubstatus = entrySubstatus,
            command.Decision,
            FinalSubstatus = finalSubstatus,
            directive.DecisionSpecificLetters,
            MainLetter = mainLetter,
            Composition = composition,
            directive.AutoCreateIpRecord,
            directive.AutoRemoveIpRecord,
            directive.SkipBasePremiumRecalc,
        }, innerCt);
        }
    }

    // FR-AOR-040 scheduling rules (source lines 339-372).
    private static bool ShouldScheduleReminders(LetterType mainLetter, Policy policy, UWState? updatedUwState)
    {
        var shortfall = policy.PremiumCollection?.TotalShortfall.IsPositive == true;

        return mainLetter switch
        {
            // LOA reminders only if there's outstanding cash.
            LetterType.Loa => shortfall,

            // CLOA reminders if there's shortfall OR the customer hasn't accepted
            // the CLOA yet (unaccepted = AcceptCloa != Yes).
            LetterType.CloaExclusion or LetterType.CloaRcmp =>
                shortfall || (updatedUwState?.AcceptCloa ?? AcceptCloa.Blank) != AcceptCloa.Yes,

            _ => false,
        };
    }

    private static PolicyContext BuildContext(Policy policy)
    {
        var residency = new ResidencyPair(policy.InsuredResidency, policy.PayerResidency);
        var shortfall = policy.PremiumCollection?.TotalShortfall ?? Money.Zero();
        var basePlan = policy.Plans.FirstOrDefault(p => p.IsBase);
        var isRenewal = policy.Type == PolicyType.Renewal;
        var baseHasLoading = basePlan?.HasActiveRiskLoading ?? false;
        return new PolicyContext(residency, shortfall, isRenewal, baseHasLoading);
    }
}
