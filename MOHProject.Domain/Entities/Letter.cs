using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Entities;

public class Letter
{
    public long Id { get; set; }
    public long PolicyId { get; set; }
    public Policy? Policy { get; set; }

    public LetterType Type { get; set; }
    public DateTime IssuedAt { get; set; }

    public bool IsCurrent { get; set; } = true;
    public Guid CorrelationId { get; set; }

    public ICollection<LetterPlan> IncludedPlans { get; set; } = new List<LetterPlan>();
}

public class LetterPlan
{
    public long LetterId { get; set; }
    public Letter? Letter { get; set; }

    public long PlanId { get; set; }
    public Plan? Plan { get; set; }
}
