using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
