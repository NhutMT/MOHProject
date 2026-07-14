using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Infrastructure.Persistence;

namespace MOHProject.Infrastructure.Reminders;

// Implements PH-05-A. Stores Reminder rows in the existing table with
// Status = Scheduled and ScheduledFor computed from letter IssuedAt plus
// the configured offset. Cancellation flips Status to Cancelled (rows
// stay for audit history — never hard-deleted).
//
// Fire-time processing (actually sending the reminder letter) is NOT part
// of this MVP; Phase 5.next will add either a Hangfire hosted job or a
// simple polling background service once BA confirms cadence (Q-501).
public sealed class EfReminderScheduler : IReminderScheduler
{
    private readonly AppDbContext _db;
    private readonly ReminderSchedulingOptions _options;

    public EfReminderScheduler(AppDbContext db, IOptions<ReminderSchedulingOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task ScheduleFromAsync(Letter letter, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(letter);

        var (reminderType, finalReminderType, reminderOffset, finalOffset) = ResolveSchedule(letter.Type);
        if (reminderType is null)
            return; // letter type not eligible for reminders (Refund, MedicalEvidence, etc.)

        var scheduledForReminder = letter.IssuedAt.AddDays(reminderOffset);
        var scheduledForFinal    = letter.IssuedAt.AddDays(finalOffset);

        _db.Reminders.Add(new Reminder
        {
            PolicyId = letter.PolicyId,
            ParentLetterId = letter.Id,
            ReminderType = reminderType.Value,
            ScheduledFor = scheduledForReminder,
            Status = ReminderStatus.Scheduled,
        });
        _db.Reminders.Add(new Reminder
        {
            PolicyId = letter.PolicyId,
            ParentLetterId = letter.Id,
            ReminderType = finalReminderType!.Value,
            ScheduledFor = scheduledForFinal,
            Status = ReminderStatus.Scheduled,
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelForAsync(Guid correlationId, CancellationToken ct)
    {
        // Correlation flows through the parent letter's CorrelationId. Match
        // reminders whose ParentLetter has that CorrelationId AND that are
        // still Scheduled — Sent and already-Cancelled rows stay untouched.
        var reminders = await _db.Reminders
            .Where(r => r.Status == ReminderStatus.Scheduled)
            .Join(_db.Letters.Where(l => l.CorrelationId == correlationId),
                  r => r.ParentLetterId, l => l.Id, (r, _) => r)
            .ToListAsync(ct);

        foreach (var r in reminders)
            r.Status = ReminderStatus.Cancelled;

        if (reminders.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    public async Task CancelAllForPolicyAsync(long policyId, CancellationToken ct)
    {
        var reminders = await _db.Reminders
            .Where(r => r.PolicyId == policyId && r.Status == ReminderStatus.Scheduled)
            .ToListAsync(ct);

        foreach (var r in reminders)
            r.Status = ReminderStatus.Cancelled;

        if (reminders.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    private (LetterType? Reminder, LetterType? FinalReminder, int ReminderDays, int FinalDays) ResolveSchedule(LetterType parentType) =>
        parentType switch
        {
            LetterType.Loa =>
                (LetterType.LoaReminder, LetterType.LoaFinalReminder, _options.LoaReminderOffsetDays, _options.LoaFinalReminderOffsetDays),

            LetterType.CloaExclusion or LetterType.CloaRcmp =>
                (LetterType.CloaReminder, LetterType.CloaFinalReminder, _options.CloaReminderOffsetDays, _options.CloaFinalReminderOffsetDays),

            _ => (null, null, 0, 0),
        };
}
