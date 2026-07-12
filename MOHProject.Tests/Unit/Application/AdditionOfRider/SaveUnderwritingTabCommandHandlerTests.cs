using Moq;
using MOHProject.Application.Features.AdditionOfRider;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Tests.Unit.Application.AdditionOfRider;

public class SaveUnderwritingTabCommandHandlerTests
{
    private readonly Mock<IPolicyRepository> _repo = new(MockBehavior.Strict);
    private readonly Mock<IAuditTrailWriter> _audit = new(MockBehavior.Strict);

    private SaveUnderwritingTabCommandHandler CreateSut() => new(_repo.Object, _audit.Object);

    [Fact]
    public async Task PolicyNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>()))
             .ReturnsAsync((Policy?)null);

        Func<Task> act = () => CreateSut().HandleAsync(new SaveUnderwritingTabCommand(42, "u1"), default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*42*not found*");
    }

    [Fact]
    public async Task PolicyWithoutDraftPlan_NoOp_NoSaveNoAudit()
    {
        var policy = new Policy { Id = 7, Substatus = PolicySubstatus.PendingCashCollection };
        policy.Plans.Add(new Plan { Status = ProductStatus.Active });

        _repo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        await CreateSut().HandleAsync(new SaveUnderwritingTabCommand(7, "u1"), default);

        policy.Substatus.Should().Be(PolicySubstatus.PendingCashCollection,
            "no Draft plan means Risk Category is not BLANK — auto-route rule does not fire");
        _repo.Verify(r => r.SaveAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>()), Times.Never);
        _audit.Verify(a => a.WriteAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                      Times.Never);
    }

    [Fact]
    public async Task PolicyWithDraftPlan_AutoRoutesToPendingManualUw_AndAuditsWithNewEventType()
    {
        var policy = new Policy { Id = 3, Substatus = PolicySubstatus.ConditionalAcceptanceLetterGenerated };
        policy.Plans.Add(new Plan { Status = ProductStatus.Active });
        policy.Plans.Add(new Plan { Status = ProductStatus.Draft }); // the newly added rider

        _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _repo.Setup(r => r.SaveAsync(policy, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _audit.Setup(a => a.WriteAsync(3L, SaveUnderwritingTabCommandHandler.AuditEventType, It.IsAny<object>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(new SaveUnderwritingTabCommand(3, "u9"), default);

        policy.Substatus.Should().Be(PolicySubstatus.PendingManualUnderwriting);
        _repo.Verify(r => r.SaveAsync(policy, It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.WriteAsync(3L, SaveUnderwritingTabCommandHandler.AuditEventType, It.IsAny<object>(), It.IsAny<CancellationToken>()),
                      Times.Once);
    }

    [Fact]
    public void AuditEventType_MatchesDocText()
    {
        SaveUnderwritingTabCommandHandler.AuditEventType.Should().Be("SubmitForUnderwritingReview",
            "source line 233: audit trail text changed from 'user manual submit for underwriting review' to 'Submit for underwriting review'");
    }
}
