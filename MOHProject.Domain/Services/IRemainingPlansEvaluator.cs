using MOHProject.Domain.Entities;

namespace MOHProject.Domain.Services;

public interface IRemainingPlansEvaluator
{
    EvaluationResult EvaluateAfterAction(Policy policy, PolicyContext context);
}
