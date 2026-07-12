using Microsoft.EntityFrameworkCore;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;

namespace MOHProject.Infrastructure.Persistence;

public sealed class EfPolicyRepository : IPolicyRepository
{
    private readonly AppDbContext _db;

    public EfPolicyRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Policy?> GetByIdAsync(long policyId, CancellationToken ct) =>
        Query().FirstOrDefaultAsync(p => p.Id == policyId, ct);

    public Task<Policy?> GetByPolicyNumberAsync(string policyNumber, CancellationToken ct) =>
        Query().FirstOrDefaultAsync(p => p.PolicyNumber == policyNumber, ct);

    public async Task SaveAsync(Policy policy, CancellationToken ct)
    {
        // Attach if detached, otherwise EF is already tracking (was returned by Get*).
        if (_db.Entry(policy).State == EntityState.Detached)
            _db.Attach(policy);
        await _db.SaveChangesAsync(ct);
    }

    private IQueryable<Policy> Query() => _db.Policies
        .Include(p => p.UWState)
        .Include(p => p.PremiumCollection)
        .Include(p => p.Insured)
        .Include(p => p.Payer)
        .Include(p => p.PolicyHolder)
        .Include(p => p.Plans)
        .Include(p => p.Letters)
        .Include(p => p.AuditEntries);
}
