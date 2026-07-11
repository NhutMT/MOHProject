using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

// Implements FR-AOR-030 step 3 (Scan remaining Active Plans → determine RiskComposition).
// Source: MOH_AdditionOfRiders_Analysis.html lines 279-307, 1901-1921.
public sealed class PlansCompositionEvaluator : IPlansCompositionEvaluator
{
    public RiskComposition Evaluate(IReadOnlyCollection<Plan> activePlans)
    {
        ArgumentNullException.ThrowIfNull(activePlans);

        if (activePlans.Count == 0)
            throw new InvalidOperationException(
                "Cannot evaluate composition of an empty active-plan set. Every policy must retain at least a Base plan.");

        // Priority: HasRcmp > ExclusionOnly > AllStandard.
        // "RCMP" at the plan level = Substandard with BOTH Risk Loading AND Exclusion.
        var hasRcmp = false;
        var hasExclusionOnly = false;

        foreach (var plan in activePlans)
        {
            if (plan.HasActiveRiskLoading && plan.HasActiveExclusion)
            {
                hasRcmp = true;
                break;
            }

            if (plan.HasActiveExclusion && !plan.HasActiveRiskLoading)
                hasExclusionOnly = true;
        }

        if (hasRcmp) return RiskComposition.HasRcmp;
        if (hasExclusionOnly) return RiskComposition.ExclusionOnly;
        return RiskComposition.AllStandard;
    }
}
