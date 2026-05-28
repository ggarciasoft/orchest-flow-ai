using Microsoft.EntityFrameworkCore;
using OrchestFlowAI.Application.Abstractions;
using OrchestFlowAI.Domain.Entities;
using OrchestFlowAI.Domain.Enums;
using OrchestFlowAI.Infrastructure.Persistence;

namespace OrchestFlowAI.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IWorkflowRepository"/>.</summary>
public sealed class EfWorkflowRepository : IWorkflowRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfWorkflowRepository(OrchestFlowAIDbContext db) => _db = db;

    public Task<Workflow?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => _db.Workflows.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId, ct);

    public async Task<Workflow> CreateAsync(Workflow workflow, CancellationToken ct = default)
    { _db.Workflows.Add(workflow); await _db.SaveChangesAsync(ct); return workflow; }

    public async Task UpdateAsync(Workflow workflow, CancellationToken ct = default)
    { _db.Workflows.Update(workflow); await _db.SaveChangesAsync(ct); }

    public async Task<IReadOnlyList<Workflow>> ListAsync(Guid tenantId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Workflows.AsNoTracking().Where(w => w.TenantId == tenantId);
        if (!string.IsNullOrEmpty(search)) q = q.Where(w => w.Name.Contains(search));
        return await q.OrderByDescending(w => w.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<int> CountAsync(Guid tenantId, string? search, CancellationToken ct = default)
    {
        var q = _db.Workflows.AsNoTracking().Where(w => w.TenantId == tenantId);
        if (!string.IsNullOrEmpty(search)) q = q.Where(w => w.Name.Contains(search));
        return await q.CountAsync(ct);
    }

    public Task<WorkflowVersion?> GetActiveVersionAsync(Guid workflowId, CancellationToken ct = default)
        => _db.WorkflowVersions.AsNoTracking().FirstOrDefaultAsync(v => v.WorkflowId == workflowId && v.IsActive, ct);

    public async Task<WorkflowVersion> CreateVersionAsync(WorkflowVersion version, CancellationToken ct = default)
    { _db.WorkflowVersions.Add(version); await _db.SaveChangesAsync(ct); return version; }

    public Task<WorkflowVersion?> GetVersionAsync(Guid versionId, CancellationToken ct = default)
        => _db.WorkflowVersions.AsNoTracking().FirstOrDefaultAsync(v => v.Id == versionId, ct);

    public async Task ActivateVersionAsync(Guid versionId, Guid workflowId, CancellationToken ct = default)
    {
        // Load all active versions for this workflow, deactivate them, then activate the target
        var versions = await _db.WorkflowVersions.Where(v => v.WorkflowId == workflowId).ToListAsync(ct);
        foreach (var v in versions)
        {
            if (v.IsActive) v.Deactivate();
            if (v.Id == versionId) v.Activate();
        }
        await _db.SaveChangesAsync(ct);
    }

    public Task<Workflow?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Workflows.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, ct);

    public Task<IReadOnlyList<Workflow>> ListByTriggerTypeAsync(TriggerType triggerType, CancellationToken ct = default)
        => _db.Workflows.AsNoTracking().Where(w => w.TriggerType == triggerType && !w.IsDeleted)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Workflow>)t.Result, ct);

    public async Task<IReadOnlyList<WorkflowVersion>> ListVersionsAsync(Guid workflowId, CancellationToken ct = default)
    {
        var versions = await _db.WorkflowVersions.AsNoTracking()
            .Where(v => v.WorkflowId == workflowId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);
        return versions;
    }
}

/// <summary>EF Core implementation of <see cref="IExecutionRepository"/>.</summary>
public sealed class EfExecutionRepository : IExecutionRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfExecutionRepository(OrchestFlowAIDbContext db) => _db = db;

    public Task<WorkflowExecution?> GetAsync(Guid id, CancellationToken ct = default)
        => _db.WorkflowExecutions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<WorkflowExecution> CreateAsync(WorkflowExecution execution, CancellationToken ct = default)
    { _db.WorkflowExecutions.Add(execution); await _db.SaveChangesAsync(ct); return execution; }

    public async Task UpdateAsync(WorkflowExecution execution, CancellationToken ct = default)
    { _db.WorkflowExecutions.Update(execution); await _db.SaveChangesAsync(ct); }

    public async Task<IReadOnlyList<WorkflowExecution>> ListAsync(Guid tenantId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.WorkflowExecutions.AsNoTracking().Where(e => e.TenantId == tenantId);
        // Parse status string to enum for clean SQL translation â€” avoid .ToString() in EF LINQ expressions
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrchestFlowAI.Domain.Enums.ExecutionStatus>(status, ignoreCase: true, out var statusEnum))
            q = q.Where(e => e.Status == statusEnum);
        return await q.OrderByDescending(e => e.StartedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<NodeExecution>> GetNodeExecutionsAsync(Guid executionId, CancellationToken ct = default)
        => await _db.NodeExecutions.AsNoTracking().Where(n => n.WorkflowExecutionId == executionId).OrderBy(n => n.Step).ToListAsync(ct);

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
    private readonly OrchestFlowAIDbContext _db;
    public EfApprovalRepository(OrchestFlowAIDbContext db) => _db = db;

    public Task<ApprovalRequest?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => _db.ApprovalRequests.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);

    public Task<ApprovalRequest?> GetByNodeExecutionIdAsync(Guid nodeExecutionId, CancellationToken ct = default)
        => _db.ApprovalRequests.FirstOrDefaultAsync(a => a.NodeExecutionId == nodeExecutionId, ct);

    public async Task<ApprovalRequest> CreateAsync(ApprovalRequest approval, CancellationToken ct = default)
    { _db.ApprovalRequests.Add(approval); await _db.SaveChangesAsync(ct); return approval; }

    public async Task UpdateAsync(ApprovalRequest approval, CancellationToken ct = default)
    { _db.ApprovalRequests.Update(approval); await _db.SaveChangesAsync(ct); }

    public async Task<IReadOnlyList<ApprovalRequest>> ListPendingAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default)
        => await _db.ApprovalRequests
            .Where(a => a.TenantId == tenantId && a.Status == ApprovalStatus.Pending)
            .OrderByDescending(a => a.RequestedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
}

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
public sealed class EfUserRepository : IUserRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfUserRepository(OrchestFlowAIDbContext db) => _db = db;

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
    private readonly OrchestFlowAIDbContext _db;
    public EfDocumentRepository(OrchestFlowAIDbContext db) => _db = db;

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
    private readonly OrchestFlowAIDbContext _db;
    public EfAIUsageRepository(OrchestFlowAIDbContext db) => _db = db;

    public async Task<AIUsageLog> CreateAsync(AIUsageLog log, CancellationToken ct = default)
    { _db.AIUsageLogs.Add(log); await _db.SaveChangesAsync(ct); return log; }

    public async Task<IReadOnlyList<AIUsageLog>> GetByExecutionAsync(Guid executionId, CancellationToken ct = default)
        => await _db.AIUsageLogs.Where(l => l.WorkflowExecutionId == executionId).ToListAsync(ct);
}

/// <summary>EF Core implementation of <see cref="OrchestFlowAI.Engine.IEngineExecutionRepository"/>.</summary>
public sealed class EfEngineExecutionRepository : OrchestFlowAI.Engine.IEngineExecutionRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfEngineExecutionRepository(OrchestFlowAIDbContext db) => _db = db;

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

    /// <summary>Gets the workflow entity by id â€” used by the engine to read the retry policy.</summary>
    public Task<Workflow?> GetWorkflowAsync(Guid workflowId, CancellationToken ct = default)
        => _db.Workflows.FindAsync(new object[] { workflowId }, ct).AsTask();
}

/// <summary>EF Core implementation of <see cref="INodePresetRepository"/>.</summary>
public sealed class EfNodePresetRepository : INodePresetRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfNodePresetRepository(OrchestFlowAIDbContext db) => _db = db;

    public Task<NodePreset?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => _db.NodePresets.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<NodePreset>> ListByNodeTypeAsync(Guid tenantId, string? nodeType, CancellationToken ct = default)
    {
        var q = _db.NodePresets.Where(p => p.TenantId == tenantId);
        if (!string.IsNullOrEmpty(nodeType)) q = q.Where(p => p.NodeType == nodeType);
        return await q.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<NodePreset> CreateAsync(NodePreset preset, CancellationToken ct = default)
    { _db.NodePresets.Add(preset); await _db.SaveChangesAsync(ct); return preset; }

    public async Task UpdateAsync(NodePreset preset, CancellationToken ct = default)
    { _db.NodePresets.Update(preset); await _db.SaveChangesAsync(ct); }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var preset = await GetAsync(id, tenantId, ct);
        if (preset != null) { _db.NodePresets.Remove(preset); await _db.SaveChangesAsync(ct); }
    }
}

/// <summary>EF Core implementation of <see cref="ITenantRepository"/>.</summary>
public sealed class EfTenantRepository : ITenantRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfTenantRepository(OrchestFlowAIDbContext db) => _db = db;

    public Task<Tenant?> GetAsync(Guid id, CancellationToken ct = default)
        => _db.Tenants.FindAsync(new object[] { id }, ct).AsTask();

    public async Task<Tenant> CreateAsync(Tenant tenant, CancellationToken ct = default)
    { _db.Tenants.Add(tenant); await _db.SaveChangesAsync(ct); return tenant; }
}

/// <summary>EF Core implementation of <see cref="ITenantInviteRepository"/>.</summary>
public sealed class EfTenantInviteRepository : ITenantInviteRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfTenantInviteRepository(OrchestFlowAIDbContext db) => _db = db;

    public Task<TenantInvite?> GetByTokenAsync(string token, CancellationToken ct = default)
        => _db.TenantInvites.FirstOrDefaultAsync(i => i.Token == token, ct);

    public async Task<TenantInvite> CreateAsync(TenantInvite invite, CancellationToken ct = default)
    { _db.TenantInvites.Add(invite); await _db.SaveChangesAsync(ct); return invite; }

    public async Task UpdateAsync(TenantInvite invite, CancellationToken ct = default)
    { _db.TenantInvites.Update(invite); await _db.SaveChangesAsync(ct); }
}

/// <summary>EF Core implementation of <see cref="IGmailCredentialRepository"/>.</summary>
public sealed class EfGmailCredentialRepository : IGmailCredentialRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfGmailCredentialRepository(OrchestFlowAIDbContext db) => _db = db;

    public Task<GmailCredential?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => _db.GmailCredentials.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId, ct);

    public Task<GmailCredential?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct = default)
        => _db.GmailCredentials.AsNoTracking().FirstOrDefaultAsync(g => g.Name == name && g.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<GmailCredential>> ListAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.GmailCredentials.AsNoTracking().Where(g => g.TenantId == tenantId).OrderBy(g => g.Name).ToListAsync(ct);

    public async Task<GmailCredential> CreateAsync(GmailCredential credential, CancellationToken ct = default)
    { _db.GmailCredentials.Add(credential); await _db.SaveChangesAsync(ct); return credential; }

    public async Task UpdateAsync(GmailCredential credential, CancellationToken ct = default)
    { _db.GmailCredentials.Update(credential); await _db.SaveChangesAsync(ct); }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var cred = await _db.GmailCredentials.FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantId, ct);
        if (cred != null) { _db.GmailCredentials.Remove(cred); await _db.SaveChangesAsync(ct); }
    }
}

/// <summary>EF Core implementation of <see cref="ISecretRepository"/>.</summary>
public sealed class EfSecretRepository : ISecretRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfSecretRepository(OrchestFlowAIDbContext db) => _db = db;

    public async Task<IReadOnlyList<Secret>> ListAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.Secrets.AsNoTracking().Where(s => s.TenantId == tenantId).OrderBy(s => s.Name).ToListAsync(ct);

    public Task<Secret?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => _db.Secrets.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

    public Task<Secret?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct = default)
        => _db.Secrets.AsNoTracking().FirstOrDefaultAsync(s => s.Name == name && s.TenantId == tenantId, ct);

    public async Task<Secret> CreateAsync(Secret secret, CancellationToken ct = default)
    { _db.Secrets.Add(secret); await _db.SaveChangesAsync(ct); return secret; }

    public async Task UpdateAsync(Secret secret, CancellationToken ct = default)
    { _db.Secrets.Update(secret); await _db.SaveChangesAsync(ct); }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var s = await _db.Secrets.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (s != null) { _db.Secrets.Remove(s); await _db.SaveChangesAsync(ct); }
    }
}

/// <summary>EF Core implementation of <see cref="IFormRepository"/>.</summary>
public sealed class EfFormRepository : IFormRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfFormRepository(OrchestFlowAIDbContext db) => _db = db;

    public async Task<IReadOnlyList<Form>> ListAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.Forms.AsNoTracking().Where(f => f.TenantId == tenantId && !f.IsDeleted).OrderBy(f => f.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Form>> ListAllAsync(CancellationToken ct = default)
        => await _db.Forms.AsNoTracking().Where(f => !f.IsDeleted).ToListAsync(ct);

    public Task<Form?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => _db.Forms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantId && !f.IsDeleted, ct);

    public Task<Form?> GetBySlugAsync(string slug, Guid tenantId, CancellationToken ct = default)
        => _db.Forms.AsNoTracking().FirstOrDefaultAsync(f => f.Slug == slug && f.TenantId == tenantId && !f.IsDeleted, ct);

    public async Task<Form> CreateAsync(Form form, CancellationToken ct = default)
    { _db.Forms.Add(form); await _db.SaveChangesAsync(ct); return form; }

    public async Task UpdateAsync(Form form, CancellationToken ct = default)
    { _db.Forms.Update(form); await _db.SaveChangesAsync(ct); }

    public async Task<FormSubmission> CreateSubmissionAsync(FormSubmission submission, CancellationToken ct = default)
    { _db.FormSubmissions.Add(submission); await _db.SaveChangesAsync(ct); return submission; }

    public Task<FormSubmission?> GetSubmissionByExecutionAsync(Guid executionId, string nodeExecutionId, CancellationToken ct = default)
        => _db.FormSubmissions.AsNoTracking().FirstOrDefaultAsync(s => s.WorkflowExecutionId == executionId && s.NodeExecutionId == nodeExecutionId, ct);

    // ── Version management ───────────────────────────────────────────────────────────────────
    public async Task<FormVersion> CreateVersionAsync(FormVersion version, CancellationToken ct = default)
    { _db.FormVersions.Add(version); await _db.SaveChangesAsync(ct); return version; }

    public async Task<IReadOnlyList<FormVersion>> ListVersionsAsync(Guid formId, CancellationToken ct = default)
        => await _db.FormVersions.AsNoTracking().Where(v => v.FormId == formId).OrderByDescending(v => v.VersionNumber).ToListAsync(ct);

    public Task<FormVersion?> GetVersionAsync(Guid versionId, CancellationToken ct = default)
        => _db.FormVersions.AsNoTracking().FirstOrDefaultAsync(v => v.Id == versionId, ct);

    public Task<FormVersion?> GetActiveVersionAsync(Guid formId, CancellationToken ct = default)
        => _db.FormVersions.AsNoTracking().FirstOrDefaultAsync(v => v.FormId == formId && v.IsActive, ct);

    public async Task ActivateVersionAsync(Guid versionId, Guid formId, CancellationToken ct = default)
    {
        var versions = await _db.FormVersions.Where(v => v.FormId == formId).ToListAsync(ct);
        foreach (var v in versions) v.Deactivate();
        var target = versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new InvalidOperationException($"FormVersion {versionId} not found");
        target.Activate();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> GetNextVersionNumberAsync(Guid formId, CancellationToken ct = default)
    {
        var max = await _db.FormVersions.Where(v => v.FormId == formId).MaxAsync(v => (int?)v.VersionNumber, ct);
        return (max ?? 0) + 1;
    }
}

/// <summary>EF Core implementation of <see cref="IPlatformSettingsRepository"/>.</summary>
public sealed class EfPlatformSettingsRepository : IPlatformSettingsRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfPlatformSettingsRepository(OrchestFlowAIDbContext db) => _db = db;

    public async Task<IReadOnlyList<PlatformSetting>> ListAsync(Guid tenantId, CancellationToken ct = default)
        => await _db.PlatformSettings.AsNoTracking().Where(p => p.TenantId == tenantId).ToListAsync(ct);

    public Task<PlatformSetting?> GetAsync(Guid tenantId, string key, CancellationToken ct = default)
        => _db.PlatformSettings.AsNoTracking().FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Key == key, ct);

    public async Task UpsertAsync(Guid tenantId, string key, string value, CancellationToken ct = default)
    {
        var existing = await _db.PlatformSettings.FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Key == key, ct);
        if (existing != null)
        {
            existing.SetValue(value);
            _db.PlatformSettings.Update(existing);
        }
        else
        {
            _db.PlatformSettings.Add(PlatformSetting.Create(tenantId, key, value));
        }
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>EF Core implementation of <see cref="ICorrelationTokenRepository"/>.</summary>
public sealed class EfCorrelationTokenRepository : ICorrelationTokenRepository
{
    private readonly OrchestFlowAIDbContext _db;
    public EfCorrelationTokenRepository(OrchestFlowAIDbContext db) => _db = db;

    public async Task<CorrelationToken> CreateAsync(CorrelationToken token, CancellationToken ct = default)
    { _db.CorrelationTokens.Add(token); await _db.SaveChangesAsync(ct); return token; }

    public Task<CorrelationToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => _db.CorrelationTokens.AsNoTracking().FirstOrDefaultAsync(t => t.Token == token, ct);

    public async Task UpdateAsync(CorrelationToken token, CancellationToken ct = default)
    { _db.CorrelationTokens.Update(token); await _db.SaveChangesAsync(ct); }
}
