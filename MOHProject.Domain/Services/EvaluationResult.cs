using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

public sealed record EvaluationResult(
    RiskComposition Composition,
    PolicySubstatus NextSubstatus,
    LetterType? LetterToGenerate,
    bool LetterHasAcknowledgementPage,
    UWState UpdatedUWState);
