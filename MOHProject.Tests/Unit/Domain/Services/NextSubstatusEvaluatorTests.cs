using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Tests.Unit.Domain.Services;

public class NextSubstatusEvaluatorTests
{
    private readonly NextSubstatusEvaluator _sut = new();

    private static PolicyContext Ctx(Residency insured, Residency payer, decimal shortfall) =>
        new(new ResidencyPair(insured, payer), new Money(shortfall), IsRenewal: false, BaseHasRiskLoading: false);

    private static UWState State(AcceptCloa cloa) => new() { AcceptCloa = cloa };

    // ------------------------------------------------------------------
    // AllStandard branch (source lines 309-322)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(Residency.Sg, Residency.Sg, PolicySubstatus.PendingIpRequestFile)]
    [InlineData(Residency.Sg, Residency.Fr, PolicySubstatus.PendingIpRequestFile)]
    [InlineData(Residency.Fr, Residency.Sg, PolicySubstatus.PendingIpRequestFile)]
    [InlineData(Residency.Pr, Residency.Pr, PolicySubstatus.PendingIpRequestFile)]
    [InlineData(Residency.Fr, Residency.Fr, PolicySubstatus.PolicyIncepted)]
    public void AllStandard_NoShortfall_ResidencyDeterminesTerminal(Residency insured, Residency payer, PolicySubstatus expected)
    {
        var result = _sut.Evaluate(RiskComposition.AllStandard, State(AcceptCloa.Blank), Ctx(insured, payer, 0m));
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(Residency.Sg, Residency.Sg)]
    [InlineData(Residency.Fr, Residency.Fr)]
    public void AllStandard_Shortfall_AlwaysPendingCash_RegardlessOfResidency(Residency insured, Residency payer)
    {
        var result = _sut.Evaluate(RiskComposition.AllStandard, State(AcceptCloa.Blank), Ctx(insured, payer, 100m));
        result.Should().Be(PolicySubstatus.PendingCashCollection);
    }

    [Fact]
    public void AllStandard_IgnoresAcceptCloa()
    {
        var sg = Ctx(Residency.Sg, Residency.Sg, 0m);
        _sut.Evaluate(RiskComposition.AllStandard, State(AcceptCloa.Blank), sg).Should().Be(PolicySubstatus.PendingIpRequestFile);
        _sut.Evaluate(RiskComposition.AllStandard, State(AcceptCloa.Yes),   sg).Should().Be(PolicySubstatus.PendingIpRequestFile,
            "AllStandard should not check AcceptCloa — no CLOA was ever issued");
    }

    // ------------------------------------------------------------------
    // ExclusionOnly / HasRcmp branch (source lines 324-335)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(RiskComposition.ExclusionOnly)]
    [InlineData(RiskComposition.HasRcmp)]
    public void Substandard_AcceptCloaBlank_YieldsCondAccept(RiskComposition composition)
    {
        var result = _sut.Evaluate(composition, State(AcceptCloa.Blank), Ctx(Residency.Sg, Residency.Sg, 0m));
        result.Should().Be(PolicySubstatus.ConditionalAcceptanceLetterGenerated,
            "no customer acceptance yet → cannot progress past the conditional letter");
    }

    [Theory]
    [InlineData(RiskComposition.ExclusionOnly, Residency.Sg, Residency.Sg, PolicySubstatus.PendingIpRequestFile)]
    [InlineData(RiskComposition.HasRcmp,      Residency.Sg, Residency.Fr, PolicySubstatus.PendingIpRequestFile)]
    [InlineData(RiskComposition.ExclusionOnly, Residency.Fr, Residency.Fr, PolicySubstatus.PolicyIncepted)]
    [InlineData(RiskComposition.HasRcmp,      Residency.Fr, Residency.Fr, PolicySubstatus.PolicyIncepted)]
    public void Substandard_AcceptCloaYes_NoShortfall_TerminalByResidency(
        RiskComposition composition, Residency insured, Residency payer, PolicySubstatus expected)
    {
        var result = _sut.Evaluate(composition, State(AcceptCloa.Yes), Ctx(insured, payer, 0m));
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(RiskComposition.ExclusionOnly)]
    [InlineData(RiskComposition.HasRcmp)]
    public void Substandard_AcceptCloaYes_Shortfall_PendingCash(RiskComposition composition)
    {
        var result = _sut.Evaluate(composition, State(AcceptCloa.Yes), Ctx(Residency.Sg, Residency.Sg, 50m));
        result.Should().Be(PolicySubstatus.PendingCashCollection);
    }

    [Fact]
    public void NullInputs_Throw()
    {
        Action nullState = () => _sut.Evaluate(RiskComposition.AllStandard, null!, Ctx(Residency.Sg, Residency.Sg, 0m));
        Action nullCtx   = () => _sut.Evaluate(RiskComposition.AllStandard, State(AcceptCloa.Blank), null!);

        nullState.Should().Throw<ArgumentNullException>();
        nullCtx.Should().Throw<ArgumentNullException>();
    }
}
