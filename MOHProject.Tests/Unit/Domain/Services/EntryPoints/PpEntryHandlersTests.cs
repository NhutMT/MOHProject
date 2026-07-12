using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;

namespace MOHProject.Tests.Unit.Domain.Services.EntryPoints;

public class PpEntryHandlersTests
{
    public static IEnumerable<object[]> NtuOnlyHandlers => new object[][]
    {
        new object[] { (IEntryPointHandler)new PendingPpRequestFileHandler(),                PolicySubstatus.PendingPpRequestFile },
        new object[] { (IEntryPointHandler)new PendingPpResponseFileCpfRejectedHandler(),    PolicySubstatus.PendingPpResponseFileCpfRejected },
    };

    [Theory]
    [MemberData(nameof(NtuOnlyHandlers))]
    public void EntrySubstatus_Matches(IEntryPointHandler handler, PolicySubstatus expected)
    {
        handler.EntrySubstatus.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NtuOnlyHandlers))]
    public void Ntu_EmitsNtuLetter_AndDefers(IEntryPointHandler handler, PolicySubstatus _)
    {
        var policy = new Policy { Id = 1, UWState = new UWState() };

        var d = handler.Handle(policy, UwDecision.NotTakenUp);

        d.DecisionSpecificLetters.Should().ContainSingle().Which.Should().Be(LetterType.NtuWithoutRefund);
        d.OverrideNextSubstatus.Should().BeNull("evaluator determines final substatus after NTU");
    }

    [Theory]
    [InlineData(UwDecision.Aps)]
    [InlineData(UwDecision.Standard)]
    [InlineData(UwDecision.Substandard)]
    [InlineData(UwDecision.Declined)]
    [InlineData(UwDecision.Postponed)]
    public void NonNtuDecision_ThrowsOn_PendingPpRequestFile(UwDecision decision)
    {
        var handler = new PendingPpRequestFileHandler();
        var policy = new Policy { Id = 1, UWState = new UWState() };

        Action act = () => handler.Handle(policy, decision);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not allowed*NTU*",
                "FR-AOR-070: PENDING PP REQUEST FILE only permits NTU (source lines 242-243)");
    }

    [Theory]
    [InlineData(UwDecision.Aps)]
    [InlineData(UwDecision.Standard)]
    [InlineData(UwDecision.Substandard)]
    [InlineData(UwDecision.Declined)]
    [InlineData(UwDecision.Postponed)]
    public void NonNtuDecision_ThrowsOn_PendingPpResponseFileCpfRejected(UwDecision decision)
    {
        var handler = new PendingPpResponseFileCpfRejectedHandler();
        var policy = new Policy { Id = 1, UWState = new UWState() };

        Action act = () => handler.Handle(policy, decision);

        act.Should().Throw<InvalidOperationException>();
    }
}
