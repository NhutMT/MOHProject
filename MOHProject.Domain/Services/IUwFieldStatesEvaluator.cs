using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

public interface IUwFieldStatesEvaluator
{
    UWState Evaluate(UWState current, RiskComposition composition, PolicyContext context);
}
