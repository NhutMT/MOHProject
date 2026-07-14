using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Infrastructure.Persistence;
using MOHProject.Infrastructure.Reminders;

namespace MOHProject.Tests.Integration.Infrastructure;

[Collection(nameof(SqlServerCollection))]
public class EfReminderSchedulerTests
{
    private readonly SqlServerFixture _fixture;

    public EfReminderSchedulerTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ScheduleFromAsync_LoaLetter_InsertsTwoScheduledRows_OneReminder_OneFinal()
    {
        var policyId = await CreatePolicyAsync();
        Letter letter;

        await using (var db = _fixture.CreateContext())
        {
            letter = new Letter
            {
                PolicyId = policyId,
                Type = LetterType.Loa,
                IssuedAt = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc),
                IsCurrent = true,
                CorrelationId = Guid.NewGuid(),
            };
            db.Letters.Add(letter);
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext())
        {
            var sut = new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions()));
            await sut.ScheduleFromAsync(letter, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var rows = await db.Reminders
                .Where(r => r.PolicyId == policyId)
                .OrderBy(r => r.ScheduledFor)
                .ToListAsync();

            rows.Should().HaveCount(2);
            rows[0].ReminderType.Should().Be(LetterType.LoaReminder);
            rows[1].ReminderType.Should().Be(LetterType.LoaFinalReminder);
            rows.Should().OnlyContain(r => r.Status == ReminderStatus.Scheduled);

            rows[0].ScheduledFor.Should().Be(new DateTime(2026, 07, 31, 0, 0, 0, DateTimeKind.Utc),
                "default LoaReminderOffsetDays = 30 → IssuedAt + 30 days");
            rows[1].ScheduledFor.Should().Be(new DateTime(2026, 08, 30, 0, 0, 0, DateTimeKind.Utc),
                "default LoaFinalReminderOffsetDays = 60 → IssuedAt + 60 days");
        }
    }

    [Theory]
    [InlineData(LetterType.CloaExclusion)]
    [InlineData(LetterType.CloaRcmp)]
    public async Task ScheduleFromAsync_CloaLetter_UsesCloaOffsets(LetterType cloaType)
    {
        var policyId = await CreatePolicyAsync();
        Letter letter;
        await using (var db = _fixture.CreateContext())
        {
            letter = new Letter { PolicyId = policyId, Type = cloaType, IssuedAt = DateTime.UtcNow, IsCurrent = true, CorrelationId = Guid.NewGuid() };
            db.Letters.Add(letter);
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext())
        {
            await new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions())).ScheduleFromAsync(letter, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var types = await db.Reminders.Where(r => r.PolicyId == policyId).Select(r => r.ReminderType).OrderBy(t => t).ToListAsync();
            types.Should().Contain(new[] { LetterType.CloaReminder, LetterType.CloaFinalReminder });
        }
    }

    [Fact]
    public async Task ScheduleFromAsync_UnrelatedLetterType_DoesNothing()
    {
        var policyId = await CreatePolicyAsync();
        Letter letter;
        await using (var db = _fixture.CreateContext())
        {
            letter = new Letter { PolicyId = policyId, Type = LetterType.MedicalEvidence, IssuedAt = DateTime.UtcNow, IsCurrent = true, CorrelationId = Guid.NewGuid() };
            db.Letters.Add(letter);
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext())
        {
            await new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions())).ScheduleFromAsync(letter, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            (await db.Reminders.Where(r => r.PolicyId == policyId).ToListAsync()).Should().BeEmpty(
                "Medical Evidence letters aren't followed by reminders");
        }
    }

    [Fact]
    public async Task CancelForAsync_FlipsMatchingReminders_ToCancelled()
    {
        var policyId = await CreatePolicyAsync();
        var correlation = Guid.NewGuid();
        Letter letter;

        await using (var db = _fixture.CreateContext())
        {
            letter = new Letter { PolicyId = policyId, Type = LetterType.Loa, IssuedAt = DateTime.UtcNow, IsCurrent = true, CorrelationId = correlation };
            db.Letters.Add(letter);
            await db.SaveChangesAsync();
            await new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions())).ScheduleFromAsync(letter, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            await new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions())).CancelForAsync(correlation, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var rows = await db.Reminders.Where(r => r.PolicyId == policyId).ToListAsync();
            rows.Should().OnlyContain(r => r.Status == ReminderStatus.Cancelled,
                "rows aren't hard-deleted — Cancelled keeps history for audit");
        }
    }

    [Fact]
    public async Task CancelForAsync_DoesNotAffect_AlreadySentReminders()
    {
        var policyId = await CreatePolicyAsync();
        var correlation = Guid.NewGuid();

        await using (var db = _fixture.CreateContext())
        {
            var letter = new Letter { PolicyId = policyId, Type = LetterType.Loa, IssuedAt = DateTime.UtcNow, IsCurrent = true, CorrelationId = correlation };
            db.Letters.Add(letter);
            await db.SaveChangesAsync();

            db.Reminders.Add(new Reminder { PolicyId = policyId, ParentLetterId = letter.Id, ReminderType = LetterType.LoaReminder, ScheduledFor = DateTime.UtcNow, Status = ReminderStatus.Sent });
            db.Reminders.Add(new Reminder { PolicyId = policyId, ParentLetterId = letter.Id, ReminderType = LetterType.LoaFinalReminder, ScheduledFor = DateTime.UtcNow.AddDays(30), Status = ReminderStatus.Scheduled });
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext())
        {
            await new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions())).CancelForAsync(correlation, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var rows = await db.Reminders.Where(r => r.PolicyId == policyId).OrderBy(r => r.ReminderType).ToListAsync();
            rows.Single(r => r.ReminderType == LetterType.LoaReminder).Status.Should().Be(ReminderStatus.Sent,
                "already-Sent rows are historical facts — cancellation must not rewrite them");
            rows.Single(r => r.ReminderType == LetterType.LoaFinalReminder).Status.Should().Be(ReminderStatus.Cancelled);
        }
    }

    [Fact]
    public async Task CancelAllForPolicyAsync_FlipsEveryScheduledReminder()
    {
        var policyId = await CreatePolicyAsync();

        await using (var db = _fixture.CreateContext())
        {
            var loa = new Letter { PolicyId = policyId, Type = LetterType.Loa, IssuedAt = DateTime.UtcNow, IsCurrent = true, CorrelationId = Guid.NewGuid() };
            var cloa = new Letter { PolicyId = policyId, Type = LetterType.CloaExclusion, IssuedAt = DateTime.UtcNow, IsCurrent = true, CorrelationId = Guid.NewGuid() };
            db.Letters.AddRange(loa, cloa);
            await db.SaveChangesAsync();
            var sut = new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions()));
            await sut.ScheduleFromAsync(loa, default);
            await sut.ScheduleFromAsync(cloa, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            await new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions())).CancelAllForPolicyAsync(policyId, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var rows = await db.Reminders.Where(r => r.PolicyId == policyId).ToListAsync();
            rows.Should().HaveCount(4, "2 LOA reminders + 2 CLOA reminders");
            rows.Should().OnlyContain(r => r.Status == ReminderStatus.Cancelled,
                "PendingUwAps stops ALL LOA/CLOA reminders on the policy per source lines 363-365");
        }
    }

    private async Task<long> CreatePolicyAsync()
    {
        await using var db = _fixture.CreateContext();
        var policy = new Policy
        {
            PolicyNumber = $"P-{Guid.NewGuid():N}"[..30],
            Type = PolicyType.NewBusiness,
            Substatus = PolicySubstatus.PendingCashCollection,
            InsuredResidency = Domain.Enums.Residency.Sg,
            PayerResidency = Domain.Enums.Residency.Sg,
        };
        db.Policies.Add(policy);
        await db.SaveChangesAsync();
        return policy.Id;
    }
}
