using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Application.Ports;

public interface ILetterGenerator
{
    Task<Letter> GenerateAsync(long policyId, LetterType type, CancellationToken ct);
}
