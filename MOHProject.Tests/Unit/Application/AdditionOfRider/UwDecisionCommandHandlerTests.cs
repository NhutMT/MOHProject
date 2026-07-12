using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MOHProject.Application.Features.AdditionOfRider;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;

namespace MOHProject.Tests.Unit.Application.AdditionOfRider;

public class UwDecisionCommandHandlerTests
{
    private readonly Mock<IPolicyRepository> _repo = new(MockBehavior.Strict);
    private readonly Mock<ILetterGenerator> _letters = new(MockBehavior.Strict);
    private readonly Mock<IAuditTrailWriter> _audit = new(MockBehavior.Strict);

    private readonly IEntryPointHandlerRegistry _registry;
    private readonly IRemainingPlansEvaluator _evaluator;

    public UwDecisionCommandHandlerTests()
    {
        // Wire real domain services — they are pure and fast.
        _registry = new EntryPointHandlerRegistry(new IEntryPointHandler[]
        {
            new CondAcceptLetterGenHandler(),
            new PendingUwCloaAssessmentHandler(),
            new PendingCashCollectionHandler(),
            new PendingIpRequestFileHandler(),
            new PendingIpResponseCpfRejectedHandler(),
            new PendingPpRequestFileHandler(),
            new PendingPpResponseFileCpfRejectedHandler(),
        });
        _evaluator = new RemainingPlansEvaluator(
            new PlansCompositionEvaluator(),
            new UwFieldStatesEvaluator(),
            new NextSubstatusEvaluator(),
            new LetterTypeEvaluator(),
            NullLogger<RemainingPlansEvaluator>.Instance);
    }

    private UwDecisionCommandHandler CreateSut() =>
        new(_repo.Object, _registry, _evaluator, _letters.Object, _audit.Object);

    [Fact]
    public async Task ApsAtCondAccept_OverridesToPendingUwAps_EmitsMedicalEvidenceOnly()
    {
        var policy = ActivePolicy(PolicySubstatus.ConditionalAcceptanceLetterGenerated);
        _repo.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _repo.Setup(r => r.SaveAsync(policy, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _letters.Setup(l => l.GenerateAsync(policy.Id, LetterType.MedicalEvidence, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Letter { Type = LetterType.MedicalEvidence });
        _audit.Setup(a => a.WriteAsync(policy.Id, UwDecisionCommandHandler.AuditEventType, It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(new UwDecisionCommand(policy.Id, UwDecision.Aps, "u"), default);

        policy.Substatus.Should().Be(PolicySubstatus.PendingUwAps,
            "APS at 1.5.1 overrides substatus, skipping the evaluator");
        _letters.Verify(l => l.GenerateAsync(policy.Id, LetterType.MedicalEvidence, It.IsAny<CancellationToken>()), Times.Once);
        // No main letter — evaluator not called.
        _letters.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StandardAtCondAccept_WithSubstandardRiderRemaining_StaysAtCondAccept_EmitsCloa()
    {
        // Base=Standard + Exclusion rider → composition ExclusionOnly.
        // AcceptCloa=Blank → evaluator returns CondAccept + CloaExclusion letter.
        var policy = ActivePolicy(PolicySubstatus.ConditionalAcceptanceLetterGenerated);
        policy.UWState!.AcceptCloa = AcceptCloa.Blank;
        policy.Plans.Add(BasePlan());
        policy.Plans.Add(new Plan { IsBase = false, Status = ProductStatus.Active, HasActiveExclusion = true, ProductCode = "R1" });

        _repo.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _repo.Setup(r => r.SaveAsync(policy, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _letters.Setup(l => l.GenerateAsync(policy.Id, LetterType.CloaExclusion, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Letter { Type = LetterType.CloaExclusion });
        _audit.Setup(a => a.WriteAsync(policy.Id, UwDecisionCommandHandler.AuditEventType, It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(new UwDecisionCommand(policy.Id, UwDecision.Standard, "u"), default);

        policy.Substatus.Should().Be(PolicySubstatus.ConditionalAcceptanceLetterGenerated,
            "ExclusionOnly composition + AcceptCloa=Blank → evaluator returns CondAccept");
        _letters.Verify(l => l.GenerateAsync(policy.Id, LetterType.CloaExclusion, It.IsAny<CancellationToken>()),
                        Times.Once);
    }

    [Fact]
    public async Task DeclineAtCondAccept_EmitsDeclineLetterFirst_ThenCloaFromEvaluator()
    {
        var policy = ActivePolicy(PolicySubstatus.ConditionalAcceptanceLetterGenerated);
        policy.UWState!.AcceptCloa = AcceptCloa.Blank;
        policy.Plans.Add(BasePlan());
        policy.Plans.Add(new Plan { IsBase = false, Status = ProductStatus.Active, HasActiveExclusion = true, ProductCode = "R1" });

        _repo.Setup(r => r.GetByIdAsync(policy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _repo.Setup(r => r.SaveAsync(policy, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var order = new List<LetterType>();
        _letters.Setup(l => l.GenerateAsync(policy.Id, It.IsAny<LetterType>(), It.IsAny<CancellationToken>()))
                .Callback<long, LetterType, CancellationToken>((_, t, _) => order.Add(t))
                .ReturnsAsync((long _, LetterType t, CancellationToken _) => new Letter { Type = t });
        _audit.Setup(a => a.WriteAsync(policy.Id, UwDecisionCommandHandler.AuditEventType, It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(new UwDecisionCommand(policy.Id, UwDecision.Declined, "u"), default);

        order.Should().Equal(new[] { LetterType.Decline, LetterType.CloaExclusion },
            "decision-specific letter (Decline) emits BEFORE the main letter (CLOA) — order matters for the reader");
    }

    [Fact]
    public async Task PolicyNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Policy?)null);

        Func<Task> act = () => CreateSut().HandleAsync(new UwDecisionCommand(999, UwDecision.Standard, "u"), default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*999*not found*");
    }

    private static Policy ActivePolicy(PolicySubstatus substatus) => new()
    {
        Id = 1,
        Substatus = substatus,
        InsuredResidency = Residency.Sg,
        PayerResidency = Residency.Sg,
        UWState = new UWState { AcceptCloa = AcceptCloa.Yes },
        PremiumCollection = new PremiumCollection(),
    };

    private static Plan BasePlan() => new()
    {
        IsBase = true,
        Status = ProductStatus.Active,
        ProductCode = "Base",
    };
}
