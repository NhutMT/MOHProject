using MOHProject.Domain.Entities;

namespace MOHProject.Application.Ports;

public interface IPolicyRepository
{
    Task<Policy?> GetByIdAsync(long policyId, CancellationToken ct);
    Task<Policy?> GetByPolicyNumberAsync(string policyNumber, CancellationToken ct);
    Task SaveAsync(Policy policy, CancellationToken ct);
}
