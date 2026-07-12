using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;

namespace MOHProject.Tests.Unit.Domain.Services.EntryPoints;

public class PendingIpRequestFileHandlerTests
{
    private readonly PendingIpRequestFileHandler _sut = new();

    [Fact]
    public void EntrySubstatus_Is_PendingIpRequestFile()
    {
        _sut.EntrySubstatus.Should().Be(PolicySubstatus.PendingIpRequestFile);
    }

    [Fact]
    public void Aps_AutoRemovesIpRecord_AndOverridesToPendingUwAps()
    {
        var d = _sut.Handle(NewPolicy(), UwDecision.Aps);

        d.OverrideNextSubstatus.Should().Be(PolicySubstatus.PendingUwAps);
        d.AutoRemoveIpRecord.Should().BeTrue(
            "1.5.4 APS auto-removes IP record 045G from CPF tab (source line 586)");
        d.AutoCreateIpRecord.Should().BeFalse();
        d.DecisionSpecificLetters.Should().ContainSingle().Which.Should().Be(LetterType.MedicalEvidence);
    }

    [Fact]
    public void Standard_Defers_NoIpRecordAction()
    {
        var d = _sut.Handle(NewPolicy(), UwDecision.Standard);

        d.OverrideNextSubstatus.Should().BeNull();
        d.AutoCreateIpRecord.Should().BeFalse();
        d.AutoRemoveIpRecord.Should().BeFalse();
    }

    [Fact]
    public void Substandard_ClearsBothFields_NoIpRecordAction()
    {
        var uwState = new UWState { AcceptCloa = AcceptCloa.Yes, RcmpOption = RcmpOption.Option1 };
        var policy = new Policy { Id = 1, UWState = uwState };

        var d = _sut.Handle(policy, UwDecision.Substandard);

        d.UWStateBeforeEvaluator.AcceptCloa.Should().Be(AcceptCloa.Blank,
            "1.5.4 Substandard CLEARS AcceptCloa (source line 588)");
        d.UWStateBeforeEvaluator.RcmpOption.Should().Be(RcmpOption.Blank,
            "1.5.4 Substandard CLEARS RcmpOption");
        d.AutoCreateIpRecord.Should().BeFalse();
    }

    [Theory]
    [InlineData(UwDecision.Declined,   LetterType.Decline)]
    [InlineData(UwDecision.Postponed,  LetterType.Postponement)]
    [InlineData(UwDecision.NotTakenUp, LetterType.NtuWithoutRefund)]
    public void DeclinePostponeNtu_StaysAtPendingIpRequest_AutoCreatesIpRecord(UwDecision decision, LetterType expected)
    {
        var d = _sut.Handle(NewPolicy(), decision);

        d.OverrideNextSubstatus.Should().Be(PolicySubstatus.PendingIpRequestFile,
            "Decline/Postpone/NTU at 1.5.4 stay at PendingIpRequestFile per doc (source lines 589-591)");
        d.AutoCreateIpRecord.Should().BeTrue(
            "1.5.4 Decline/Postpone/NTU auto-create IP record in CPF tab");
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
        Substatus = PolicySubstatus.PendingIpRequestFile,
        UWState = new UWState(),
    };
}
