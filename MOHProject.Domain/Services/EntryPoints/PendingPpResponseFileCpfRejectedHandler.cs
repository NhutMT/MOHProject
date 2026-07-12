using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services.EntryPoints;

// Entry: PENDING PP RESPONSE FILE (CPF REJECTED) — NTU-only entry substatus.
// Source: MOH_AdditionOfRiders_Analysis.html lines 242-243 · FR-AOR-070.
public sealed class PendingPpResponseFileCpfRejectedHandler : IEntryPointHandler
{
    public PolicySubstatus EntrySubstatus => PolicySubstatus.PendingPpResponseFileCpfRejected;

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
