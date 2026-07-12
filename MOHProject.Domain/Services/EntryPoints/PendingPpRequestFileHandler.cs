using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services.EntryPoints;

// Entry: PENDING PP REQUEST FILE — NTU-only entry substatus.
// Source: MOH_AdditionOfRiders_Analysis.html lines 242-243 · FR-AOR-070.
// Only NTU is allowed from this substatus; other UW decisions are rejected.
public sealed class PendingPpRequestFileHandler : IEntryPointHandler
{
    public PolicySubstatus EntrySubstatus => PolicySubstatus.PendingPpRequestFile;

    public EntryPointDirective Handle(Policy policy, UwDecision decision)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.UWState is null)
            throw new InvalidOperationException($"Policy {policy.Id} has no UWState.");

        if (decision != UwDecision.NotTakenUp)
            throw new InvalidOperationException(
                $"UW decision '{decision}' is not allowed from entry substatus {EntrySubstatus}. " +
                "Only NTU is permitted here per FR-AOR-070 (source lines 242-243).");

        return new EntryPointDirective(
            OverrideNextSubstatus: null,
            UWStateBeforeEvaluator: policy.UWState,
            DecisionSpecificLetters: new[] { LetterType.NtuWithoutRefund },
            AutoCreateIpRecord: false,
            AutoRemoveIpRecord: false,
            SkipBasePremiumRecalc: false);
    }
}
