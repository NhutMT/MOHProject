using Microsoft.Extensions.Logging.Abstractions;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Tests.Regression.UatBugs;

// Shared helpers for the 4 UAT bug regression tests.
// Each concrete test reconstructs the "Setup" row from BUG-uat-2026-06.md,
// invokes the evaluator, and asserts the "Expected" outcome.
public abstract class UatBugTestBase
{
    protected readonly IRemainingPlansEvaluator Sut = new RemainingPlansEvaluator(
        new PlansCompositionEvaluator(),
        new UwFieldStatesEvaluator(),
        new NextSubstatusEvaluator(),
        new LetterTypeEvaluator(),
        NullLogger<RemainingPlansEvaluator>.Instance);

    protected static Plan BasePlan(bool loading = false, bool exclusion = false, ProductStatus status = ProductStatus.Active) => new()
    {
        IsBase = true,
        ProductCode = "Base",
        Status = status,
        HasActiveRiskLoading = loading,
        HasActiveExclusion = exclusion,
    };

    protected static Plan Rider(string productCode, bool loading, bool exclusion, ProductStatus status) => new()
    {
        IsBase = false,
        ProductCode = productCode,
        Status = status,
        HasActiveRiskLoading = loading,
        HasActiveExclusion = exclusion,
    };

    protected static PolicyContext SgSgContext(decimal shortfall) => new(
        new ResidencyPair(Residency.Sg, Residency.Sg),
        new Money(shortfall),
        IsRenewal: false,
        BaseHasRiskLoading: false);
}
