using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MOHProject.Domain.Services;
using MOHProject.Infrastructure;

namespace MOHProject.Tests.Unit.Infrastructure;

public class DependencyInjectionEvaluatorTests
{
    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb);Database=T;Trusted_Connection=True;"
            })
            .Build();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddInfrastructure_RegistersAllEvaluators_AsSingletons()
    {
        using var provider = (ServiceProvider)BuildProvider();

        provider.GetService<IPlansCompositionEvaluator>().Should().NotBeNull().And.BeOfType<PlansCompositionEvaluator>();
        provider.GetService<IUwFieldStatesEvaluator>().Should().NotBeNull().And.BeOfType<UwFieldStatesEvaluator>();
        provider.GetService<INextSubstatusEvaluator>().Should().NotBeNull().And.BeOfType<NextSubstatusEvaluator>();
        provider.GetService<ILetterTypeEvaluator>().Should().NotBeNull().And.BeOfType<LetterTypeEvaluator>();
        provider.GetService<IRemainingPlansEvaluator>().Should().NotBeNull().And.BeOfType<RemainingPlansEvaluator>();
    }

    [Fact]
    public void RemainingPlansEvaluator_ResolvesFromRoot_WithoutManualWiring()
    {
        using var provider = (ServiceProvider)BuildProvider();

        var evaluator = provider.GetService<IRemainingPlansEvaluator>();

        evaluator.Should().NotBeNull(
            "Web startup calls AddInfrastructure — every downstream consumer should get RemainingPlansEvaluator via DI");
    }

    [Fact]
    public void AddInfrastructure_RegistersAll7EntryPointHandlers()
    {
        using var provider = (ServiceProvider)BuildProvider();

        var handlers = provider.GetServices<IEntryPointHandler>().ToArray();

        handlers.Should().HaveCount(7, "5 main entry substatuses + 2 NTU-only PP substatuses");
        handlers.Select(h => h.EntrySubstatus).Should().OnlyHaveUniqueItems(
            "each handler must claim a distinct entry substatus so the registry can dispatch unambiguously");

        var registry = provider.GetRequiredService<IEntryPointHandlerRegistry>();
        registry.ResolveFor(MOHProject.Domain.Enums.PolicySubstatus.ConditionalAcceptanceLetterGenerated)
            .Should().BeOfType<MOHProject.Domain.Services.EntryPoints.CondAcceptLetterGenHandler>();
    }
}
