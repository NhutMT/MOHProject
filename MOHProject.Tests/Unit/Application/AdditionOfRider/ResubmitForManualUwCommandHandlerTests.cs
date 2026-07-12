using Moq;
using MOHProject.Application.Features.AdditionOfRider;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Tests.Unit.Application.AdditionOfRider;

public class ResubmitForManualUwCommandHandlerTests
{
    private readonly Mock<IPolicyRepository> _repo = new(MockBehavior.Strict);
    private readonly Mock<IAuditTrailWriter> _audit = new(MockBehavior.Strict);

    private ResubmitForManualUwCommandHandler CreateSut() => new(_repo.Object, _audit.Object);

    [Fact]
    public async Task PolicyNotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Policy?)null);

        Func<Task> act = () => CreateSut().HandleAsync(new ResubmitForManualUwCommand(1, "u1"), default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not found*");
    }

    [Theory]
    [InlineData(PolicySubstatus.PendingUwAps)]
    [InlineData(PolicySubstatus.ConditionalAcceptanceLetterGenerated)]
    [InlineData(PolicySubstatus.PendingCashCollection)]
    [InlineData(PolicySubstatus.PendingIpRequestFile)]
    public async Task AllowedSubstatus_TransitionsAndAudits(PolicySubstatus from)
    {
        var policy = new Policy { Id = 5, Substatus = from };
        _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        _repo.Setup(r => r.SaveAsync(policy, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _audit.Setup(a => a.WriteAsync(5L, ResubmitForManualUwCommandHandler.AuditEventType, It.IsAny<object>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(new ResubmitForManualUwCommand(5, "u1"), default);

        policy.Substatus.Should().Be(PolicySubstatus.PendingManualUnderwriting);
        _repo.Verify(r => r.SaveAsync(policy, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(PolicySubstatus.PendingManualUnderwriting)]
    [InlineData(PolicySubstatus.PendingUwCloaAssessment)]
    [InlineData(PolicySubstatus.PendingIpResponseFileCpfRejected)]
    [InlineData(PolicySubstatus.PolicyIncepted)]
    [InlineData(PolicySubstatus.PendingPpRequestFile)]
    [InlineData(PolicySubstatus.PendingPpResponseFileCpfRejected)]
    public async Task DisallowedSubstatus_Throws_NoStateChange(PolicySubstatus from)
    {
        var policy = new Policy { Id = 5, Substatus = from };
        _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(policy);

        Func<Task> act = () => CreateSut().HandleAsync(new ResubmitForManualUwCommand(5, "u1"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*'{from}'*",
                "FR-AOR-042 restricts resubmit to a specific whitelist (source lines 202-205)");

        policy.Substatus.Should().Be(from, "state must not mutate on rejection");
        _repo.Verify(r => r.SaveAsync(It.IsAny<Policy>(), It.IsAny<CancellationToken>()), Times.Never);
        _audit.Verify(a => a.WriteAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()),
                      Times.Never);
    }

    [Fact]
    public void AllowedFrom_MatchesDocList()
    {
        ResubmitForManualUwCommandHandler.AllowedFrom.Should().BeEquivalentTo(new[]
        {
            PolicySubstatus.PendingUwAps,
            PolicySubstatus.ConditionalAcceptanceLetterGenerated,
            PolicySubstatus.PendingCashCollection,
            PolicySubstatus.PendingIpRequestFile,
        }, "FR-AOR-042 explicit list");
    }
}
