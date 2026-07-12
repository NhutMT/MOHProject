using MOHProject.Domain.Enums;

namespace MOHProject.Domain.Services;

public interface IEntryPointHandlerRegistry
{
    IEntryPointHandler ResolveFor(PolicySubstatus entrySubstatus);
}

public sealed class EntryPointHandlerRegistry : IEntryPointHandlerRegistry
{
    private readonly Dictionary<PolicySubstatus, IEntryPointHandler> _byEntry;

    public EntryPointHandlerRegistry(IEnumerable<IEntryPointHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        _byEntry = handlers.ToDictionary(h => h.EntrySubstatus);
    }

    public IEntryPointHandler ResolveFor(PolicySubstatus entrySubstatus)
    {
        if (!_byEntry.TryGetValue(entrySubstatus, out var handler))
            throw new InvalidOperationException(
                $"No IEntryPointHandler registered for entry substatus '{entrySubstatus}'. " +
                "UW decisions from this substatus are not routable.");
        return handler;
    }
}
