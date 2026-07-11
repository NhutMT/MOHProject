using Microsoft.Extensions.Logging.Abstractions;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Tests.Unit.Domain.Services;

public class RemainingPlansEvaluatorTests
{
    private readonly RemainingPlansEvaluator _sut = new(
        new PlansCompositionEvaluator(),
        new UwFieldStatesEvaluator(),
        new NextSubstatusEvaluator(),
        new LetterTypeEvaluator(),
        NullLogger<RemainingPlansEvaluator>.Instance);

    [Fact]
    public void NullPolicy_Throws()
    {
        Action act = () => _sut.EvaluateAfterAction(null!, NewContext());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NullContext_Throws()
    {
        Action act = () => _sut.EvaluateAfterAction(NewPolicy(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PolicyWithoutUWState_Throws()
    {
        var policy = new Policy { Id = 42 };

        Action act = () => _sut.EvaluateAfterAction(policy, NewContext());

        act.Should().Throw<InvalidOperationException>().WithMessage("*Policy 42*UWState*");
    }

    [Fact]
    public void FiltersOutInactivePlans_BeforeComposition()
    {
        var policy = NewPolicy();
        policy.Plans.Add(BasePlan(loading: false, exclusion: false));
        // A "leftover" RCMP rider still on the policy but NTU'd — must not push composition to HasRcmp.
        policy.Plans.Add(new Plan
        {
            HasActiveRiskLoading = true,
            HasActiveExclusion = true,
            Status = ProductStatus.NotTakenUp,
        });

        var result = _sut.EvaluateAfterAction(policy, NewContext());

        result.Composition.Should().Be(RiskComposition.AllStandard,
            "NTU'd plan is excluded from active set; only the Standard Base contributes");
    }

    [Fact]
    public void AllStandard_SgSg_NoShortfall_YieldsPendingIpRequestFile_WithLoa()
    {
        var policy = NewPolicy();
        policy.Plans.Add(BasePlan(loading: false, exclusion: false));

        var result = _sut.EvaluateAfterAction(policy, NewContext(shortfall: 0m));

        result.Composition.Should().Be(RiskComposition.AllStandard);
        result.NextSubstatus.Should().Be(PolicySubstatus.PendingIpRequestFile);
        // At PendingIpRequestFile substatus, letter is NOT generated (source line 271 skip rule)
        result.LetterToGenerate.Should().BeNull(
            "PendingIpRequestFile is not a letter-generating substatus");
    }

    [Fact]
    public void RCMP_Shortfall_YieldsPendingCash_WithCloaRcmp()
    {
        var policy = NewPolicy();
        policy.UWState!.AcceptCloa = AcceptCloa.Yes;
        policy.Plans.Add(BasePlan(loading: false, exclusion: false));
        policy.Plans.Add(new Plan
        {
            HasActiveRiskLoading = true,
            HasActiveExclusion = true,
            Status = ProductStatus.Active,
        });

        var result = _sut.EvaluateAfterAction(policy, NewContext(shortfall: 100m));

        result.Composition.Should().Be(RiskComposition.HasRcmp);
        result.NextSubstatus.Should().Be(PolicySubstatus.PendingCashCollection);
        result.LetterToGenerate.Should().Be(LetterType.CloaRcmp);
        result.LetterHasAcknowledgementPage.Should().BeFalse(
            "AcceptCloa=Yes → superseding CLOA is informational, no Ack");
    }

    [Fact]
    public void RepeatedInvocations_AreIdempotent_ForSameInput()
    {
        var policy = NewPolicy();
        policy.Plans.Add(BasePlan(loading: false, exclusion: true));

        var r1 = _sut.EvaluateAfterAction(policy, NewContext());
        var r2 = _sut.EvaluateAfterAction(policy, NewContext());

        r1.Composition.Should().Be(r2.Composition);
        r1.NextSubstatus.Should().Be(r2.NextSubstatus);
        r1.LetterToGenerate.Should().Be(r2.LetterToGenerate);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static Policy NewPolicy() => new()
    {
        Id = 1,
        UWState = new UWState(),
    };

    private static Plan BasePlan(bool loading, bool exclusion) => new()
    {
        IsBase = true,
        Status = ProductStatus.Active,
        HasActiveRiskLoading = loading,
        HasActiveExclusion = exclusion,
    };

    private static PolicyContext NewContext(decimal shortfall = 0m) =>
        new(new ResidencyPair(Residency.Sg, Residency.Sg),
            new Money(shortfall),
            IsRenewal: false,
            BaseHasRiskLoading: false);
}
