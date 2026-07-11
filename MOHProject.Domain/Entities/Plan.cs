using MOHProject.Domain.Enums;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Domain.Entities;

public class Plan
{
    public long Id { get; set; }
    public long PolicyId { get; set; }
    public Policy? Policy { get; set; }

    public bool IsBase { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public ProductStatus Status { get; set; }

    public bool HasActiveRiskLoading { get; set; }
    public bool HasActiveExclusion { get; set; }

    public Money GrossPremium { get; set; }
    public Money PrivateInsuranceExtraPremium { get; set; }

    public DateTime? AddedAt { get; set; }
    public DateTime? StatusChangedAt { get; set; }

    public bool IsSelectedInProductTab { get; set; }

    public RiskAssessment RiskAssessment => new(HasActiveRiskLoading, HasActiveExclusion);
    public RiskCategory RiskCategory => RiskAssessment.DeriveRiskCategory();
}
