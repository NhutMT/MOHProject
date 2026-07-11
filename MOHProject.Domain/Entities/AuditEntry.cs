namespace MOHProject.Domain.Entities;

public class AuditEntry
{
    public long Id { get; set; }
    public long PolicyId { get; set; }
    public Policy? Policy { get; set; }

    public DateTime OccurredAt { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}
