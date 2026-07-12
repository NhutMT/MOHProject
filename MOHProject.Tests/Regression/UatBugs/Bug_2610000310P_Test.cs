using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Tests.Regression.UatBugs;

// BUG-2610000310P — Substatus stuck after NTU Exclusion rider (UAT).
// Ref: docs/specs/bugs/uat-2026-06.md · Setup / Action / Expected rows.
public class Bug_2610000310P_Test : UatBugTestBase
{
    [Fact]
    public void NtuPremierExclusion_YieldsPendingCashAndNewLoa()
    {
        // Setup: Base=Std, Premier=Exclusion (→ CLOA(Exclusion) issued),
        // added Choice Rider (Decline/Standard) + Cancer Guard (Standard).
        // Substatus starts at ConditionalAcceptanceLetterGenerated (CLOA issued).
        var policy = new Policy
        {
            Id = 2610000310,
            PolicyNumber = "2610000310P",
            Substatus = PolicySubstatus.ConditionalAcceptanceLetterGenerated,
            UWState = new UWState
            {
                AcceptCloa = AcceptCloa.Yes,
                RcmpFlag = false,
                RcmpOption = RcmpOption.Blank,
            },
        };
        policy.Plans.Add(BasePlan());
        policy.Plans.Add(Rider("Premier",     loading: false, exclusion: true,  status: ProductStatus.NotTakenUp));  // <-- the NTU action
        policy.Plans.Add(Rider("Choice",      loading: false, exclusion: false, status: ProductStatus.Declined));    // Choice was Declined by UW
        policy.Plans.Add(Rider("CancerGuard", loading: false, exclusion: false, status: ProductStatus.Active));

        // Positive shortfall present at the time of NTU (typical AOR scenario).
        var result = Sut.EvaluateAfterAction(policy, SgSgContext(shortfall: 250m));

        // Expected: after removing Premier (Exclusion), remaining Active plans
        // are Base+CancerGuard, both Standard → composition = AllStandard.
        // Shortfall > 0 → PendingCashCollection.
        // At AllStandard, RCMP fields greyed, CompleteUw auto.
        // Letter type = LOA (would be new NB LOA).
        result.Composition.Should().Be(RiskComposition.AllStandard,
            "NTU Premier removes the only Substandard plan → remaining Base+CGR both Standard");
        result.NextSubstatus.Should().Be(PolicySubstatus.PendingCashCollection,
            "shortfall > 0 → PendingCashCollection (BUG was: stuck at CondAccept)");
        result.UpdatedUWState.AcceptCloaEnabled.Should().BeFalse(
            "AllStandard → Accept CLOA field greyed (BUG was: still enabled)");
        result.UpdatedUWState.RcmpFlag.Should().BeFalse();
        result.UpdatedUWState.RcmpFlagEnabled.Should().BeFalse();
        result.UpdatedUWState.CompleteUw.Should().BeTrue();
        result.LetterToGenerate.Should().Be(LetterType.Loa,
            "PendingCashCollection is a letter-generating substatus + AllStandard → LOA (BUG was: no new letter)");
    }
}
