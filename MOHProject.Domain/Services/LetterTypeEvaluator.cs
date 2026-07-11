using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

// Implements FR-AOR-030 step 4 (new LOA/CLOA generation, substatus-gated) and
// FR-LTR-COMBO-020 (composition → letter type + AcceptCloa → hasAck).
// Source: MOH_AdditionOfRiders_Analysis.html lines 271, 279-307, 1841-1859.
public sealed class LetterTypeEvaluator : ILetterTypeEvaluator
{
    private static readonly HashSet<PolicySubstatus> LetterGeneratingSubstatuses = new()
    {
        PolicySubstatus.ConditionalAcceptanceLetterGenerated,
        PolicySubstatus.PendingUwCloaAssessment,
        PolicySubstatus.PendingCashCollection,
    };

    public LetterDecision Evaluate(RiskComposition composition, UWState uwState, PolicySubstatus currentSubstatus)
    {
        ArgumentNullException.ThrowIfNull(uwState);

        // Substatus-gated skip (source line 271):
        // Letter is only re-generated when the current substatus is one where
        // a customer-facing letter belongs. Other substatuses (PendingManualUw,
        // PendingUwAps, PendingIpRequestFile, PendingIpResponseFileCpfRejected)
        // let UW/CPF complete before a fresh letter is issued.
        if (!LetterGeneratingSubstatuses.Contains(currentSubstatus))
            return new LetterDecision(null, false);

        var type = composition switch
        {
            RiskComposition.AllStandard => LetterType.Loa,
            RiskComposition.ExclusionOnly => LetterType.CloaExclusion,
            RiskComposition.HasRcmp => LetterType.CloaRcmp,
            _ => throw new InvalidOperationException($"Unhandled composition: {composition}"),
        };

        // Ack page rule (FR-LTR-COMBO-010, source lines 1807-1839):
        // - LOA never has an Ack page (no conditions to acknowledge).
        // - CLOA needs Ack when the customer has NOT yet accepted (AcceptCloa = Blank).
        //   If AcceptCloa = Yes, the letter is informational (a superseding CLOA) and skips Ack.
        var hasAck = type != LetterType.Loa && uwState.AcceptCloa != AcceptCloa.Yes;

        return new LetterDecision(type, hasAck);
    }
}
