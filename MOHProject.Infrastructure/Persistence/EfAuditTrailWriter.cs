using System.Text.Json;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;

namespace MOHProject.Infrastructure.Persistence;

// Implements PH-04-09. Serializes payload with System.Text.Json (camelCase,
// non-indented). The payload contract is enforced by convention — command
// handlers pass anonymous records that hold only IDs, enum values, and
// non-PII fields. If future callers need to reference an Insured or Payer,
// they must pass ExternalId or a projected DTO, never the entity itself.
public sealed class EfAuditTrailWriter : IAuditTrailWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly AppDbContext _db;

    public EfAuditTrailWriter(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(long policyId, string eventType, string actorUserId, object payload, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorUserId);
        ArgumentNullException.ThrowIfNull(payload);

        var entry = new AuditEntry
        {
            PolicyId = policyId,
            OccurredAt = DateTime.UtcNow,
            EventType = eventType,
            ActorUserId = actorUserId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
        };

        _db.AuditEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
