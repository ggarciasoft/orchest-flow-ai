using Microsoft.EntityFrameworkCore;
using OrchestAI.Domain.Entities;
using OrchestAI.Application.Abstractions;
using OrchestAI.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrchestAI.Infrastructure.Repositories;

public class EfWorkflowRepository : IWorkflowRepository
{
    private readonly OrchestAIDbContext _context;

    public EfWorkflowRepository(OrchestAIDbContext context)
    {
        _context = context;
    }

    public async Task<Workflow?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        return await _context.Workflows.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId, ct);
    }

    public async Task<Workflow> CreateAsync(Workflow workflow, CancellationToken ct = default)
    {
        await _context.Workflows.AddAsync(workflow, ct);
        await _context.SaveChangesAsync(ct);
        return workflow;
    }

    public async Task UpdateAsync(Workflow workflow, CancellationToken ct = default)
    {
        _context.Workflows.Update(workflow);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Workflow>> ListAsync(Guid tenantId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<Workflow> query = _context.Workflows.Where(w => w.TenantId == tenantId);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(w => w.Name.Contains(search));
        }

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> CountAsync(Guid tenantId, string? search, CancellationToken ct = default)
    {
        IQueryable<Workflow> query = _context.Workflows.Where(w => w.TenantId == tenantId);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(w => w.Name.Contains(search));
        }

        return await query.CountAsync(ct);
    }

    public async Task<WorkflowVersion?> GetActiveVersionAsync(Guid workflowId, CancellationToken ct = default)
    {
        return await _context.WorkflowVersions.FirstOrDefaultAsync(v => v.WorkflowId == workflowId && v.IsActive, ct);
    }

    public async Task<WorkflowVersion> CreateVersionAsync(WorkflowVersion version, CancellationToken ct = default)
    {
        await _context.WorkflowVersions.AddAsync(version, ct);
        await _context.SaveChangesAsync(ct);
        return version;
    }

    public async Task<WorkflowVersion?> GetVersionAsync(Guid versionId, CancellationToken ct = default)
    {
        return await _context.WorkflowVersions.FindAsync(new object[] { versionId }, ct);
    }

    public async Task ActivateVersionAsync(Guid versionId, Guid workflowId, CancellationToken ct = default)
    {
        var activeVersions = _context.WorkflowVersions.Where(v => v.WorkflowId == workflowId && v.IsActive);
        await activeVersions.ForEachAsync(v => v.Deactivate(), ct);

        var version = await _context.WorkflowVersions.FindAsync(new object[] { versionId }, ct);

        if (version != null)
        {
            version.Activate();
        }

        await _context.SaveChangesAsync(ct);
    }
}

// Add similar EF implementation classes: EfExecutionRepository, EfApprovalRepository, EfUserRepository, EfDocumentRepository, EfAIUsageRepository

/// <summary>EF Core implementation of <see cref="IExecutionRepository"/>.</summary>
public sealed class EfExecutionRepository : IExecutionRepository
{
    private readonly OrchestAIDbContext _db;
    public EfExecutionRepository(OrchestAIDbContext db) => _db = db;

    public Task<WorkflowExecution?> GetAsync(Guid id, CancellationToken ct = default)
        => _db.WorkflowExecutions.FindAsync(new object[] { id }, ct).AsTask();

    public async Task<WorkflowExecution> CreateAsync(WorkflowExecution execution, CancellationToken ct = default)
    { _db.WorkflowExecutions.Add(execution); await _db.SaveChangesAsync(ct); return execution; }

    public async Task UpdateAsync(WorkflowExecution execution, CancellationToken ct = default)
    { _db.WorkflowExecutions.Update(execution); await _db.SaveChangesAsync(ct); }

    public async Task<IReadOnlyList<WorkflowExecution>> ListAsync(Guid tenantId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.WorkflowExecutions.Where(e => e.TenantId == tenantId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(e => e.Status.ToString() == status);
        return await q.OrderByDescending(e => e.StartedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<NodeExecution>> GetNodeExecutionsAsync(Guid executionId, CancellationToken ct = default)
        => await _db.NodeExecutions.Where(n => n.WorkflowExecutionId == executionId).OrderBy(n => n.Step).ToListAsync(ct);

    public async Task<NodeExecution> CreateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default)
    { _db.NodeExecutions.Add(nodeExecution); await _db.SaveChangesAsync(ct); return nodeExecution; }

    public async Task UpdateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default)
    { _db.NodeExecutions.Update(nodeExecution); await _db.SaveChangesAsync(ct); }

    public Task<NodeExecution?> GetNodeExecutionAsync(Guid id, CancellationToken ct = default)
        => _db.NodeExecutions.FindAsync(new object[] { id }, ct).AsTask();
}

/// <summary>EF Core implementation of <see cref="IApprovalRepository"/>.</summary>
public sealed class EfApprovalRepository : IApprovalRepository
{
    private readonly OrchestAIDbContext _db;
    public EfApprovalRepository(OrchestAIDbContext db) => _db = db;

    public Task<ApprovalRequest?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => _db.ApprovalRequests.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);

    public async Task<ApprovalRequest> CreateAsync(ApprovalRequest approval, CancellationToken ct = default)
    { _db.ApprovalRequests.Add(approval); await _db.SaveChangesAsync(ct); return approval; }

    public async Task UpdateAsync(ApprovalRequest approval, CancellationToken ct = default)
    { _db.ApprovalRequests.Update(approval); await _db.SaveChangesAsync(ct); }

    public async Task<IReadOnlyList<ApprovalRequest>> ListPendingAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default)
        => await _db.ApprovalRequests
            .Where(a => a.TenantId == tenantId && a.Status == OrchestAI.Domain.Enums.ApprovalStatus.Pending)
            .OrderByDescending(a => a.RequestedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
}

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
public sealed class EfUserRepository : IUserRepository
{
    private readonly OrchestAIDbContext _db;
    public EfUserRepository(OrchestAIDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.TenantId == tenantId, ct);

    public Task<User?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId, ct);

    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    { _db.Users.Add(user); await _db.SaveChangesAsync(ct); return user; }
}

/// <summary>EF Core implementation of <see cref="IDocumentRepository"/>.</summary>
public sealed class EfDocumentRepository : IDocumentRepository
{
    private readonly OrchestAIDbContext _db;
    public EfDocumentRepository(OrchestAIDbContext db) => _db = db;

    public Task<Document?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => _db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId, ct);

    public async Task<Document> CreateAsync(Document document, CancellationToken ct = default)
    { _db.Documents.Add(document); await _db.SaveChangesAsync(ct); return document; }

    public async Task<IReadOnlyList<Document>> ListByOwnerAsync(Guid ownerId, Guid tenantId, CancellationToken ct = default)
        => await _db.Documents.Where(d => d.OwnerId == ownerId && d.TenantId == tenantId).ToListAsync(ct);
}

/// <summary>EF Core implementation of <see cref="IAIUsageRepository"/>.</summary>
public sealed class EfAIUsageRepository : IAIUsageRepository
{
    private readonly OrchestAIDbContext _db;
    public EfAIUsageRepository(OrchestAIDbContext db) => _db = db;

    public async Task<AIUsageLog> CreateAsync(AIUsageLog log, CancellationToken ct = default)
    { _db.AIUsageLogs.Add(log); await _db.SaveChangesAsync(ct); return log; }

    public async Task<IReadOnlyList<AIUsageLog>> GetByExecutionAsync(Guid executionId, CancellationToken ct = default)
        => await _db.AIUsageLogs.Where(l => l.WorkflowExecutionId == executionId).ToListAsync(ct);
}

/// <summary>EF Core implementation of <see cref="OrchestAI.Engine.IEngineExecutionRepository"/>.</summary>
public sealed class EfEngineExecutionRepository : OrchestAI.Engine.IEngineExecutionRepository
{
    private readonly OrchestAIDbContext _db;
    public EfEngineExecutionRepository(OrchestAIDbContext db) => _db = db;

    public Task<WorkflowExecution?> GetExecutionAsync(Guid id, CancellationToken ct = default)
        => _db.WorkflowExecutions.FindAsync(new object[] { id }, ct).AsTask();

    public async Task UpdateExecutionAsync(WorkflowExecution execution, CancellationToken ct = default)
    { _db.WorkflowExecutions.Update(execution); await _db.SaveChangesAsync(ct); }

    public Task<WorkflowVersion?> GetWorkflowVersionAsync(Guid versionId, CancellationToken ct = default)
        => _db.WorkflowVersions.FindAsync(new object[] { versionId }, ct).AsTask();

    public async Task<NodeExecution> CreateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default)
    { _db.NodeExecutions.Add(nodeExecution); await _db.SaveChangesAsync(ct); return nodeExecution; }

    public async Task UpdateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default)
    { _db.NodeExecutions.Update(nodeExecution); await _db.SaveChangesAsync(ct); }

    public Task<NodeExecution?> GetNodeExecutionAsync(Guid id, CancellationToken ct = default)
        => _db.NodeExecutions.FindAsync(new object[] { id }, ct).AsTask();

    public async Task<IReadOnlyList<NodeExecution>> GetNodeExecutionsAsync(Guid executionId, CancellationToken ct = default)
        => await _db.NodeExecutions.Where(n => n.WorkflowExecutionId == executionId).OrderBy(n => n.Step).ToListAsync(ct);

    public async Task<ApprovalRequest> CreateApprovalAsync(ApprovalRequest approval, CancellationToken ct = default)
    { _db.ApprovalRequests.Add(approval); await _db.SaveChangesAsync(ct); return approval; }
}