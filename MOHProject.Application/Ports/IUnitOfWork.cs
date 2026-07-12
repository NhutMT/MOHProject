namespace MOHProject.Application.Ports;

// Wraps command work in a single DB transaction. Recalculate + letter emission
// + audit + substatus persistence must be atomic — if any step throws, none of
// them commit.
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct);
}
