namespace MOHProject.Application.Ports;

public interface IAuditTrailWriter
{
    Task WriteAsync(long policyId, string eventType, string actorUserId, object payload, CancellationToken ct);
}
