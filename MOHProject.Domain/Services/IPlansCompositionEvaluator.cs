using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

public interface IPlansCompositionEvaluator
{
    RiskComposition Evaluate(IReadOnlyCollection<Plan> activePlans);
}
