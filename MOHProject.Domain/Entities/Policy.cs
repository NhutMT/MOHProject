using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Entities;

public class Policy
{
    public long Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public PolicyType Type { get; set; }
    public PolicySubstatus Substatus { get; set; }

    public Residency InsuredResidency { get; set; }
    public Residency PayerResidency { get; set; }

    public DateTime? UwCompletedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public long? UwStateId { get; set; }
    public UWState? UWState { get; set; }

    public long? PremiumCollectionId { get; set; }
    public PremiumCollection? PremiumCollection { get; set; }

    public long? InsuredId { get; set; }
    public Insured? Insured { get; set; }

    public long? PayerId { get; set; }
    public Payer? Payer { get; set; }

    public long? PolicyHolderId { get; set; }
    public PolicyHolder? PolicyHolder { get; set; }

    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
    public ICollection<Letter> Letters { get; set; } = new List<Letter>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public ICollection<AuditEntry> AuditEntries { get; set; } = new List<AuditEntry>();
}
