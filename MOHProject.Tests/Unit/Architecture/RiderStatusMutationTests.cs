using MOHProject.Application.Features.AdditionOfRider;
using MOHProject.Domain.Services;
using NetArchTest.Rules;

namespace MOHProject.Tests.Unit.Architecture;

// PH-06-05 — safety net for FR-AOR-030 root fix.
//
// Rule: the set of Application command handlers that inject
// IRemainingPlansEvaluator is a declared canonical list. If a new handler
// starts (or stops) participating in the pipeline, this test fails and forces
// the change to be acknowledged in code.
//
// The canonical members split into two groups:
//   - Rider-lifecycle handlers (MarkRiderStatus, ReAddRider) — the ones that
//     mutate Plan.Status and MUST call the evaluator per FR-AOR-030.
//   - UwDecision — calls the evaluator in the non-override path to determine
//     final substatus + main letter.
public class RiderStatusMutationTests
{
    // Canonical set. Update in the same PR that changes the pipeline.
    private static readonly HashSet<string> CanonicalEvaluatorConsumers = new()
    {
        nameof(MarkRiderStatusCommandHandler),
        nameof(ReAddRiderCommandHandler),
        nameof(UwDecisionCommandHandler),
    };

    [Fact]
    public void ExactSet_OfCommandHandlers_InjectingRemainingPlansEvaluator_MatchesCanonicalList()
    {
        var appAssembly = typeof(MarkRiderStatusCommandHandler).Assembly;

        var handlersInjectingEvaluator = Types.InAssembly(appAssembly)
            .That().ResideInNamespaceStartingWith("MOHProject.Application.Features")
            .And().HaveNameEndingWith("CommandHandler")
            .GetTypes()
            .Where(t => t.GetConstructors()
                        .Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(IRemainingPlansEvaluator))))
            .Select(t => t.Name)
            .ToHashSet();

        handlersInjectingEvaluator.Should().BeEquivalentTo(CanonicalEvaluatorConsumers,
            "PH-06-05: the set of handlers wiring IRemainingPlansEvaluator must exactly match " +
            "the canonical list. If you added a new pipeline handler, update CanonicalEvaluatorConsumers. " +
            "If you dropped the dependency from an existing handler, verify that FR-AOR-030 is still enforced " +
            "and update this list to acknowledge the change.");
    }
}
