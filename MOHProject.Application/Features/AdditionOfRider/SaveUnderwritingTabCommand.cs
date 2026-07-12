using MOHProject.Application.Ports;
using MOHProject.Domain.Enums;

namespace MOHProject.Application.Features.AdditionOfRider;

// Enhancement 1 — Auto-Route to Underwriter (FR-AOR-001).
// Source: MOH_AdditionOfRiders_Analysis.html lines 211-235.
public sealed record SaveUnderwritingTabCommand(long PolicyId, string ActorUserId);

public sealed class SaveUnderwritingTabCommandHandler
{
    // Audit event text per doc line 233 (replaces old "user manual submit for underwriting review").
    public const string AuditEventType = "SubmitForUnderwritingReview";

    private readonly IPolicyRepository _policies;
    private readonly IAuditTrailWriter _audit;

    public SaveUnderwritingTabCommandHandler(IPolicyRepository policies, IAuditTrailWriter audit)
    {
        _policies = policies;
        _audit = audit;
    }

    public async Task HandleAsync(SaveUnderwritingTabCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var policy = await _policies.GetByIdAsync(command.PolicyId, ct)
            ?? throw new InvalidOperationException($"Policy {command.PolicyId} not found.");

        // FR-AOR-001: auto-route only when at least one plan is still in Draft
        // (Risk Category BLANK — user hasn't manually underwritten it).
        var anyDraftPlan = policy.Plans.Any(p => p.Status == ProductStatus.Draft);
        if (!anyDraftPlan)
            return;

        policy.Substatus = PolicySubstatus.PendingManualUnderwriting;
        await _policies.SaveAsync(policy, ct);

        await _audit.WriteAsync(policy.Id, AuditEventType,
            new { command.ActorUserId, TriggeredAt = DateTime.UtcNow }, ct);
    }
}
