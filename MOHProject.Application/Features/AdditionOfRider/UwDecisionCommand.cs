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

    public UwDecisionCommandHandler(
        IPolicyRepository policies,
        IEntryPointHandlerRegistry handlers,
        IRemainingPlansEvaluator evaluator,
        ILetterGenerator letters,
        IAuditTrailWriter audit)
    {
        _policies = policies;
        _handlers = handlers;
        _evaluator = evaluator;
        _letters = letters;
        _audit = audit;
    }

    public async Task HandleAsync(UwDecisionCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var policy = await _policies.GetByIdAsync(command.PolicyId, ct)
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
            await _letters.GenerateAsync(policy.Id, letterType, ct);

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
                await _letters.GenerateAsync(policy.Id, main, ct);
        }

        policy.Substatus = finalSubstatus;
        await _policies.SaveAsync(policy, ct);

        await _audit.WriteAsync(policy.Id, AuditEventType, new
        {
            command.ActorUserId,
            EntrySubstatus = entrySubstatus,
            command.Decision,
            FinalSubstatus = finalSubstatus,
            directive.DecisionSpecificLetters,
            MainLetter = mainLetter,
            Composition = composition,
            directive.AutoCreateIpRecord,
            directive.AutoRemoveIpRecord,
            directive.SkipBasePremiumRecalc,
        }, ct);
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
