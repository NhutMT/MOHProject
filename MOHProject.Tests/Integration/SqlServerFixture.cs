using System.Runtime.InteropServices;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MOHProject.Infrastructure.Persistence;

namespace MOHProject.Tests.Integration;

// Spins up SQL Server per test run and applies migrations. Shared across
// tests in the same [Collection] — tests must use unique data (Guid in
// keys) since the fixture is NOT reset between tests, per CLAUDE.md.
//
// Uses generic ContainerBuilder rather than MsSqlBuilder because
// azure-sql-edge (required for arm64) does not include the sqlcmd binary
// that MsSqlContainer relies on for its readiness probe. We wait on TCP
// port availability + retry the initial DB connect.
public class SqlServerFixture : IAsyncLifetime
{
    private const string SaPassword = "yourStrong(!)Password";
    private readonly IContainer _container;

    public SqlServerFixture()
    {
        var image = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? "mcr.microsoft.com/azure-sql-edge:latest"
            : "mcr.microsoft.com/mssql/server:2022-latest";

        _container = new ContainerBuilder()
            .WithImage(image)
            .WithPortBinding(1433, true)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", SaPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();
    }

    public string ConnectionString =>
        $"Server=localhost,{_container.GetMappedPublicPort(1433)};" +
        $"Database=MOHProject;User Id=sa;Password={SaPassword};" +
        "TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False;";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // SQL Server accepts TCP before the SQL engine is fully ready to serve.
        // Retry connect for up to 60s, then run migrations.
        await WaitForSqlServerAsync(TimeSpan.FromSeconds(60));

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private async Task WaitForSqlServerAsync(TimeSpan timeout)
    {
        // Master DB connect probe — smaller surface than opening our app DB
        // which doesn't exist yet.
        var masterCs = ConnectionString.Replace("Database=MOHProject;", "Database=master;", StringComparison.Ordinal);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await using var conn = new SqlConnection(masterCs);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("SELECT 1", conn);
                await cmd.ExecuteScalarAsync();
                return;
            }
            catch (SqlException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        throw new TimeoutException($"SQL Server did not become ready within {timeout}. Container image: {_container.Image.FullName}.");
    }
}

[CollectionDefinition(nameof(SqlServerCollection))]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
}
