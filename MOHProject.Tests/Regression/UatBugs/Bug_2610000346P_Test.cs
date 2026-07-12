using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Tests.Regression.UatBugs;

// BUG-2610000346P — Substatus stuck after NTU Exclusion at COND. ACCEPT (UAT).
// Ref: docs/specs/bugs/uat-2026-06.md.
public class Bug_2610000346P_Test : UatBugTestBase
{
    [Fact]
    public void NtuChoiceExclusion_YieldsPendingCashAndNewLoa()
    {
        // Setup: Base=Std, CancerGuard=Std, added Choice (Exclusion).
        // Substatus=ConditionalAcceptanceLetterGenerated (CLOA(Exclusion) issued).
        var policy = new Policy
        {
            Id = 2610000346,
            PolicyNumber = "2610000346P",
            Substatus = PolicySubstatus.ConditionalAcceptanceLetterGenerated,
            UWState = new UWState
            {
                RcmpFlag = false,
                AcceptCloa = AcceptCloa.Blank, // customer hadn't accepted the CLOA yet
                RcmpOption = RcmpOption.Blank,
                CompleteUw = false,
            },
        };
        policy.Plans.Add(BasePlan());
        policy.Plans.Add(Rider("CancerGuard", loading: false, exclusion: false, status: ProductStatus.Active));
        // The NTU action on Choice (Exclusion):
        policy.Plans.Add(Rider("Choice",      loading: false, exclusion: true,  status: ProductStatus.NotTakenUp));

        var result = Sut.EvaluateAfterAction(policy, SgSgContext(shortfall: 150m));

        // Expected: NTU Choice removes the only Exclusion → Base+CGR both Standard → AllStandard.
        // Shortfall > 0 → PendingCashCollection. New LOA generated.
        result.Composition.Should().Be(RiskComposition.AllStandard);
        result.NextSubstatus.Should().Be(PolicySubstatus.PendingCashCollection,
            "BUG was: stuck at ConditionalAcceptanceLetterGenerated");
        result.UpdatedUWState.CompleteUw.Should().BeTrue();
        result.LetterToGenerate.Should().Be(LetterType.Loa,
            "AllStandard + letter-generating substatus (PendingCashCollection) → LOA (BUG was: no new NB LOA generated)");
    }
}
