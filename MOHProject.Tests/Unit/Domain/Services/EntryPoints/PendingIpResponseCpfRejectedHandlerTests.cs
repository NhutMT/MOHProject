using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;

namespace MOHProject.Tests.Unit.Domain.Services.EntryPoints;

public class PendingIpResponseCpfRejectedHandlerTests
{
    private readonly PendingIpResponseCpfRejectedHandler _sut = new();

    [Fact]
    public void EntrySubstatus_Is_PendingIpResponseFileCpfRejected()
    {
        _sut.EntrySubstatus.Should().Be(PolicySubstatus.PendingIpResponseFileCpfRejected);
    }

    [Fact]
    public void Aps_SkipsBasePremiumRecalc_OverridesToPendingUwAps()
    {
        var d = _sut.Handle(NewPolicy(), UwDecision.Aps);

        d.OverrideNextSubstatus.Should().Be(PolicySubstatus.PendingUwAps);
        d.SkipBasePremiumRecalc.Should().BeTrue(
            "1.5.5 APS: Base Plan premium allocation NOT recalculated — only Linked Riders (source line 599)");
        d.DecisionSpecificLetters.Should().ContainSingle().Which.Should().Be(LetterType.MedicalEvidence);
    }

    [Fact]
    public void Standard_Defers_NoSkipFlag()
    {
        var d = _sut.Handle(NewPolicy(), UwDecision.Standard);

        d.OverrideNextSubstatus.Should().BeNull();
        d.SkipBasePremiumRecalc.Should().BeFalse();
    }

    [Fact]
    public void Substandard_ClearsBothFields()
    {
        var policy = new Policy
        {
            Id = 1,
            UWState = new UWState { AcceptCloa = AcceptCloa.Yes, RcmpOption = RcmpOption.Option1 },
        };

        var d = _sut.Handle(policy, UwDecision.Substandard);

        d.UWStateBeforeEvaluator.AcceptCloa.Should().Be(AcceptCloa.Blank,
            "1.5.5 Substandard clears AcceptCloa (source line 601)");
        d.UWStateBeforeEvaluator.RcmpOption.Should().Be(RcmpOption.Blank);
    }

    [Theory]
    [InlineData(UwDecision.Declined,   LetterType.Decline)]
    [InlineData(UwDecision.Postponed,  LetterType.Postponement)]
    [InlineData(UwDecision.NotTakenUp, LetterType.NtuWithoutRefund)]
    public void DeclinePostponeNtu_KeepsSameSubstatus_EmitsLetter(UwDecision decision, LetterType expected)
    {
        var d = _sut.Handle(NewPolicy(), decision);

        d.OverrideNextSubstatus.Should().Be(PolicySubstatus.PendingIpResponseFileCpfRejected,
            "1.5.5 Decline/Postpone/NTU keep substatus unchanged per doc: 'Giữ nguyên substatus' (source lines 602-604)");
        d.DecisionSpecificLetters.Should().ContainSingle().Which.Should().Be(expected);
    }

    [Fact]
    public void NullPolicy_Throws()
    {
        Action act = () => _sut.Handle(null!, UwDecision.Standard);
        act.Should().Throw<ArgumentNullException>();
    }

    private static Policy NewPolicy() => new()
    {
        Id = 1,
        Substatus = PolicySubstatus.PendingIpResponseFileCpfRejected,
        UWState = new UWState(),
    };
}
