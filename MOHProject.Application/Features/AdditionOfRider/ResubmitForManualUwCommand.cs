using MOHProject.Application.Ports;
using MOHProject.Domain.Enums;

namespace MOHProject.Application.Features.AdditionOfRider;

// FR-AOR-042 — Resubmit for Manual UW.
// Source: MOH_AdditionOfRiders_Analysis.html lines 202-205.
public sealed record ResubmitForManualUwCommand(long PolicyId, string ActorUserId);

public sealed class ResubmitForManualUwCommandHandler
{
    public const string AuditEventType = "ResubmitForManualUnderwriting";

    // FR-AOR-042: only allowed from these substatuses.
    public static readonly IReadOnlySet<PolicySubstatus> AllowedFrom = new HashSet<PolicySubstatus>
    {
        PolicySubstatus.PendingUwAps,
        PolicySubstatus.ConditionalAcceptanceLetterGenerated,
        PolicySubstatus.PendingCashCollection,
        PolicySubstatus.PendingIpRequestFile,
    };

    private readonly IPolicyRepository _policies;
    private readonly IAuditTrailWriter _audit;

    public ResubmitForManualUwCommandHandler(IPolicyRepository policies, IAuditTrailWriter audit)
    {
        _policies = policies;
        _audit = audit;
    }

    public async Task HandleAsync(ResubmitForManualUwCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        var policy = await _policies.GetByIdAsync(command.PolicyId, ct)
            ?? throw new InvalidOperationException($"Policy {command.PolicyId} not found.");

        if (!AllowedFrom.Contains(policy.Substatus))
            throw new InvalidOperationException(
                $"Resubmit for Manual UW not allowed from substatus '{policy.Substatus}'. " +
                $"Allowed from: {string.Join(", ", AllowedFrom)} (FR-AOR-042, source lines 202-205).");

        policy.Substatus = PolicySubstatus.PendingManualUnderwriting;
        await _policies.SaveAsync(policy, ct);

        await _audit.WriteAsync(policy.Id, AuditEventType,
            new { command.ActorUserId, TriggeredAt = DateTime.UtcNow }, ct);
    }
}
