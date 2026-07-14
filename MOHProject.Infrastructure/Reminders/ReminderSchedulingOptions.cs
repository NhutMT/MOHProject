namespace MOHProject.Infrastructure.Reminders;

// Bound from appsettings.json section "ReminderScheduling".
// Placeholder values — Q-501 (docs/specs/phases/05-reminders.md) is open for BA
// to confirm exact offsets.
public sealed class ReminderSchedulingOptions
{
    public const string SectionName = "ReminderScheduling";

    // Days from letter IssuedAt to the first reminder.
    public int LoaReminderOffsetDays { get; set; } = 30;
    public int LoaFinalReminderOffsetDays { get; set; } = 60;

    public int CloaReminderOffsetDays { get; set; } = 30;
    public int CloaFinalReminderOffsetDays { get; set; } = 60;
}
