using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;

namespace MOHProject.Tests.Unit.Domain.Services.EntryPoints;

public class CondAcceptLetterGenHandlerTests
{
    private readonly CondAcceptLetterGenHandler _sut = new();

    [Fact]
    public void EntrySubstatus_Is_ConditionalAcceptanceLetterGenerated()
    {
        _sut.EntrySubstatus.Should().Be(PolicySubstatus.ConditionalAcceptanceLetterGenerated);
    }

    [Fact]
    public void Aps_OverridesToPendingUwAps_WithMedicalEvidenceLetter_NoEvaluatorNeeded()
    {
        var policy = NewPolicy(AcceptCloa.Yes);

        var d = _sut.Handle(policy, UwDecision.Aps);

        d.OverrideNextSubstatus.Should().Be(PolicySubstatus.PendingUwAps,
            "APS decision goes directly to PendingUwAps — composition hasn't changed, skip evaluator");
        d.DecisionSpecificLetters.Should().ContainSingle().Which.Should().Be(LetterType.MedicalEvidence);
        d.UWStateBeforeEvaluator.AcceptCloa.Should().Be(AcceptCloa.Yes,
            "APS does not clear AcceptCloa");
    }

    [Fact]
    public void Standard_DefersToEvaluator_NoExtraLetters()
    {
        var policy = NewPolicy(AcceptCloa.Yes);

        var d = _sut.Handle(policy, UwDecision.Standard);

        d.OverrideNextSubstatus.Should().BeNull("Standard defers substatus to RemainingPlansEvaluator");
        d.DecisionSpecificLetters.Should().BeEmpty(
            "no decision-specific letter for Standard — evaluator emits the main letter");
        d.UWStateBeforeEvaluator.AcceptCloa.Should().Be(AcceptCloa.Yes, "Standard does not clear AcceptCloa");
    }

    [Fact]
    public void Substandard_ClearsAcceptCloa_DefersToEvaluator()
    {
        var policy = NewPolicy(AcceptCloa.Yes);

        var d = _sut.Handle(policy, UwDecision.Substandard);

        d.OverrideNextSubstatus.Should().BeNull();
        d.UWStateBeforeEvaluator.AcceptCloa.Should().Be(AcceptCloa.Blank,
            "FR-AOR-050: if new rider = Sub, clear Accept CLOA before evaluator (source line 549)");
        d.DecisionSpecificLetters.Should().BeEmpty(
            "evaluator produces the new CLOA (main letter) — handler adds no extras for Substandard");
    }

    [Theory]
    [InlineData(UwDecision.Declined,  LetterType.Decline)]
    [InlineData(UwDecision.Postponed, LetterType.Postponement)]
    [InlineData(UwDecision.NotTakenUp, LetterType.NtuWithoutRefund)]
    public void DeclinePostponeNtu_EmitDecisionSpecificLetter_DeferSubstatusToEvaluator(
        UwDecision decision, LetterType expectedExtra)
    {
        var policy = NewPolicy(AcceptCloa.Yes);

        var d = _sut.Handle(policy, decision);

        d.OverrideNextSubstatus.Should().BeNull(
            "Decline/Postpone/NTU: composition changes → let evaluator decide substatus + main LOA/CLOA");
        d.DecisionSpecificLetters.Should().ContainSingle().Which.Should().Be(expectedExtra);
        d.UWStateBeforeEvaluator.AcceptCloa.Should().Be(AcceptCloa.Yes,
            "Decline/Postpone/NTU do not clear AcceptCloa at 1.5.1 (evaluator's rules apply)");
    }

    [Fact]
    public void NullPolicy_Throws()
    {
        Action act = () => _sut.Handle(null!, UwDecision.Standard);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PolicyWithoutUWState_Throws()
    {
        var policy = new Policy { Id = 99 };
        Action act = () => _sut.Handle(policy, UwDecision.Standard);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Policy 99*UWState*");
    }

    private static Policy NewPolicy(AcceptCloa cloa) => new()
    {
        Id = 1,
        Substatus = PolicySubstatus.ConditionalAcceptanceLetterGenerated,
        UWState = new UWState { AcceptCloa = cloa },
    };
}
