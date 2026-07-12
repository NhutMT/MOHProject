using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Tests.Regression.UatBugs;

// BUG-2610000154P — RCMP fields not auto-reset after NTU RCMP rider (Staging).
// Ref: docs/specs/bugs/uat-2026-06.md.
public class Bug_2610000154P_Test : UatBugTestBase
{
    [Fact]
    public void NtuChoiceRcmp_ResetsRcmpFieldsAndYieldsPendingIpRequest()
    {
        // Setup: Base=Std, Choice=RCMP, NB CLOA(RCMP) issued, UW chose Option 1,
        // AcceptCloa=Yes, substatus=PendingCashCollection.
        var policy = new Policy
        {
            Id = 2610000154,
            PolicyNumber = "2610000154P",
            Substatus = PolicySubstatus.PendingCashCollection,
            UWState = new UWState
            {
                RcmpFlag = true,
                RcmpFlagEnabled = true,
                AcceptCloa = AcceptCloa.Yes,
                AcceptCloaEnabled = true,
                RcmpOption = RcmpOption.Option1,
                RcmpOptionEnabled = true,
                CompleteUw = true,
            },
        };
        policy.Plans.Add(BasePlan());
        // The NTU action on Choice (was RCMP):
        policy.Plans.Add(Rider("Choice", loading: true, exclusion: true, status: ProductStatus.NotTakenUp));

        // No shortfall for this scenario (per doc — SG/PR × SG/PR clean).
        var result = Sut.EvaluateAfterAction(policy, SgSgContext(shortfall: 0m));

        // Expected: only Base=Standard remains active → AllStandard.
        // SG/PR × SG/PR, no shortfall → PendingIpRequestFile.
        // RCMP fields all reset to blank+greyed.
        result.Composition.Should().Be(RiskComposition.AllStandard);
        result.NextSubstatus.Should().Be(PolicySubstatus.PendingIpRequestFile,
            "no shortfall + SG/PR × SG/PR + AllStandard → PendingIpRequestFile (BUG was: stuck at PendingCashCollection)");

        result.UpdatedUWState.RcmpFlag.Should().BeFalse(
            "BUG was: RCMP flag remained ticked");
        result.UpdatedUWState.RcmpFlagEnabled.Should().BeFalse();
        result.UpdatedUWState.AcceptCloa.Should().Be(AcceptCloa.Blank,
            "BUG was: AcceptCloa remained Yes");
        result.UpdatedUWState.AcceptCloaEnabled.Should().BeFalse();
        result.UpdatedUWState.RcmpOption.Should().Be(RcmpOption.Blank,
            "BUG was: RCMP Option remained Option1");
        result.UpdatedUWState.RcmpOptionEnabled.Should().BeFalse();
    }
}
