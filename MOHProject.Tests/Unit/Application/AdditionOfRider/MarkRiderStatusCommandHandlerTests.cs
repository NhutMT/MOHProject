using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MOHProject.Application.Features.AdditionOfRider;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;

namespace MOHProject.Tests.Unit.Application.AdditionOfRider;

public class MarkRiderStatusCommandHandlerTests
{
    private readonly Mock<IPolicyRepository> _repo = new(MockBehavior.Strict);
    private readonly Mock<ILetterGenerator> _letters = new(MockBehavior.Strict);
    private readonly Mock<IAuditTrailWriter> _audit = new(MockBehavior.Strict);
    private readonly IRemainingPlansEvaluator _evaluator = new RemainingPlansEvaluator(
        new PlansCompositionEvaluator(),
        new UwFieldStatesEvaluator(),
        new NextSubstatusEvaluator(),
        new LetterTypeEvaluator(),
        NullLogger<RemainingPlansEvaluator>.Instance);

    private readonly Mock<IReminderScheduler> _reminders = new();

    private MarkRiderStatusCommandHandler CreateSut() =>
        new(_repo.Object, _evaluator, _letters.Object, _audit.Object, new PassthroughUnitOfWork(), _reminders.Object);

    [Fact]
    public async Task DisallowedTargetStatus_ThrowsBeforeAnyIo()
    {
        Func<Task> act = () => CreateSut().HandleAsync(
            new MarkRiderStatusCommand(1, 1, ProductStatus.Terminated, "u"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Terminated*",
                "MarkRiderStatusCommand only supports NTU / Declined / Postponed — Terminated is a separate lifecycle event");

        _repo.Verify(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never,
            "guard must fire before repository load");
    }

    [Fact]
    public async Task PolicyNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Policy?)null);

        Func<Task> act = () => CreateSut().HandleAsync(
            new MarkRiderStatusCommand(99, 1, ProductStatus.NotTakenUp, "u"), default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*99*not found*");
    }

    [Fact]
    public async Task PlanNotOnPolicy_Throws()
    {
        var policy = new Policy { Id = 1, UWState = new UWState() };
        policy.Plans.Add(new Plan { Id = 10, IsBase = true, Status = ProductStatus.Active });

        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        Func<Task> act = () => CreateSut().HandleAsync(
            new MarkRiderStatusCommand(1, 999, ProductStatus.NotTakenUp, "u"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Plan 999*not found*policy 1*");
    }

    [Fact]
    public async Task MarkingBasePlan_Throws()
    {
        var policy = new Policy { Id = 1, UWState = new UWState() };
        policy.Plans.Add(new Plan { Id = 10, IsBase = true, Status = ProductStatus.Active });

        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        Func<Task> act = () => CreateSut().HandleAsync(
            new MarkRiderStatusCommand(1, 10, ProductStatus.NotTakenUp, "u"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Base plan*",
                "Base plans cannot be NTU'd/Declined/Postponed independently of the policy");
    }

    [Theory]
    [InlineData(ProductStatus.NotTakenUp, LetterType.NtuWithoutRefund)]
    [InlineData(ProductStatus.Declined,   LetterType.Decline)]
    [InlineData(ProductStatus.Postponed,  LetterType.Postponement)]
    public async Task ValidRiderStatusChange_EmitsCorrectDecisionLetter_ThenRunsEvaluator(
        ProductStatus target, LetterType expectedDecisionLetter)
    {
        var policy = SgSgPolicyWithBaseAndRider();
        _repo.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _repo.Setup(r => r.SaveAsync(policy, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var lettersEmitted = new List<LetterType>();
        _letters.Setup(l => l.GenerateAsync(policy.Id, It.IsAny<LetterType>(), It.IsAny<CancellationToken>()))
                .Callback<long, LetterType, CancellationToken>((_, t, _) => lettersEmitted.Add(t))
                .ReturnsAsync((long _, LetterType t, CancellationToken _) => new Letter { Type = t });
        _audit.Setup(a => a.WriteAsync(policy.Id, MarkRiderStatusCommandHandler.AuditEventType, It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(new MarkRiderStatusCommand(policy.Id, 20, target, "u"), default);

        var rider = policy.Plans.Single(p => p.Id == 20);
        rider.Status.Should().Be(target);
        rider.StatusChangedAt.Should().NotBeNull();

        lettersEmitted.Should().StartWith(new[] { expectedDecisionLetter },
            "the decision-specific letter is emitted before any main letter from the evaluator");
    }

    [Fact]
    public async Task NtuOfSubstandardRider_TriggersEvaluator_ReflectsUatBugRootFix()
    {
        // Regression pattern: rider was RCMP (both loading + exclusion), then NTU'd.
        // Base=Standard, no shortfall, SG/PR × SG/PR. Remaining Active = Base (Standard).
        // Expected: composition AllStandard, substatus PendingIpRequestFile, no letter (not letter-gating).
        var policy = SgSgPolicyWithBaseAndRider(riderLoading: true, riderExclusion: true,
            substatus: PolicySubstatus.PendingCashCollection);

        _repo.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _repo.Setup(r => r.SaveAsync(policy, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _letters.Setup(l => l.GenerateAsync(policy.Id, LetterType.NtuWithoutRefund, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Letter { Type = LetterType.NtuWithoutRefund });
        _audit.Setup(a => a.WriteAsync(policy.Id, MarkRiderStatusCommandHandler.AuditEventType, It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(new MarkRiderStatusCommand(policy.Id, 20, ProductStatus.NotTakenUp, "u"), default);

        policy.Substatus.Should().Be(PolicySubstatus.PendingIpRequestFile,
            "NTU'ing the only Substandard rider leaves AllStandard → SG/PR × SG/PR + no shortfall → PendingIpRequestFile. " +
            "This is the code path that fixes BUG-2610000154P.");
        policy.UWState!.RcmpFlag.Should().BeFalse("evaluator's AllStandard branch clears the RCMP flag");
    }

    private static Policy SgSgPolicyWithBaseAndRider(
        bool riderLoading = false,
        bool riderExclusion = false,
        PolicySubstatus substatus = PolicySubstatus.ConditionalAcceptanceLetterGenerated)
    {
        var policy = new Policy
        {
            Id = 1,
            Substatus = substatus,
            InsuredResidency = Residency.Sg,
            PayerResidency = Residency.Sg,
            UWState = new UWState { AcceptCloa = AcceptCloa.Yes, RcmpFlag = riderLoading && riderExclusion },
            PremiumCollection = new PremiumCollection(),
        };
        policy.Plans.Add(new Plan { Id = 10, IsBase = true, Status = ProductStatus.Active, ProductCode = "Base" });
        policy.Plans.Add(new Plan
        {
            Id = 20,
            IsBase = false,
            Status = ProductStatus.Active,
            ProductCode = "Rider",
            HasActiveRiskLoading = riderLoading,
            HasActiveExclusion = riderExclusion,
        });
        return policy;
    }
}
