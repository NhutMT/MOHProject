using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MOHProject.Application.Ports;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;
using MOHProject.Infrastructure.Persistence;

namespace MOHProject.Infrastructure;

public static class DependencyInjection
{
    public const string DefaultConnectionName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DefaultConnectionName)
            ?? throw new InvalidOperationException(
                $"Missing connection string '{DefaultConnectionName}' in configuration. " +
                "Set it in appsettings.json under ConnectionStrings, or via environment variable " +
                $"ConnectionStrings__{DefaultConnectionName}.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        // Pure domain services — stateless, safe as singleton.
        services.AddSingleton<IPlansCompositionEvaluator, PlansCompositionEvaluator>();
        services.AddSingleton<IUwFieldStatesEvaluator, UwFieldStatesEvaluator>();
        services.AddSingleton<INextSubstatusEvaluator, NextSubstatusEvaluator>();
        services.AddSingleton<ILetterTypeEvaluator, LetterTypeEvaluator>();
        services.AddSingleton<IRemainingPlansEvaluator, RemainingPlansEvaluator>();

        // Entry-point handlers — one per entry substatus.
        services.AddSingleton<IEntryPointHandler, CondAcceptLetterGenHandler>();
        services.AddSingleton<IEntryPointHandler, PendingUwCloaAssessmentHandler>();
        services.AddSingleton<IEntryPointHandler, PendingCashCollectionHandler>();
        services.AddSingleton<IEntryPointHandler, PendingIpRequestFileHandler>();
        services.AddSingleton<IEntryPointHandler, PendingIpResponseCpfRejectedHandler>();
        services.AddSingleton<IEntryPointHandler, PendingPpRequestFileHandler>();
        services.AddSingleton<IEntryPointHandler, PendingPpResponseFileCpfRejectedHandler>();
        services.AddSingleton<IEntryPointHandlerRegistry, EntryPointHandlerRegistry>();

        // Repositories (scoped — hold the DbContext).
        services.AddScoped<IPolicyRepository, EfPolicyRepository>();

        return services;
    }
}
