using MOHProject.Domain.ValueObjects;

namespace MOHProject.Domain.Services;

public sealed record PolicyContext(
    ResidencyPair Residency,
    Money CashShortfall,
    bool IsRenewal,
    bool BaseHasRiskLoading);
