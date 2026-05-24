using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OrchestFlowAI.Infrastructure.Persistence;

namespace OrchestFlowAI.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations tooling.
/// Used by `dotnet ef migrations add` without needing the full app running.
/// </summary>
public sealed class OrchestFlowAIDbContextFactory : IDesignTimeDbContextFactory<OrchestFlowAIDbContext>
{
    public OrchestFlowAIDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<OrchestFlowAIDbContext>();

        // Use env var or fall back to a local dev placeholder for migration generation
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=localhost;Database=OrchestFlowAI_dev;Username=postgres;Password=changeme";

        opts.UseNpgsql(connectionString);
        return new OrchestFlowAIDbContext(opts.Options);
    }
}
