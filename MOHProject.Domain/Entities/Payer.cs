using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Entities;

public class Payer
{
    public long Id { get; set; }
    public Residency Residency { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string ExternalId { get; set; } = string.Empty;
}
