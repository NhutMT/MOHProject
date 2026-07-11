using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

public interface ILetterTypeEvaluator
{
    LetterDecision Evaluate(RiskComposition composition, UWState uwState, PolicySubstatus currentSubstatus);
}

public sealed record LetterDecision(LetterType? Type, bool HasAcknowledgementPage);
