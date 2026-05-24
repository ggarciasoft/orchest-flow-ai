using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OrchestAI.Infrastructure.Persistence;

namespace OrchestAI.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations tooling.
/// Uses a local PostgreSQL connection for generating migrations without needing the full app running.
/// </summary>
public sealed class OrchestAIDbContextFactory : IDesignTimeDbContextFactory<OrchestAIDbContext>
{
    public OrchestAIDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<OrchestAIDbContext>();

        // Use env var or fall back to a local dev connection string for migrations
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=localhost;Database=orchestai_dev;Username=postgres;Password=postgres";

        opts.UseNpgsql(connectionString);
        return new OrchestAIDbContext(opts.Options);
    }
}
