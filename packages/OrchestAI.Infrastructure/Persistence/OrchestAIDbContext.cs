using Microsoft.EntityFrameworkCore;
using OrchestAI.Domain.Entities;

namespace OrchestAI.Infrastructure.Persistence;

public sealed class OrchestAIDbContext : DbContext
{
    public OrchestAIDbContext(DbContextOptions<OrchestAIDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowVersion> WorkflowVersions => Set<WorkflowVersion>();
    public DbSet<WorkflowExecution> WorkflowExecutions => Set<WorkflowExecution>();
    public DbSet<NodeExecution> NodeExecutions => Set<NodeExecution>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AIUsageLog> AIUsageLogs => Set<AIUsageLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>().HasKey(t => t.Id);
        modelBuilder.Entity<User>().HasKey(u => u.Id);
        modelBuilder.Entity<Workflow>().HasKey(w => w.Id);
        modelBuilder.Entity<WorkflowVersion>().HasKey(v => v.Id);
        modelBuilder.Entity<WorkflowExecution>().HasKey(e => e.Id);
        modelBuilder.Entity<NodeExecution>().HasKey(n => n.Id);
        modelBuilder.Entity<ApprovalRequest>().HasKey(a => a.Id);
        modelBuilder.Entity<Document>().HasKey(d => d.Id);
        modelBuilder.Entity<AIUsageLog>().HasKey(l => l.Id);

        modelBuilder.Entity<Tenant>().Property(t => t.Name).IsRequired();
        modelBuilder.Entity<User>().Property(u => u.Email).IsRequired();
        modelBuilder.Entity<Workflow>().Property(w => w.Name).IsRequired();
        modelBuilder.Entity<WorkflowVersion>().Property(v => v.DefinitionJson).IsRequired();
        modelBuilder.Entity<WorkflowExecution>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<NodeExecution>().Property(n => n.Status).HasConversion<string>();
        modelBuilder.Entity<ApprovalRequest>().Property(a => a.Status).HasConversion<string>();
        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();

        modelBuilder.Entity<Workflow>().HasIndex(w => w.TenantId);
        modelBuilder.Entity<WorkflowExecution>().HasIndex(e => e.TenantId);
        modelBuilder.Entity<WorkflowExecution>().HasIndex(e => e.WorkflowId);
        modelBuilder.Entity<NodeExecution>().HasIndex(n => n.WorkflowExecutionId);
        modelBuilder.Entity<ApprovalRequest>().HasIndex(a => a.TenantId);
        modelBuilder.Entity<WorkflowVersion>().HasIndex(v => v.WorkflowId);
    }
}