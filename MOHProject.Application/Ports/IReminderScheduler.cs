using MOHProject.Domain.Entities;

namespace MOHProject.Application.Ports;

public interface IReminderScheduler
{
    Task ScheduleFromAsync(Letter letter, CancellationToken ct);
    Task CancelForAsync(Guid correlationId, CancellationToken ct);

    // Used when substatus transitions to PendingUwAps — doc rule: STOP all
    // LOA/CLOA reminders (source lines 363-365).
    Task CancelAllForPolicyAsync(long policyId, CancellationToken ct);
}
