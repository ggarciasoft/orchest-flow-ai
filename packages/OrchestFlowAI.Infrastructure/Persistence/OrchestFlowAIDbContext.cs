using Microsoft.EntityFrameworkCore;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;

namespace OrchestFlowAI.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for OrchestFlowAI.
/// All domain entities use private setters — EF accesses them via backing fields.
/// </summary>
public sealed class OrchestFlowAIDbContext : DbContext
{
    public OrchestFlowAIDbContext(DbContextOptions<OrchestFlowAIDbContext> options) : base(options) { }

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
    public DbSet<TenantInvite> TenantInvites => Set<TenantInvite>();
    public DbSet<ExecutionQueueItem> ExecutionQueue => Set<ExecutionQueueItem>();
    public DbSet<GmailCredential> GmailCredentials => Set<GmailCredential>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

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

        modelBuilder.Entity<TenantInvite>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Email).IsRequired().HasMaxLength(320);
            e.Property(i => i.Role).IsRequired().HasMaxLength(50);
            e.Property(i => i.Token).IsRequired().HasMaxLength(64);
            e.HasIndex(i => i.Token).IsUnique();
            e.HasIndex(i => i.TenantId);
        });

        modelBuilder.Entity<NodePreset>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.NodeType).IsRequired().HasMaxLength(200);
            e.Property(p => p.ConfigJson).IsRequired();
            e.HasIndex(p => new { p.TenantId, p.NodeType });
        });

        modelBuilder.Entity<GmailCredential>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Name).IsRequired().HasMaxLength(200);
            e.Property(g => g.ClientId).IsRequired().HasMaxLength(500);
            e.Property(g => g.ClientSecret).IsRequired().HasMaxLength(500);
            e.Property(g => g.RefreshToken).IsRequired();
            e.Property(g => g.Email).HasMaxLength(320);
            e.HasIndex(g => new { g.TenantId, g.Name }).IsUnique();
            e.HasIndex(g => g.TenantId);
        });

        modelBuilder.Entity<PlatformSetting>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Key).IsRequired().HasMaxLength(200);
            e.Property(p => p.Value).IsRequired();
            e.HasIndex(p => new { p.TenantId, p.Key }).IsUnique();
        });

        modelBuilder.Entity<ExecutionQueueItem>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.TriggeredBy).IsRequired().HasMaxLength(50);
            e.Property(q => q.Payload).IsRequired();
            e.Property(q => q.Status).HasConversion<string>().IsRequired();
            // Primary access pattern: workers poll for Pending items ordered by CreatedAt
            e.HasIndex(q => new { q.Status, q.CreatedAt });
            e.HasIndex(q => q.TenantId);
        });
    }
}
