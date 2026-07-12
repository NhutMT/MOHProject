using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;

namespace MOHProject.Tests.Unit.Domain.Services.EntryPoints;

public class PendingCashCollectionHandlerTests
{
    private readonly PendingCashCollectionHandler _sut = new();

    [Fact]
    public void EntrySubstatus_Is_PendingCashCollection()
    {
        _sut.EntrySubstatus.Should().Be(PolicySubstatus.PendingCashCollection);
    }

    [Fact]
    public void Aps_OverridesToPendingUwAps_WithMedicalEvidence()
    {
        var d = _sut.Handle(NewPolicy(AcceptCloa.Yes, RcmpOption.Option1), UwDecision.Aps);

        d.OverrideNextSubstatus.Should().Be(PolicySubstatus.PendingUwAps);
        d.DecisionSpecificLetters.Should().ContainSingle().Which.Should().Be(LetterType.MedicalEvidence);
    }

    [Fact]
    public void Standard_Defers_WithoutClearingAcceptCloaOrRcmpOption()
    {
        var d = _sut.Handle(NewPolicy(AcceptCloa.Yes, RcmpOption.Option2), UwDecision.Standard);

        d.OverrideNextSubstatus.Should().BeNull();
        d.UWStateBeforeEvaluator.AcceptCloa.Should().Be(AcceptCloa.Yes, "Standard retains customer's prior CLOA acceptance");
        d.UWStateBeforeEvaluator.RcmpOption.Should().Be(RcmpOption.Option2, "Standard retains RcmpOption");
        d.DecisionSpecificLetters.Should().BeEmpty();
    }

    [Fact]
    public void Substandard_ClearsBothAcceptCloaAndRcmpOption()
    {
        var d = _sut.Handle(NewPolicy(AcceptCloa.Yes, RcmpOption.Option1), UwDecision.Substandard);

        d.OverrideNextSubstatus.Should().BeNull(
            "evaluator determines the substatus — with cleared AcceptCloa it will return CondAccept");
        d.UWStateBeforeEvaluator.AcceptCloa.Should().Be(AcceptCloa.Blank,
            "1.5.3-specific: Substandard AUTO CLEARS AcceptCloa (source line 575)");
        d.UWStateBeforeEvaluator.RcmpOption.Should().Be(RcmpOption.Blank,
            "1.5.3-specific: Substandard AUTO CLEARS RcmpOption (source line 575)");
        d.DecisionSpecificLetters.Should().BeEmpty(
            "the evaluator emits the CLOA + Ack (main letter) — no handler extras");
    }

    [Theory]
    [InlineData(UwDecision.Declined,   LetterType.Decline)]
    [InlineData(UwDecision.Postponed,  LetterType.Postponement)]
    [InlineData(UwDecision.NotTakenUp, LetterType.NtuWithoutRefund)]
    public void DeclinePostponeNtu_EmitLetter_RetainCustomerChoices(UwDecision decision, LetterType expected)
    {
        var d = _sut.Handle(NewPolicy(AcceptCloa.Yes, RcmpOption.Option1), decision);

        d.DecisionSpecificLetters.Should().ContainSingle().Which.Should().Be(expected);
        d.UWStateBeforeEvaluator.AcceptCloa.Should().Be(AcceptCloa.Yes, "Decline/Postpone/NTU retain AcceptCloa");
        d.UWStateBeforeEvaluator.RcmpOption.Should().Be(RcmpOption.Option1, "Decline/Postpone/NTU retain RcmpOption");
    }

    [Fact]
    public void NullPolicy_Throws()
    {
        Action act = () => _sut.Handle(null!, UwDecision.Standard);
        act.Should().Throw<ArgumentNullException>();
    }

    private static Policy NewPolicy(AcceptCloa cloa, RcmpOption option) => new()
    {
        Id = 1,
        Substatus = PolicySubstatus.PendingCashCollection,
        UWState = new UWState { AcceptCloa = cloa, RcmpOption = option },
    };
}
