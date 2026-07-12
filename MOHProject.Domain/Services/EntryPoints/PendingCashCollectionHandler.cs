using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services.EntryPoints;

// Entry 1.5.3 — PENDING CASH COLLECTION (FR-AOR-052).
// Source: MOH_AdditionOfRiders_Analysis.html lines 569-580.
//
// Special rule (this entry only): UW = Substandard AUTO CLEARS both
// AcceptCloa and RcmpOption before the evaluator runs. Doc treats this
// as a re-underwriting event that invalidates the prior customer choice.
public sealed class PendingCashCollectionHandler : IEntryPointHandler
{
    public PolicySubstatus EntrySubstatus => PolicySubstatus.PendingCashCollection;

    public EntryPointDirective Handle(Policy policy, UwDecision decision)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.UWState is null)
            throw new InvalidOperationException($"Policy {policy.Id} has no UWState.");

        var uwState = policy.UWState;

        return decision switch
        {
            UwDecision.Aps => new EntryPointDirective(
                OverrideNextSubstatus: PolicySubstatus.PendingUwAps,
                UWStateBeforeEvaluator: uwState,
                DecisionSpecificLetters: new[] { LetterType.MedicalEvidence },
                AutoCreateIpRecord: false,
                AutoRemoveIpRecord: false,
                SkipBasePremiumRecalc: false),

            UwDecision.Standard => Defer(uwState),

            // 1.5.3-specific: auto clear both AcceptCloa and RcmpOption.
            UwDecision.Substandard => Defer(WithClearedAcceptCloaAndRcmpOption(uwState)),

            UwDecision.Declined   => Defer(uwState, extra: LetterType.Decline),
            UwDecision.Postponed  => Defer(uwState, extra: LetterType.Postponement),
            UwDecision.NotTakenUp => Defer(uwState, extra: LetterType.NtuWithoutRefund),

            _ => throw new InvalidOperationException($"Unhandled UwDecision: {decision}"),
        };
    }

    private static EntryPointDirective Defer(UWState uwState, LetterType? extra = null) => new(
        OverrideNextSubstatus: null,
        UWStateBeforeEvaluator: uwState,
        DecisionSpecificLetters: extra is null ? Array.Empty<LetterType>() : new[] { extra.Value },
        AutoCreateIpRecord: false,
        AutoRemoveIpRecord: false,
        SkipBasePremiumRecalc: false);

    private static UWState WithClearedAcceptCloaAndRcmpOption(UWState original) => new()
    {
        Id = original.Id,
        RcmpFlag = original.RcmpFlag,
        RcmpFlagEnabled = original.RcmpFlagEnabled,
        AcceptCloa = AcceptCloa.Blank,
        AcceptCloaEnabled = original.AcceptCloaEnabled,
        RcmpOption = RcmpOption.Blank,
        RcmpOptionEnabled = original.RcmpOptionEnabled,
        CompleteUw = original.CompleteUw,
        CurrentComposition = original.CurrentComposition,
    };
}
