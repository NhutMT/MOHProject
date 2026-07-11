using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Application.Ports;

public sealed record EvaluationResult(
    RiskComposition Composition,
    PolicySubstatus NextSubstatus,
    LetterType? LetterToGenerate,
    bool LetterHasAcknowledgementPage,
    UWState UpdatedUWState);
