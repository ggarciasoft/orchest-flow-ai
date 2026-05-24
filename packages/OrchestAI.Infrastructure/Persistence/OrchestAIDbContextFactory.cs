using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OrchestAI.Infrastructure.Persistence;

namespace OrchestAI.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations tooling.
/// Used by `dotnet ef migrations add` without needing the full app running.
/// </summary>
public sealed class OrchestAIDbContextFactory : IDesignTimeDbContextFactory<OrchestAIDbContext>
{
    public OrchestAIDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<OrchestAIDbContext>();

        // Use env var or fall back to a local dev placeholder for migration generation
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=localhost;Database=orchestai_dev;Username=postgres;Password=changeme";

        opts.UseNpgsql(connectionString);
        return new OrchestAIDbContext(opts.Options);
    }
}
