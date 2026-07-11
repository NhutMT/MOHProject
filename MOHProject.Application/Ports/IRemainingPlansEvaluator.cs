using MOHProject.Domain.Entities;

namespace MOHProject.Application.Ports;

public interface IRemainingPlansEvaluator
{
    EvaluationResult EvaluateAfterAction(Policy policy, PolicyContext context);
}
