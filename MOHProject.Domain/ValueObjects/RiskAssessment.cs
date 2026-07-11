using MOHProject.Domain.Enums;

namespace MOHProject.Domain.ValueObjects;

public readonly record struct RiskAssessment
{
    public bool HasActiveRiskLoading { get; }
    public bool HasActiveExclusion { get; }

    public RiskAssessment(bool hasActiveRiskLoading, bool hasActiveExclusion)
    {
        HasActiveRiskLoading = hasActiveRiskLoading;
        HasActiveExclusion = hasActiveExclusion;
    }

    public static RiskAssessment Standard { get; } = new(false, false);

    public RiskCategory DeriveRiskCategory() =>
        (HasActiveRiskLoading, HasActiveExclusion) switch
        {
            (false, false) => RiskCategory.Standard,
            (true, false) => RiskCategory.SubstandardLoading,
            (false, true) => RiskCategory.SubstandardExclusion,
            (true, true) => RiskCategory.SubstandardBoth,
        };

    public bool IsSubstandard => HasActiveRiskLoading || HasActiveExclusion;
    public bool IsRcmp => HasActiveRiskLoading && HasActiveExclusion;
}
