using MOHProject.Domain.Enums;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Tests.Unit.Domain.ValueObjects;

public class RiskAssessmentTests
{
    [Theory]
    [InlineData(false, false, RiskCategory.Standard)]
    [InlineData(true, false, RiskCategory.SubstandardLoading)]
    [InlineData(false, true, RiskCategory.SubstandardExclusion)]
    [InlineData(true, true, RiskCategory.SubstandardBoth)]
    public void DeriveRiskCategory_TruthTable(bool loading, bool exclusion, RiskCategory expected)
    {
        var ra = new RiskAssessment(loading, exclusion);
        ra.DeriveRiskCategory().Should().Be(expected);
    }

    [Fact]
    public void IsRcmp_TrueOnlyWhenBothPresent()
    {
        new RiskAssessment(true, true).IsRcmp.Should().BeTrue(
            "RCMP composition requires BOTH Loading and Exclusion active on the same plan");
        new RiskAssessment(true, false).IsRcmp.Should().BeFalse();
        new RiskAssessment(false, true).IsRcmp.Should().BeFalse();
        new RiskAssessment(false, false).IsRcmp.Should().BeFalse();
    }

    [Fact]
    public void IsSubstandard_TrueWhenAnyRiskPresent()
    {
        new RiskAssessment(true, false).IsSubstandard.Should().BeTrue();
        new RiskAssessment(false, true).IsSubstandard.Should().BeTrue();
        new RiskAssessment(true, true).IsSubstandard.Should().BeTrue();
        new RiskAssessment(false, false).IsSubstandard.Should().BeFalse();
    }

    [Fact]
    public void Standard_StaticInstance_HasNoRisk()
    {
        RiskAssessment.Standard.HasActiveRiskLoading.Should().BeFalse();
        RiskAssessment.Standard.HasActiveExclusion.Should().BeFalse();
        RiskAssessment.Standard.DeriveRiskCategory().Should().Be(RiskCategory.Standard);
    }
}
