using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Application.Features.AdditionOfRider;

// FR-AOR-030 trigger #1..#3 — the command surface for NTU / Declined / Postponed
// user actions on a specific rider. This is where the RemainingPlansEvaluator is
// called and the 4 UAT bugs' root cause is prevented (see BUG-uat-2026-06).
public sealed record MarkRiderStatusCommand(
    long PolicyId,
    long PlanId,
    ProductStatus NewStatus,
    string ActorUserId);

public sealed class MarkRiderStatusCommandHandler
{
    public const string AuditEventType = "RiderStatusChanged";

    // Rider-status transitions that this command supports. Other statuses are
    // reserved for lifecycle events not driven by AOR (Terminated, Cancelled).
    public static readonly IReadOnlySet<ProductStatus> AllowedTargetStatuses = new HashSet<ProductStatus>
    {
        ProductStatus.NotTakenUp,
        ProductStatus.Declined,
        ProductStatus.Postponed,
    };

    private readonly IPolicyRepository _policies;
    private readonly IRemainingPlansEvaluator _evaluator;
    private readonly ILetterGenerator _letters;
    private readonly IAuditTrailWriter _audit;
    private readonly IUnitOfWork _uow;
    private readonly IReminderScheduler _reminders;

    public MarkRiderStatusCommandHandler(
        IPolicyRepository policies,
        IRemainingPlansEvaluator evaluator,
        ILetterGenerator letters,
        IAuditTrailWriter audit,
        IUnitOfWork uow,
        IReminderScheduler reminders)
    {
        _policies = policies;
        _evaluator = evaluator;
        _letters = letters;
        _audit = audit;
        _uow = uow;
        _reminders = reminders;
    }

    public Task HandleAsync(MarkRiderStatusCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Guard fires BEFORE the transaction opens — no DB work if invalid.
        if (!AllowedTargetStatuses.Contains(command.NewStatus))
            throw new InvalidOperationException(
                $"Cannot mark a rider as '{command.NewStatus}' via this command. " +
                $"Allowed target statuses: {string.Join(", ", AllowedTargetStatuses)}.");

        return _uow.ExecuteInTransactionAsync(HandleInner, ct);

        async Task HandleInner(CancellationToken innerCt)
        {
            var policy = await _policies.GetByIdAsync(command.PolicyId, innerCt)
                ?? throw new InvalidOperationException($"Policy {command.PolicyId} not found.");

            var rider = policy.Plans.FirstOrDefault(p => p.Id == command.PlanId)
                ?? throw new InvalidOperationException(
                    $"Plan {command.PlanId} not found on policy {command.PolicyId}.");

            if (rider.IsBase)
                throw new InvalidOperationException(
                    $"Cannot change status of the Base plan via this command. " +
                    "Base plans cannot be NTU'd, Declined, or Postponed independently of the policy.");

            var previousStatus = rider.Status;
            rider.Status = command.NewStatus;
            rider.StatusChangedAt = DateTime.UtcNow;

            // Emit the decision-specific letter (NTU / Decline / Postpone).
            var decisionLetter = DecisionLetterFor(command.NewStatus);
            await _letters.GenerateAsync(policy.Id, decisionLetter, innerCt);

            // The event that fixes the 4 UAT bugs: run the evaluator, apply its result.
            var evalResult = _evaluator.EvaluateAfterAction(policy, BuildContext(policy));

            policy.Substatus = evalResult.NextSubstatus;
            policy.UWState = evalResult.UpdatedUWState;

            // Main letter (LOA / CLOA per composition) — may be null when the new
            // substatus is outside the letter-gating set.
            if (evalResult.LetterToGenerate is { } main)
            {
                var mainRow = await _letters.GenerateAsync(policy.Id, main, innerCt);

                // Schedule reminders using the same FR-AOR-040 policy as UwDecisionCommand.
                if (ShouldScheduleReminders(main, policy))
                    await _reminders.ScheduleFromAsync(mainRow, innerCt);
            }

            // Substatus → PendingUwAps stops ALL reminders (doc lines 363-365).
            if (evalResult.NextSubstatus == PolicySubstatus.PendingUwAps)
                await _reminders.CancelAllForPolicyAsync(policy.Id, innerCt);

            await _policies.SaveAsync(policy, innerCt);

            await _audit.WriteAsync(policy.Id, AuditEventType, command.ActorUserId, new
            {
                command.PlanId,
                RiderProductCode = rider.ProductCode,
                PreviousStatus = previousStatus,
                NewStatus = command.NewStatus,
                evalResult.Composition,
                NextSubstatus = evalResult.NextSubstatus,
                DecisionLetter = decisionLetter,
                MainLetter = evalResult.LetterToGenerate,
            }, innerCt);
        }
    }

    private static bool ShouldScheduleReminders(LetterType mainLetter, Policy policy)
    {
        var shortfall = policy.PremiumCollection?.TotalShortfall.IsPositive == true;
        return mainLetter switch
        {
            LetterType.Loa => shortfall,
            LetterType.CloaExclusion or LetterType.CloaRcmp
                => shortfall || (policy.UWState?.AcceptCloa ?? AcceptCloa.Blank) != AcceptCloa.Yes,
            _ => false,
        };
    }

    private static LetterType DecisionLetterFor(ProductStatus status) => status switch
    {
        ProductStatus.NotTakenUp => LetterType.NtuWithoutRefund,
        ProductStatus.Declined   => LetterType.Decline,
        ProductStatus.Postponed  => LetterType.Postponement,
        _ => throw new InvalidOperationException($"No decision letter defined for status {status}."),
    };

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
