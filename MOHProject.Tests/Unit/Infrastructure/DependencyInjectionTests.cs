using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MOHProject.Infrastructure;
using MOHProject.Infrastructure.Persistence;

namespace MOHProject.Tests.Unit.Infrastructure;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_MissingConnectionString_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        Action act = () => services.AddInfrastructure(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultConnection*")
            .And.Message.Should().Contain("appsettings.json",
                "the error must tell a developer where to set the connection string");
    }

    [Fact]
    public void AddInfrastructure_WithConnectionString_RegistersAppDbContext()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;Trusted_Connection=True;"
            })
            .Build();

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetService<AppDbContext>().Should().NotBeNull();
    }
}
