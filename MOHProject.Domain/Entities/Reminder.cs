using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Entities;

public class Reminder
{
    public long Id { get; set; }
    public long PolicyId { get; set; }
    public Policy? Policy { get; set; }

    public long ParentLetterId { get; set; }
    public Letter? ParentLetter { get; set; }

    public LetterType ReminderType { get; set; }

    public DateTime ScheduledFor { get; set; }

    public ReminderStatus Status { get; set; } = ReminderStatus.Scheduled;
}
