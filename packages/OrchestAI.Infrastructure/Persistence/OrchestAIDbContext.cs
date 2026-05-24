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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // All entities use private setters — use field access so EF can hydrate them
        modelBuilder.UsePropertyAccessMode(PropertyAccessMode.PreferFieldDuringConstruction);

        // ── Tenant ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
        });

        // ── User ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(320);
            e.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().IsRequired();
            e.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
        });

        // ── Workflow ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Workflow>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Name).IsRequired().HasMaxLength(300);
            e.Property(w => w.Description).HasMaxLength(2000);
            e.HasIndex(w => w.TenantId);
        });

        // ── WorkflowVersion ──────────────────────────────────────────────────
        modelBuilder.Entity<WorkflowVersion>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.DefinitionJson).IsRequired();
            e.HasIndex(v => v.WorkflowId);
            // Only one active version per workflow — enforced in code, not DB constraint
        });

        // ── WorkflowExecution ────────────────────────────────────────────────
        modelBuilder.Entity<WorkflowExecution>(e =>
        {
            e.HasKey(ex => ex.Id);
            e.Property(ex => ex.Status).HasConversion<string>().IsRequired();
            e.Property(ex => ex.InputJson).IsRequired();
            e.HasIndex(ex => ex.TenantId);
            e.HasIndex(ex => ex.WorkflowId);
            e.HasIndex(ex => ex.CorrelationId);
        });

        // ── NodeExecution ────────────────────────────────────────────────────
        modelBuilder.Entity<NodeExecution>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Status).HasConversion<string>().IsRequired();
            e.Property(n => n.NodeId).IsRequired().HasMaxLength(200);
            e.Property(n => n.NodeType).IsRequired().HasMaxLength(200);
            e.HasIndex(n => n.WorkflowExecutionId);
        });

        // ── ApprovalRequest ──────────────────────────────────────────────────
        modelBuilder.Entity<ApprovalRequest>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Status).HasConversion<string>().IsRequired();
            e.Property(a => a.PayloadJson).IsRequired();
            e.HasIndex(a => a.TenantId);
            e.HasIndex(a => a.WorkflowExecutionId);
        });

        // ── Document ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Filename).IsRequired().HasMaxLength(500);
            e.Property(d => d.MimeType).IsRequired().HasMaxLength(100);
            e.Property(d => d.StorageUri).IsRequired();
            e.HasIndex(d => d.TenantId);
        });

        // ── AIUsageLog ───────────────────────────────────────────────────────
        modelBuilder.Entity<AIUsageLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.WorkflowExecutionId);
        });
    }
}
