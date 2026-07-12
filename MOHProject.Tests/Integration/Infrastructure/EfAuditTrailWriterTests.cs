using Microsoft.EntityFrameworkCore;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Infrastructure.Persistence;

namespace MOHProject.Tests.Integration.Infrastructure;

[Collection(nameof(SqlServerCollection))]
public class EfAuditTrailWriterTests
{
    private readonly SqlServerFixture _fixture;

    public EfAuditTrailWriterTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task WriteAsync_PersistsAuditEntry_WithCamelCaseJson()
    {
        var policyId = await CreatePolicy();

        await using (var db = _fixture.CreateContext())
        {
            var sut = new EfAuditTrailWriter(db);
            await sut.WriteAsync(policyId, "SomeEvent", "actor-01", new
            {
                RiderProductCode = "Choice",
                PreviousStatus = ProductStatus.Active,
                NewStatus = ProductStatus.NotTakenUp,
            }, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var row = await db.AuditEntries.SingleAsync(a => a.PolicyId == policyId);
            row.EventType.Should().Be("SomeEvent");
            row.ActorUserId.Should().Be("actor-01");
            row.PayloadJson.Should().Contain("\"riderProductCode\":\"Choice\"",
                "System.Text.Json is configured with camelCase naming policy");
            row.PayloadJson.Should().Contain("\"newStatus\":3",
                "enums serialize as numeric values by default — the DB stores raw values, presentation layer maps back");
            row.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task WriteAsync_MultipleEvents_MaintainsChronologicalOrder()
    {
        var policyId = await CreatePolicy();

        await using (var db = _fixture.CreateContext())
        {
            var sut = new EfAuditTrailWriter(db);
            await sut.WriteAsync(policyId, "Event1", "u", new { seq = 1 }, default);
            await sut.WriteAsync(policyId, "Event2", "u", new { seq = 2 }, default);
            await sut.WriteAsync(policyId, "Event3", "u", new { seq = 3 }, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var events = await db.AuditEntries
                .Where(a => a.PolicyId == policyId)
                .OrderBy(a => a.OccurredAt)
                .ThenBy(a => a.Id)
                .Select(a => a.EventType)
                .ToArrayAsync();

            events.Should().Equal("Event1", "Event2", "Event3");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task WriteAsync_EmptyOrNullEventType_Throws(string? eventType)
    {
        await using var db = _fixture.CreateContext();
        var sut = new EfAuditTrailWriter(db);

        Func<Task> act = () => sut.WriteAsync(1, eventType!, "u", new { }, default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private async Task<long> CreatePolicy()
    {
        // PolicyNumber column is nvarchar(32) — keep test IDs short but unique.
        var policyNumber = $"P-{Guid.NewGuid():N}"[..30];
        await using var db = _fixture.CreateContext();
        var policy = new Policy
        {
            PolicyNumber = policyNumber,
            Type = PolicyType.NewBusiness,
            Substatus = PolicySubstatus.PendingManualUnderwriting,
            InsuredResidency = Residency.Sg,
            PayerResidency = Residency.Sg,
        };
        db.Policies.Add(policy);
        await db.SaveChangesAsync();
        return policy.Id;
    }
}
