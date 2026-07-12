using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

// One implementation per entry substatus (FR-AOR-050..054 + NTU-only entries).
// Pure — no I/O. The command layer coordinates I/O per the directive.
public interface IEntryPointHandler
{
    PolicySubstatus EntrySubstatus { get; }
    EntryPointDirective Handle(Policy policy, UwDecision decision);
}
