using MOHProject.Application.Ports;

namespace MOHProject.Tests.Unit.Application.AdditionOfRider;

// Test double: executes the work delegate directly, no transaction.
// Real transactional semantics are covered by integration tests.
public sealed class PassthroughUnitOfWork : IUnitOfWork
{
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct) => work(ct);
}
