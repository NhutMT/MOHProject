using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services.EntryPoints;

// Entry 1.5.1 — CONDITIONAL ACCEPTANCE LETTER GENERATED (FR-AOR-050).
// Source: MOH_AdditionOfRiders_Analysis.html lines 543-554.
public sealed class CondAcceptLetterGenHandler : IEntryPointHandler
{
    public PolicySubstatus EntrySubstatus => PolicySubstatus.ConditionalAcceptanceLetterGenerated;

    public EntryPointDirective Handle(Policy policy, UwDecision decision)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.UWState is null)
            throw new InvalidOperationException($"Policy {policy.Id} has no UWState.");

        var uwState = policy.UWState;

        return decision switch
        {
            // APS → PendingUwAps + Medical Evidence letter. Skips evaluator (no composition change).
            UwDecision.Aps => new EntryPointDirective(
                OverrideNextSubstatus: PolicySubstatus.PendingUwAps,
                UWStateBeforeEvaluator: uwState,
                DecisionSpecificLetters: new[] { LetterType.MedicalEvidence },
                AutoCreateIpRecord: false,
                AutoRemoveIpRecord: false,
                SkipBasePremiumRecalc: false),

            // Standard → defer to evaluator. Composition may have changed if this is a re-decision.
            UwDecision.Standard => Defer(uwState),

            // Substandard → clear AcceptCloa before evaluator. Doc: "if new rider = Sub → clear Accept CLOA".
            // UW=Substandard implies the newly-decided rider IS Sub, so we always clear.
            UwDecision.Substandard => Defer(WithClearedAcceptCloa(uwState)),

            // Declined / Postponed / NTU → decision-specific letter + evaluator.
            // Refund-vs-no-refund variant selection is Phase 4 (needs premium state).
            UwDecision.Declined  => Defer(uwState, extra: LetterType.Decline),
            UwDecision.Postponed => Defer(uwState, extra: LetterType.Postponement),
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

    private static UWState WithClearedAcceptCloa(UWState original) => new()
    {
        Id = original.Id,
        RcmpFlag = original.RcmpFlag,
        RcmpFlagEnabled = original.RcmpFlagEnabled,
        AcceptCloa = AcceptCloa.Blank,
        AcceptCloaEnabled = original.AcceptCloaEnabled,
        RcmpOption = original.RcmpOption,
        RcmpOptionEnabled = original.RcmpOptionEnabled,
        CompleteUw = original.CompleteUw,
        CurrentComposition = original.CurrentComposition,
    };
}
