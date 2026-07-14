using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Application.Features.AdditionOfRider;

// PH-06-01 bullet #4: re-add a rider that was previously NTU / Declined / Postponed.
// Restores Plan.Status to Active and re-runs the full evaluator pipeline.
// Composition may swing back to what it was before the removal.
public sealed record ReAddRiderCommand(long PolicyId, long PlanId, string ActorUserId);

public sealed class ReAddRiderCommandHandler
{
    public const string AuditEventType = "RiderReAdded";

    // Only reversible from these statuses. Terminated / Cancelled are lifecycle
    // ends that must not be undone via this command.
    public static readonly IReadOnlySet<ProductStatus> ReversibleFrom = new HashSet<ProductStatus>
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

    public ReAddRiderCommandHandler(
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

    public Task HandleAsync(ReAddRiderCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
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
                    "Cannot re-add the Base plan — Base is never removed to begin with.");

            if (!ReversibleFrom.Contains(rider.Status))
                throw new InvalidOperationException(
                    $"Cannot re-add a rider from status '{rider.Status}'. " +
                    $"Reversible only from: {string.Join(", ", ReversibleFrom)}.");

            var previousStatus = rider.Status;
            rider.Status = ProductStatus.Active;
            rider.StatusChangedAt = DateTime.UtcNow;

            // Run the full pipeline — same shape as MarkRiderStatusCommand.
            var evalResult = _evaluator.EvaluateAfterAction(policy, BuildContext(policy));

            policy.Substatus = evalResult.NextSubstatus;
            policy.UWState = evalResult.UpdatedUWState;

            if (evalResult.LetterToGenerate is { } main)
            {
                var mainRow = await _letters.GenerateAsync(policy.Id, main, innerCt);
                if (ShouldScheduleReminders(main, policy))
                    await _reminders.ScheduleFromAsync(mainRow, innerCt);
            }

            if (evalResult.NextSubstatus == PolicySubstatus.PendingUwAps)
                await _reminders.CancelAllForPolicyAsync(policy.Id, innerCt);

            await _policies.SaveAsync(policy, innerCt);

            await _audit.WriteAsync(policy.Id, AuditEventType, command.ActorUserId, new
            {
                command.PlanId,
                RiderProductCode = rider.ProductCode,
                PreviousStatus = previousStatus,
                NewStatus = ProductStatus.Active,
                evalResult.Composition,
                NextSubstatus = evalResult.NextSubstatus,
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
