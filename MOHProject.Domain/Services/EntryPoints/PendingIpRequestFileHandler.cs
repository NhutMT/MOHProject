using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services.EntryPoints;

// Entry 1.5.4 — PENDING IP REQUEST FILE (FR-AOR-053).
// Source: MOH_AdditionOfRiders_Analysis.html lines 582-593.
//
// Special rules this entry:
//   - APS auto-removes the IP record 045G from the CPF tab.
//   - Substandard CLEARS both AcceptCloa and RcmpOption.
//   - Declined / Postponed / NTU stay at PendingIpRequestFile and
//     auto-create an IP record in the CPF tab.
public sealed class PendingIpRequestFileHandler : IEntryPointHandler
{
    public PolicySubstatus EntrySubstatus => PolicySubstatus.PendingIpRequestFile;

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
                AutoRemoveIpRecord: true,
                SkipBasePremiumRecalc: false),

            UwDecision.Standard => Defer(uwState),

            UwDecision.Substandard => Defer(WithClearedAcceptCloaAndRcmpOption(uwState)),

            UwDecision.Declined  => StayAndCreateIpRecord(uwState, LetterType.Decline),
            UwDecision.Postponed => StayAndCreateIpRecord(uwState, LetterType.Postponement),
            UwDecision.NotTakenUp => StayAndCreateIpRecord(uwState, LetterType.NtuWithoutRefund),

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

    private static EntryPointDirective StayAndCreateIpRecord(UWState uwState, LetterType extra) => new(
        OverrideNextSubstatus: PolicySubstatus.PendingIpRequestFile,
        UWStateBeforeEvaluator: uwState,
        DecisionSpecificLetters: new[] { extra },
        AutoCreateIpRecord: true,
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
