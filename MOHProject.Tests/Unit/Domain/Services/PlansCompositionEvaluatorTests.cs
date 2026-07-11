using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;

namespace MOHProject.Tests.Unit.Domain.Services;

public class PlansCompositionEvaluatorTests
{
    private readonly PlansCompositionEvaluator _sut = new();

    [Fact]
    public void EmptyActivePlans_Throws()
    {
        Action act = () => _sut.Evaluate(Array.Empty<Plan>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*empty*",
                "an empty active set violates the domain invariant that a policy always retains at least a Base plan");
    }

    [Fact]
    public void Null_Throws()
    {
        Action act = () => _sut.Evaluate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OnlyStandardPlans_ReturnsAllStandard()
    {
        var plans = new[]
        {
            Plan(loading: false, exclusion: false),
            Plan(loading: false, exclusion: false),
        };

        _sut.Evaluate(plans).Should().Be(RiskComposition.AllStandard);
    }

    [Fact]
    public void OnePlanExclusionOnly_OthersStandard_ReturnsExclusionOnly()
    {
        var plans = new[]
        {
            Plan(loading: false, exclusion: false),
            Plan(loading: false, exclusion: true),
        };

        _sut.Evaluate(plans).Should().Be(RiskComposition.ExclusionOnly);
    }

    [Fact]
    public void OnePlanLoadingOnly_OthersStandard_ReturnsAllStandard_BecauseLoadingWithoutExclusionIsNotRcmp()
    {
        // Loading-only plan without exclusion does NOT elevate composition to HasRcmp
        // and does not qualify as ExclusionOnly. Composition source doc lines 279-307
        // enumerates only 3 buckets — Loading-only stays in AllStandard bucket for evaluation.
        var plans = new[]
        {
            Plan(loading: false, exclusion: false),
            Plan(loading: true, exclusion: false),
        };

        _sut.Evaluate(plans).Should().Be(RiskComposition.AllStandard,
            "loading-only plan without exclusion is not in the ExclusionOnly bucket; " +
            "Base=Loading edge case is handled by the letter combination rules (FR-LTR-COMBO-002), not by composition");
    }

    [Fact]
    public void OnePlanBothLoadingAndExclusion_ReturnsHasRcmp()
    {
        var plans = new[]
        {
            Plan(loading: false, exclusion: false),
            Plan(loading: true, exclusion: true),
        };

        _sut.Evaluate(plans).Should().Be(RiskComposition.HasRcmp);
    }

    [Fact]
    public void MixedRcmpAndExclusion_ReturnsHasRcmp_HighestPriorityWins()
    {
        var plans = new[]
        {
            Plan(loading: true, exclusion: true),   // RCMP
            Plan(loading: false, exclusion: true),  // Exclusion only
        };

        _sut.Evaluate(plans).Should().Be(RiskComposition.HasRcmp,
            "priority order: HasRcmp > ExclusionOnly > AllStandard (source lines 1904-1911)");
    }

    [Fact]
    public void SinglePlan_LoadingAndExclusion_ReturnsHasRcmp()
    {
        var plans = new[] { Plan(loading: true, exclusion: true) };

        _sut.Evaluate(plans).Should().Be(RiskComposition.HasRcmp);
    }

    private static Plan Plan(bool loading, bool exclusion) => new()
    {
        HasActiveRiskLoading = loading,
        HasActiveExclusion = exclusion,
        Status = ProductStatus.Active,
    };
}
