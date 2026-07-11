using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

public interface INextSubstatusEvaluator
{
    PolicySubstatus Evaluate(RiskComposition composition, UWState uwState, PolicyContext context);
}
