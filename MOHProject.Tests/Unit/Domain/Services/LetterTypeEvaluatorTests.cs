using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;

namespace MOHProject.Tests.Unit.Domain.Services;

public class LetterTypeEvaluatorTests
{
    private readonly LetterTypeEvaluator _sut = new();

    private static UWState State(AcceptCloa cloa) => new() { AcceptCloa = cloa };

    // ------------------------------------------------------------------
    // Composition → LetterType (FR-LTR-COMBO-020, at a letter-generating substatus)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(RiskComposition.AllStandard, LetterType.Loa)]
    [InlineData(RiskComposition.ExclusionOnly, LetterType.CloaExclusion)]
    [InlineData(RiskComposition.HasRcmp, LetterType.CloaRcmp)]
    public void CondAcceptSubstatus_MapsCompositionToLetterType(RiskComposition composition, LetterType expected)
    {
        var decision = _sut.Evaluate(composition, State(AcceptCloa.Blank),
            PolicySubstatus.ConditionalAcceptanceLetterGenerated);

        decision.Type.Should().Be(expected);
    }

    // ------------------------------------------------------------------
    // Ack page rule (FR-LTR-COMBO-010)
    // ------------------------------------------------------------------

    [Fact]
    public void Loa_NeverIncludesAck()
    {
        var decision = _sut.Evaluate(RiskComposition.AllStandard, State(AcceptCloa.Blank),
            PolicySubstatus.PendingCashCollection);

        decision.Type.Should().Be(LetterType.Loa);
        decision.HasAcknowledgementPage.Should().BeFalse(
            "LOA has no conditions — nothing to acknowledge");
    }

    [Theory]
    [InlineData(RiskComposition.ExclusionOnly, LetterType.CloaExclusion)]
    [InlineData(RiskComposition.HasRcmp, LetterType.CloaRcmp)]
    public void Cloa_AcceptCloaBlank_IncludesAck(RiskComposition composition, LetterType expected)
    {
        var decision = _sut.Evaluate(composition, State(AcceptCloa.Blank),
            PolicySubstatus.ConditionalAcceptanceLetterGenerated);

        decision.Type.Should().Be(expected);
        decision.HasAcknowledgementPage.Should().BeTrue(
            "CLOA + AcceptCloa=Blank → customer hasn't agreed yet → Ack required");
    }

    [Theory]
    [InlineData(RiskComposition.ExclusionOnly, LetterType.CloaExclusion)]
    [InlineData(RiskComposition.HasRcmp, LetterType.CloaRcmp)]
    public void Cloa_AcceptCloaYes_OmitsAck(RiskComposition composition, LetterType expected)
    {
        var decision = _sut.Evaluate(composition, State(AcceptCloa.Yes),
            PolicySubstatus.PendingCashCollection);

        decision.Type.Should().Be(expected);
        decision.HasAcknowledgementPage.Should().BeFalse(
            "CLOA + AcceptCloa=Yes → superseding letter is informational, no Ack");
    }

    // ------------------------------------------------------------------
    // Substatus-gated skip (source line 271)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(PolicySubstatus.PendingManualUnderwriting)]
    [InlineData(PolicySubstatus.PendingUwAps)]
    [InlineData(PolicySubstatus.PendingIpRequestFile)]
    [InlineData(PolicySubstatus.PendingIpResponseFileCpfRejected)]
    [InlineData(PolicySubstatus.PolicyIncepted)]
    [InlineData(PolicySubstatus.PendingPpRequestFile)]
    [InlineData(PolicySubstatus.PendingPpResponseFileCpfRejected)]
    public void NonLetterGeneratingSubstatus_ReturnsNoLetter(PolicySubstatus substatus)
    {
        var decision = _sut.Evaluate(RiskComposition.AllStandard, State(AcceptCloa.Blank), substatus);

        decision.Type.Should().BeNull(
            $"letter is only re-generated at CondAccept / PendingUwCloaAssessment / PendingCashCollection; {substatus} is not one of them (source line 271)");
        decision.HasAcknowledgementPage.Should().BeFalse();
    }

    [Theory]
    [InlineData(PolicySubstatus.ConditionalAcceptanceLetterGenerated)]
    [InlineData(PolicySubstatus.PendingUwCloaAssessment)]
    [InlineData(PolicySubstatus.PendingCashCollection)]
    public void LetterGeneratingSubstatuses_AlwaysProduceLetter(PolicySubstatus substatus)
    {
        var decision = _sut.Evaluate(RiskComposition.AllStandard, State(AcceptCloa.Blank), substatus);

        decision.Type.Should().NotBeNull();
    }

    [Fact]
    public void NullState_Throws()
    {
        Action act = () => _sut.Evaluate(RiskComposition.AllStandard, null!, PolicySubstatus.PendingCashCollection);
        act.Should().Throw<ArgumentNullException>();
    }
}
