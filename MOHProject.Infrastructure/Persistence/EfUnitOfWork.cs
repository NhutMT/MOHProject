using Microsoft.EntityFrameworkCore;
using MOHProject.Application.Ports;

namespace MOHProject.Infrastructure.Persistence;

// SQL Server transactional wrapper for the scoped DbContext.
// Reuses an ambient transaction if one is already open (nested calls are
// no-ops) — lets outer coordinators own the boundary while inner services
// stay atomic-agnostic.
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public EfUnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(work);

        if (_db.Database.CurrentTransaction is not null)
        {
            await work(ct);
            return;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await work(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
