using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;

namespace MOHProject.Tests.Unit.Domain.Services.EntryPoints;

public class PendingUwCloaAssessmentHandlerTests
{
    private readonly PendingUwCloaAssessmentHandler _sut = new();

    [Fact]
    public void EntrySubstatus_Is_PendingUwCloaAssessment()
    {
        _sut.EntrySubstatus.Should().Be(PolicySubstatus.PendingUwCloaAssessment);
    }

    [Fact]
    public void Aps_OverridesToPendingUwAps_WithMedicalEvidence()
    {
        var d = _sut.Handle(NewPolicy(AcceptCloa.Yes), UwDecision.Aps);

        d.OverrideNextSubstatus.Should().Be(PolicySubstatus.PendingUwAps);
        d.DecisionSpecificLetters.Should().ContainSingle().Which.Should().Be(LetterType.MedicalEvidence);
    }

    [Theory]
    [InlineData(UwDecision.Standard)]
    [InlineData(UwDecision.Substandard)]
    public void StandardOrSubstandard_DoNotModifyAcceptCloa(UwDecision decision)
    {
        var d = _sut.Handle(NewPolicy(AcceptCloa.Yes), decision);

        d.UWStateBeforeEvaluator.AcceptCloa.Should().Be(AcceptCloa.Yes,
            "unlike entry 1.5.1, entry 1.5.2 does NOT clear AcceptCloa on Substandard decision (doc silent → no clear)");
        d.DecisionSpecificLetters.Should().BeEmpty(
            "evaluator produces the main letter (CLOA per composition)");
        d.OverrideNextSubstatus.Should().BeNull();
    }

    [Theory]
    [InlineData(UwDecision.Declined,   LetterType.Decline)]
    [InlineData(UwDecision.Postponed,  LetterType.Postponement)]
    [InlineData(UwDecision.NotTakenUp, LetterType.NtuWithoutRefund)]
    public void DeclinePostponeNtu_EmitDecisionSpecificLetter(UwDecision decision, LetterType expected)
    {
        var d = _sut.Handle(NewPolicy(AcceptCloa.Blank), decision);

        d.DecisionSpecificLetters.Should().ContainSingle().Which.Should().Be(expected);
        d.OverrideNextSubstatus.Should().BeNull();
    }

    [Fact]
    public void NullPolicy_Throws()
    {
        Action act = () => _sut.Handle(null!, UwDecision.Standard);
        act.Should().Throw<ArgumentNullException>();
    }

    private static Policy NewPolicy(AcceptCloa cloa) => new()
    {
        Id = 1,
        Substatus = PolicySubstatus.PendingUwCloaAssessment,
        UWState = new UWState { AcceptCloa = cloa },
    };
}
