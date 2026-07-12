using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Tests.Regression.UatBugs;

// BUG-2610000345P — Complete UW button not auto-selected after NTU (UAT).
// Ref: docs/specs/bugs/uat-2026-06.md.
public class Bug_2610000345P_Test : UatBugTestBase
{
    [Fact]
    public void NtuPremier_AutoSelectsCompleteUw_AndYieldsPendingCash()
    {
        // Setup: Base=Std, Choice=Std, NB LOA issued.
        // Then Premier added with a Condition (Blood Disorder) → manual UW.
        // Substatus=PendingManualUnderwriting at time of NTU.
        var policy = new Policy
        {
            Id = 2610000345,
            PolicyNumber = "2610000345P",
            Substatus = PolicySubstatus.PendingManualUnderwriting,
            UWState = new UWState
            {
                RcmpFlag = false,
                AcceptCloa = AcceptCloa.Blank,
                RcmpOption = RcmpOption.Blank,
                CompleteUw = false, // BUG: was not auto-selecting
            },
        };
        policy.Plans.Add(BasePlan());
        policy.Plans.Add(Rider("Choice",  loading: false, exclusion: false, status: ProductStatus.Active));
        // The NTU action on Premier (was a candidate with medical Condition):
        policy.Plans.Add(Rider("Premier", loading: false, exclusion: false, status: ProductStatus.NotTakenUp));

        // Positive shortfall (typical for AOR with cash payment).
        var result = Sut.EvaluateAfterAction(policy, SgSgContext(shortfall: 100m));

        // Expected: NTU Premier leaves Base+Choice both Standard → AllStandard.
        // Shortfall > 0 → PendingCashCollection. CompleteUw auto-selected.
        result.Composition.Should().Be(RiskComposition.AllStandard);
        result.NextSubstatus.Should().Be(PolicySubstatus.PendingCashCollection,
            "BUG was: stuck at PendingManualUnderwriting");
        result.UpdatedUWState.CompleteUw.Should().BeTrue(
            "BUG was: Complete UW button not selected; expected auto-select");
    }
}
