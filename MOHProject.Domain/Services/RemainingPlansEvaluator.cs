using Microsoft.Extensions.Logging;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

// Orchestrator for FR-AOR-030. Composes 4 pure sub-evaluators to produce the
// full EvaluationResult after any NTU / Decline / Postpone / Re-add event.
// Fix root cause of BUG-2610000310P / 154P / 345P / 346P.
// Source: MOH_AdditionOfRiders_Analysis.html lines 611-909, 1862-1948.
public sealed class RemainingPlansEvaluator : IRemainingPlansEvaluator
{
    private readonly IPlansCompositionEvaluator _composition;
    private readonly IUwFieldStatesEvaluator _uwFields;
    private readonly INextSubstatusEvaluator _substatus;
    private readonly ILetterTypeEvaluator _letter;
    private readonly ILogger<RemainingPlansEvaluator> _logger;

    public RemainingPlansEvaluator(
        IPlansCompositionEvaluator composition,
        IUwFieldStatesEvaluator uwFields,
        INextSubstatusEvaluator substatus,
        ILetterTypeEvaluator letter,
        ILogger<RemainingPlansEvaluator> logger)
    {
        _composition = composition;
        _uwFields = uwFields;
        _substatus = substatus;
        _letter = letter;
        _logger = logger;
    }

    public EvaluationResult EvaluateAfterAction(Policy policy, PolicyContext context)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(context);

        if (policy.UWState is null)
            throw new InvalidOperationException(
                $"Policy {policy.Id} has no UWState. Every policy must have one — see domain-model.md.");

        var activePlans = policy.Plans
            .Where(p => p.Status == ProductStatus.Active)
            .ToArray();

        var composition = _composition.Evaluate(activePlans);
        var updatedUwState = _uwFields.Evaluate(policy.UWState, composition, context);
        var nextSubstatus = _substatus.Evaluate(composition, updatedUwState, context);
        var letterDecision = _letter.Evaluate(composition, updatedUwState, nextSubstatus);

        _logger.LogDebug(
            "Policy {PolicyId}: composition={Composition} nextSubstatus={Substatus} letter={LetterType} ack={HasAck} activePlans={ActiveCount}",
            policy.Id, composition, nextSubstatus, letterDecision.Type, letterDecision.HasAcknowledgementPage, activePlans.Length);

        return new EvaluationResult(
            composition,
            nextSubstatus,
            letterDecision.Type,
            letterDecision.HasAcknowledgementPage,
            updatedUwState);
    }
}
