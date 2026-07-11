using MOHProject.Domain.Entities;

namespace MOHProject.Application.Ports;

public interface IReminderScheduler
{
    Task ScheduleFromAsync(Letter letter, CancellationToken ct);
    Task CancelForAsync(Guid correlationId, CancellationToken ct);
}
