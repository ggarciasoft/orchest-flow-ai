using Microsoft.EntityFrameworkCore;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;

namespace OrchestAI.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for OrchestAI.
/// All domain entities use private setters — EF accesses them via backing fields.
/// </summary>
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
    public DbSet<NodePreset> NodePresets => Set<NodePreset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // All entities use private setters — use field access so EF can hydrate them
        modelBuilder.UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(320);
            e.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().IsRequired();
            e.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        });

        modelBuilder.Entity<Workflow>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Name).IsRequired().HasMaxLength(300);
            e.Property(w => w.Description).HasMaxLength(2000);
            e.Property(w => w.TriggerType).HasConversion<string>().IsRequired();
            e.Property(w => w.WebhookSecret).HasMaxLength(500);
            e.Property(w => w.CronExpression).HasMaxLength(100);
            e.HasIndex(w => w.TenantId);
            // Map RetryPolicy as owned/flattened columns
            e.OwnsOne(w => w.RetryPolicy, rp =>
            {
                rp.Property(p => p.MaxAttempts).HasColumnName("RetryMaxAttempts").HasDefaultValue(0);
                rp.Property(p => p.BackoffMs).HasColumnName("RetryBackoffMs").HasDefaultValue(0);
                rp.Property(p => p.BackoffMultiplier).HasColumnName("RetryBackoffMultiplier").HasDefaultValue(2.0);
            });
        });

        modelBuilder.Entity<WorkflowVersion>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.DefinitionJson).IsRequired();
            e.HasIndex(v => v.WorkflowId);
        });

        modelBuilder.Entity<WorkflowExecution>(e =>
        {
            e.HasKey(ex => ex.Id);
            e.Property(ex => ex.Status).HasConversion<string>().IsRequired();
            e.Property(ex => ex.InputJson).IsRequired();
            e.HasIndex(ex => ex.TenantId);
            e.HasIndex(ex => ex.WorkflowId);
            e.HasIndex(ex => ex.CorrelationId);
        });

        modelBuilder.Entity<NodeExecution>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Status).HasConversion<string>().IsRequired();
            e.Property(n => n.NodeId).IsRequired().HasMaxLength(200);
            e.Property(n => n.NodeType).IsRequired().HasMaxLength(200);
            e.Property(n => n.AttemptNumber).HasDefaultValue(1);
            e.HasIndex(n => n.WorkflowExecutionId);
        });

        modelBuilder.Entity<ApprovalRequest>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Status).HasConversion<string>().IsRequired();
            e.Property(a => a.PayloadJson).IsRequired();
            e.HasIndex(a => a.TenantId);
            e.HasIndex(a => a.WorkflowExecutionId);
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Filename).IsRequired().HasMaxLength(500);
            e.Property(d => d.MimeType).IsRequired().HasMaxLength(100);
            e.Property(d => d.StorageUri).IsRequired();
            e.HasIndex(d => d.TenantId);
        });

        modelBuilder.Entity<AIUsageLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.WorkflowExecutionId);
        });

        modelBuilder.Entity<NodePreset>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.NodeType).IsRequired().HasMaxLength(200);
            e.Property(p => p.ConfigJson).IsRequired();
            e.HasIndex(p => new { p.TenantId, p.NodeType });
        });
    }
}
