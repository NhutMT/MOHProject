using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services.EntryPoints;

// Entry 1.5.2 — PENDING UW CLOA ASSESSMENT (FR-AOR-051).
// Source: MOH_AdditionOfRiders_Analysis.html lines 556-567.
//
// Difference from 1.5.1: doc does NOT specify "clear AcceptCloa if new rider = Sub"
// for this entry — the Substandard branch just defers to the evaluator without
// modifying AcceptCloa.
public sealed class PendingUwCloaAssessmentHandler : IEntryPointHandler
{
    public PolicySubstatus EntrySubstatus => PolicySubstatus.PendingUwCloaAssessment;

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

            UwDecision.Standard    => Defer(uwState),
            UwDecision.Substandard => Defer(uwState),
            UwDecision.Declined    => Defer(uwState, extra: LetterType.Decline),
            UwDecision.Postponed   => Defer(uwState, extra: LetterType.Postponement),
            UwDecision.NotTakenUp  => Defer(uwState, extra: LetterType.NtuWithoutRefund),

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
}
