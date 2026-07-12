using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

// Handler output describing what a UW decision at a given entry substatus does.
// See docs/specs/sections/section-1-addition-of-riders.md FR-AOR-050..054.
//
// The command layer applies the directive in this order:
//   1. Apply plan-status side effect if decision was NTU/Declined/Postponed on the newly-added rider.
//   2. Set policy.UWState = UWStateBeforeEvaluator (pre-evaluator modifications, e.g. 1.5.3 Sub clears AcceptCloa).
//   3. Emit DecisionSpecificLetters (Medical Evidence, Decline, Postpone, NTU letters).
//   4. Process IP-record side effects (Create/Remove — 1.5.4-specific).
//   5. If OverrideNextSubstatus is set, use it and skip RemainingPlansEvaluator.
//      Else call RemainingPlansEvaluator → its EvaluationResult supplies the next substatus + main letter.
public sealed record EntryPointDirective(
    PolicySubstatus? OverrideNextSubstatus,
    UWState UWStateBeforeEvaluator,
    IReadOnlyList<LetterType> DecisionSpecificLetters,
    bool AutoCreateIpRecord,
    bool AutoRemoveIpRecord,
    bool SkipBasePremiumRecalc);
