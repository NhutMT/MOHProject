using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

// Implements FR-AOR-032: composition × residency × shortfall × AcceptCloa → PolicySubstatus.
// Source: MOH_AdditionOfRiders_Analysis.html lines 309-336, 1923-1948.
public sealed class NextSubstatusEvaluator : INextSubstatusEvaluator
{
    public PolicySubstatus Evaluate(RiskComposition composition, UWState uwState, PolicyContext context)
    {
        ArgumentNullException.ThrowIfNull(uwState);
        ArgumentNullException.ThrowIfNull(context);

        var hasShortfall = context.CashShortfall.IsPositive;

        // Composition AllStandard: no AcceptCloa needed (source lines 309-322).
        if (composition == RiskComposition.AllStandard)
        {
            if (hasShortfall)
                return PolicySubstatus.PendingCashCollection;

            return context.Residency.IsFrFr()
                ? PolicySubstatus.PolicyIncepted
                : PolicySubstatus.PendingIpRequestFile;
        }

        // Composition ExclusionOnly / HasRcmp: gated by AcceptCloa (source lines 324-335).
        if (uwState.AcceptCloa != AcceptCloa.Yes)
            return PolicySubstatus.ConditionalAcceptanceLetterGenerated;

        if (hasShortfall)
            return PolicySubstatus.PendingCashCollection;

        return context.Residency.IsFrFr()
            ? PolicySubstatus.PolicyIncepted
            : PolicySubstatus.PendingIpRequestFile;
    }
}
