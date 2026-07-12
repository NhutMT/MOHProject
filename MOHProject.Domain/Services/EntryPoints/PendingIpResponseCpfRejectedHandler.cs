using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services.EntryPoints;

// Entry 1.5.5 — PENDING IP RESPONSE FILE (CPF REJECTED) (FR-AOR-054).
// Source: MOH_AdditionOfRiders_Analysis.html lines 595-606.
//
// Special rules this entry:
//   - APS keeps Base Plan premium allocation intact; only Linked Rider(s)
//     get recalculated (SkipBasePremiumRecalc = true).
//   - Substandard clears AcceptCloa + RcmpOption.
//   - Declined / Postponed / NTU keep the same substatus (no advance).
public sealed class PendingIpResponseCpfRejectedHandler : IEntryPointHandler
{
    public PolicySubstatus EntrySubstatus => PolicySubstatus.PendingIpResponseFileCpfRejected;

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
                SkipBasePremiumRecalc: true),

            UwDecision.Standard => Defer(uwState),

            UwDecision.Substandard => Defer(WithClearedAcceptCloaAndRcmpOption(uwState)),

            UwDecision.Declined  => Stay(uwState, extra: LetterType.Decline),
            UwDecision.Postponed => Stay(uwState, extra: LetterType.Postponement),
            UwDecision.NotTakenUp => Stay(uwState, extra: LetterType.NtuWithoutRefund),

            _ => throw new InvalidOperationException($"Unhandled UwDecision: {decision}"),
        };
    }

    private static EntryPointDirective Defer(UWState uwState) => new(
        OverrideNextSubstatus: null,
        UWStateBeforeEvaluator: uwState,
        DecisionSpecificLetters: Array.Empty<LetterType>(),
        AutoCreateIpRecord: false,
        AutoRemoveIpRecord: false,
        SkipBasePremiumRecalc: false);

    private static EntryPointDirective Stay(UWState uwState, LetterType extra) => new(
        OverrideNextSubstatus: PolicySubstatus.PendingIpResponseFileCpfRejected,
        UWStateBeforeEvaluator: uwState,
        DecisionSpecificLetters: new[] { extra },
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
