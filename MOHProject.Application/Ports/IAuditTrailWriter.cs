namespace MOHProject.Application.Ports;

public interface IAuditTrailWriter
{
    Task WriteAsync(long policyId, string eventType, object payload, CancellationToken ct);
}
