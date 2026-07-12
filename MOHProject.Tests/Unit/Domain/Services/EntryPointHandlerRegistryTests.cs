using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;

namespace MOHProject.Tests.Unit.Domain.Services;

public class EntryPointHandlerRegistryTests
{
    private readonly IEntryPointHandler[] _allSeven = new IEntryPointHandler[]
    {
        new CondAcceptLetterGenHandler(),
        new PendingUwCloaAssessmentHandler(),
        new PendingCashCollectionHandler(),
        new PendingIpRequestFileHandler(),
        new PendingIpResponseCpfRejectedHandler(),
        new PendingPpRequestFileHandler(),
        new PendingPpResponseFileCpfRejectedHandler(),
    };

    [Theory]
    [InlineData(PolicySubstatus.ConditionalAcceptanceLetterGenerated, typeof(CondAcceptLetterGenHandler))]
    [InlineData(PolicySubstatus.PendingUwCloaAssessment, typeof(PendingUwCloaAssessmentHandler))]
    [InlineData(PolicySubstatus.PendingCashCollection, typeof(PendingCashCollectionHandler))]
    [InlineData(PolicySubstatus.PendingIpRequestFile, typeof(PendingIpRequestFileHandler))]
    [InlineData(PolicySubstatus.PendingIpResponseFileCpfRejected, typeof(PendingIpResponseCpfRejectedHandler))]
    [InlineData(PolicySubstatus.PendingPpRequestFile, typeof(PendingPpRequestFileHandler))]
    [InlineData(PolicySubstatus.PendingPpResponseFileCpfRejected, typeof(PendingPpResponseFileCpfRejectedHandler))]
    public void ResolveFor_ReturnsCorrectHandler(PolicySubstatus entry, Type expectedType)
    {
        var registry = new EntryPointHandlerRegistry(_allSeven);

        var handler = registry.ResolveFor(entry);

        handler.Should().BeOfType(expectedType);
    }

    [Theory]
    [InlineData(PolicySubstatus.PendingManualUnderwriting)]
    [InlineData(PolicySubstatus.PendingUwAps)]
    [InlineData(PolicySubstatus.PolicyIncepted)]
    public void ResolveFor_UnregisteredSubstatus_Throws(PolicySubstatus notRoutable)
    {
        var registry = new EntryPointHandlerRegistry(_allSeven);

        Action act = () => registry.ResolveFor(notRoutable);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{notRoutable}*",
                "substatuses that are not entry points (PendingManualUw, PendingUwAps, PolicyIncepted) are not routable via a handler");
    }

    [Fact]
    public void Constructor_NullEnumerable_Throws()
    {
        Action act = () => new EntryPointHandlerRegistry(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_DuplicateEntrySubstatuses_Throws()
    {
        var duplicated = new IEntryPointHandler[]
        {
            new CondAcceptLetterGenHandler(),
            new CondAcceptLetterGenHandler(),
        };

        Action act = () => new EntryPointHandlerRegistry(duplicated);

        act.Should().Throw<ArgumentException>(
            "ToDictionary throws on duplicate keys; two handlers claiming the same EntrySubstatus is a wiring bug");
    }
}
