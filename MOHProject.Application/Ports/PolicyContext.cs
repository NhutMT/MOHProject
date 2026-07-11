using MOHProject.Domain.ValueObjects;

namespace MOHProject.Application.Ports;

public sealed record PolicyContext(
    ResidencyPair Residency,
    Money CashShortfall,
    bool IsRenewal,
    bool BaseHasRiskLoading);
