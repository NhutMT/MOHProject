using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

// Implements FR-AOR-031 (RCMP field state per composition) and
// FR-AOR-060 (Renewal + Base ExtraLoading + CGR/Choice → RCMP retained).
// Source: MOH_AdditionOfRiders_Analysis.html lines 279-307, 451-457.
public sealed class UwFieldStatesEvaluator : IUwFieldStatesEvaluator
{
    public UWState Evaluate(UWState current, RiskComposition composition, PolicyContext context)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(context);

        var next = new UWState { CurrentComposition = composition };

        // Renewal exception (FR-AOR-060 · source lines 451-457):
        // Renewal + Base has Risk Loading → RCMP flag retained, no matter the composition.
        // Reason: Base itself already carries Loading, composition remains HasRcmp
        // conceptually even if rider set is Standard/Exclusion; no need to re-underwrite.
        if (context.IsRenewal && context.BaseHasRiskLoading)
        {
            next.RcmpFlag = true;
            next.RcmpFlagEnabled = true;
            next.AcceptCloa = current.AcceptCloa;
            next.AcceptCloaEnabled = true;
            next.RcmpOption = current.RcmpOption;
            next.RcmpOptionEnabled = true;
            next.CompleteUw = true;
            return next;
        }

        switch (composition)
        {
            case RiskComposition.AllStandard:
                // FR-AOR-031 row 1: all fields greyed + blank; CompleteUW auto-selected.
                next.RcmpFlag = false;
                next.RcmpFlagEnabled = false;
                next.AcceptCloa = AcceptCloa.Blank;
                next.AcceptCloaEnabled = false;
                next.RcmpOption = RcmpOption.Blank;
                next.RcmpOptionEnabled = false;
                next.CompleteUw = true;
                break;

            case RiskComposition.ExclusionOnly:
                // FR-AOR-031 row 2: RCMP flag greyed; AcceptCloa retained if Yes; RcmpOption greyed.
                next.RcmpFlag = false;
                next.RcmpFlagEnabled = false;
                next.AcceptCloa = current.AcceptCloa;
                next.AcceptCloaEnabled = current.AcceptCloa == AcceptCloa.Yes;
                next.RcmpOption = RcmpOption.Blank;
                next.RcmpOptionEnabled = false;
                next.CompleteUw = true;
                break;

            case RiskComposition.HasRcmp:
                // FR-AOR-031 row 3: everything retained + enabled.
                next.RcmpFlag = true;
                next.RcmpFlagEnabled = true;
                next.AcceptCloa = current.AcceptCloa;
                next.AcceptCloaEnabled = true;
                next.RcmpOption = current.RcmpOption;
                next.RcmpOptionEnabled = true;
                next.CompleteUw = true;
                break;

            default:
                throw new InvalidOperationException($"Unhandled composition: {composition}");
        }

        return next;
    }
}
