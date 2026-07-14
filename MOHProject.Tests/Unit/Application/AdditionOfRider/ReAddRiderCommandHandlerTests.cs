using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MOHProject.Application.Features.AdditionOfRider;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;

namespace MOHProject.Tests.Unit.Application.AdditionOfRider;

public class ReAddRiderCommandHandlerTests
{
    private readonly Mock<IPolicyRepository> _repo = new(MockBehavior.Strict);
    private readonly Mock<ILetterGenerator> _letters = new(MockBehavior.Strict);
    private readonly Mock<IAuditTrailWriter> _audit = new(MockBehavior.Strict);
    private readonly Mock<IReminderScheduler> _reminders = new();
    private readonly IRemainingPlansEvaluator _evaluator = new RemainingPlansEvaluator(
        new PlansCompositionEvaluator(),
        new UwFieldStatesEvaluator(),
        new NextSubstatusEvaluator(),
        new LetterTypeEvaluator(),
        NullLogger<RemainingPlansEvaluator>.Instance);

    private ReAddRiderCommandHandler CreateSut() =>
        new(_repo.Object, _evaluator, _letters.Object, _audit.Object, new PassthroughUnitOfWork(), _reminders.Object);

    [Fact]
    public async Task PolicyNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Policy?)null);
        Func<Task> act = () => CreateSut().HandleAsync(new ReAddRiderCommand(1, 20, "u"), default);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task PlanNotOnPolicy_Throws()
    {
        var policy = new Policy { Id = 1, UWState = new UWState() };
        policy.Plans.Add(new Plan { Id = 10, IsBase = true, Status = ProductStatus.Active });
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        Func<Task> act = () => CreateSut().HandleAsync(new ReAddRiderCommand(1, 999, "u"), default);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Plan 999*");
    }

    [Fact]
    public async Task ReAddingBasePlan_Throws()
    {
        var policy = new Policy { Id = 1, UWState = new UWState() };
        policy.Plans.Add(new Plan { Id = 10, IsBase = true, Status = ProductStatus.NotTakenUp });
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        Func<Task> act = () => CreateSut().HandleAsync(new ReAddRiderCommand(1, 10, "u"), default);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Base plan*never removed*");
    }

    [Theory]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Draft)]
    [InlineData(ProductStatus.Terminated)]
    [InlineData(ProductStatus.Cancelled)]
    public async Task IrreversibleStatus_Throws(ProductStatus from)
    {
        var policy = BuildPolicy(riderStatus: from);
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        Func<Task> act = () => CreateSut().HandleAsync(new ReAddRiderCommand(1, 20, "u"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*'{from}'*",
                "re-add is only reversible from NotTakenUp / Declined / Postponed");
    }

    [Theory]
    [InlineData(ProductStatus.NotTakenUp)]
    [InlineData(ProductStatus.Declined)]
    [InlineData(ProductStatus.Postponed)]
    public async Task ValidReAdd_RestoresActive_AndRunsEvaluator(ProductStatus from)
    {
        // Scenario mirrors BUG-2610000154P: a rider was NTU'd → composition became AllStandard →
        // substatus went to PendingIpRequest. Re-adding brings the RCMP rider back → composition
        // swings to HasRcmp → substatus swings back to a Substandard-appropriate state.
        var policy = BuildPolicy(riderStatus: from, riderLoading: true, riderExclusion: true,
                                 substatus: PolicySubstatus.PendingIpRequestFile);

        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _repo.Setup(r => r.SaveAsync(policy, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _letters.Setup(l => l.GenerateAsync(policy.Id, LetterType.CloaRcmp, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Letter { Type = LetterType.CloaRcmp });
        _audit.Setup(a => a.WriteAsync(policy.Id, ReAddRiderCommandHandler.AuditEventType, It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(new ReAddRiderCommand(policy.Id, 20, "u"), default);

        var rider = policy.Plans.Single(p => p.Id == 20);
        rider.Status.Should().Be(ProductStatus.Active, "the re-add restores the rider to Active");
        rider.StatusChangedAt.Should().NotBeNull();

        // The rider's RCMP composition + Blank AcceptCloa → CondAccept.
        policy.Substatus.Should().Be(PolicySubstatus.ConditionalAcceptanceLetterGenerated,
            "HasRcmp remaining + AcceptCloa=Blank → CondAccept (swings the substatus back)");
        policy.UWState!.RcmpFlag.Should().BeTrue("HasRcmp composition sets the flag");
    }

    private static Policy BuildPolicy(
        ProductStatus riderStatus,
        bool riderLoading = false,
        bool riderExclusion = false,
        PolicySubstatus substatus = PolicySubstatus.PendingCashCollection)
    {
        var policy = new Policy
        {
            Id = 1,
            Substatus = substatus,
            InsuredResidency = Residency.Sg,
            PayerResidency = Residency.Sg,
            UWState = new UWState { AcceptCloa = AcceptCloa.Blank },
            PremiumCollection = new PremiumCollection(),
        };
        policy.Plans.Add(new Plan { Id = 10, IsBase = true, Status = ProductStatus.Active, ProductCode = "Base" });
        policy.Plans.Add(new Plan
        {
            Id = 20,
            IsBase = false,
            Status = riderStatus,
            ProductCode = "Rider",
            HasActiveRiskLoading = riderLoading,
            HasActiveExclusion = riderExclusion,
        });
        return policy;
    }
}
