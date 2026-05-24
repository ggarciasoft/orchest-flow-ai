using OrchestAI.Application.Abstractions;
using OrchestAI.Domain.Entities;
using OrchestAI.Domain.Enums;
using System.Collections.Concurrent;

namespace OrchestAI.Infrastructure.Repositories;

/// <summary>In-memory stub implementation of <see cref="IWorkflowRepository"/>. Used when no database is configured.</summary>
public sealed class StubWorkflowRepository : IWorkflowRepository
{
    private static readonly ConcurrentDictionary<Guid, Workflow> _store = new();
    private static readonly ConcurrentDictionary<Guid, WorkflowVersion> _versions = new();

    public Task<Workflow?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default) => Task.FromResult(_store.GetValueOrDefault(id));
    public Task<Workflow> CreateAsync(Workflow workflow, CancellationToken ct = default) { _store[workflow.Id] = workflow; return Task.FromResult(workflow); }
    public Task UpdateAsync(Workflow workflow, CancellationToken ct = default) { _store[workflow.Id] = workflow; return Task.CompletedTask; }

    public Task<IReadOnlyList<Workflow>> ListAsync(Guid tenantId, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _store.Values.Where(w => w.TenantId == tenantId);
        if (!string.IsNullOrEmpty(search)) q = q.Where(w => w.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        IReadOnlyList<Workflow> list = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(list);
    }

    public Task<int> CountAsync(Guid tenantId, string? search, CancellationToken ct = default)
    {
        var q = _store.Values.Where(w => w.TenantId == tenantId);
        if (!string.IsNullOrEmpty(search)) q = q.Where(w => w.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(q.Count());
    }

    public Task<WorkflowVersion?> GetActiveVersionAsync(Guid workflowId, CancellationToken ct = default)
        => Task.FromResult(_versions.Values.FirstOrDefault(v => v.WorkflowId == workflowId && v.IsActive));

    public Task<WorkflowVersion?> GetVersionAsync(Guid versionId, CancellationToken ct = default)
        => Task.FromResult(_versions.GetValueOrDefault(versionId));

    public Task<WorkflowVersion> CreateVersionAsync(WorkflowVersion version, CancellationToken ct = default) { _versions[version.Id] = version; return Task.FromResult(version); }

    public Task ActivateVersionAsync(Guid versionId, Guid workflowId, CancellationToken ct = default)
    {
        foreach (var v in _versions.Values.Where(x => x.WorkflowId == workflowId)) v.Deactivate();
        if (_versions.TryGetValue(versionId, out var ver)) ver.Activate();
        return Task.CompletedTask;
    }

    public Task<Workflow?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Workflow?>(_store.Values.FirstOrDefault(w => w.Id == id && !w.IsDeleted));

    public Task<IReadOnlyList<Workflow>> ListByTriggerTypeAsync(TriggerType triggerType, CancellationToken ct = default)
    {
        IReadOnlyList<Workflow> list = _store.Values.Where(w => w.TriggerType == triggerType && !w.IsDeleted).ToList();
        return Task.FromResult(list);
    }
}

/// <summary>In-memory stub implementation of <see cref="IExecutionRepository"/>.</summary>
public sealed class StubExecutionRepository : IExecutionRepository
{
    private static readonly ConcurrentDictionary<Guid, WorkflowExecution> _execs = new();
    private static readonly ConcurrentDictionary<Guid, NodeExecution> _nodes = new();

    public Task<WorkflowExecution?> GetAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_execs.GetValueOrDefault(id));
    public Task<WorkflowExecution> CreateAsync(WorkflowExecution execution, CancellationToken ct = default) { _execs[execution.Id] = execution; return Task.FromResult(execution); }
    public Task UpdateAsync(WorkflowExecution execution, CancellationToken ct = default) { _execs[execution.Id] = execution; return Task.CompletedTask; }

    public Task<IReadOnlyList<WorkflowExecution>> ListAsync(Guid tenantId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _execs.Values.Where(e => e.TenantId == tenantId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(e => e.Status.ToString() == status);
        IReadOnlyList<WorkflowExecution> list = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<NodeExecution>> GetNodeExecutionsAsync(Guid executionId, CancellationToken ct = default)
    {
        IReadOnlyList<NodeExecution> l = _nodes.Values.Where(n => n.WorkflowExecutionId == executionId).OrderBy(n => n.Step).ToList();
        return Task.FromResult(l);
    }

    public Task<NodeExecution> CreateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default) { _nodes[nodeExecution.Id] = nodeExecution; return Task.FromResult(nodeExecution); }
    public Task UpdateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default) { _nodes[nodeExecution.Id] = nodeExecution; return Task.CompletedTask; }
    public Task<NodeExecution?> GetNodeExecutionAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_nodes.GetValueOrDefault(id));
}

/// <summary>In-memory stub implementation of <see cref="IApprovalRepository"/>.</summary>
public sealed class StubApprovalRepository : IApprovalRepository
{
    private static readonly ConcurrentDictionary<Guid, ApprovalRequest> _store = new();

    public Task<ApprovalRequest?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default) => Task.FromResult(_store.GetValueOrDefault(id));
    public Task<ApprovalRequest> CreateAsync(ApprovalRequest approval, CancellationToken ct = default) { _store[approval.Id] = approval; return Task.FromResult(approval); }
    public Task UpdateAsync(ApprovalRequest approval, CancellationToken ct = default) { _store[approval.Id] = approval; return Task.CompletedTask; }

    public Task<IReadOnlyList<ApprovalRequest>> ListPendingAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        IReadOnlyList<ApprovalRequest> list = _store.Values
            .Where(a => a.TenantId == tenantId && a.Status == ApprovalStatus.Pending)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(list);
    }
}

/// <summary>In-memory stub implementation of <see cref="IDocumentRepository"/>.</summary>
public sealed class StubDocumentRepository : IDocumentRepository
{
    private static readonly ConcurrentDictionary<Guid, Document> _store = new();

    public Task<Document?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default) => Task.FromResult(_store.GetValueOrDefault(id));
    public Task<Document> CreateAsync(Document document, CancellationToken ct = default) { _store[document.Id] = document; return Task.FromResult(document); }
    public Task<IReadOnlyList<Document>> ListByOwnerAsync(Guid ownerId, Guid tenantId, CancellationToken ct = default)
    {
        IReadOnlyList<Document> list = _store.Values.Where(d => d.OwnerId == ownerId && d.TenantId == tenantId).ToList();
        return Task.FromResult(list);
    }
}

/// <summary>In-memory stub implementation of <see cref="IAIUsageRepository"/>.</summary>
public sealed class StubAIUsageRepository : IAIUsageRepository
{
    private static readonly ConcurrentDictionary<Guid, AIUsageLog> _store = new();

    public Task<AIUsageLog> CreateAsync(AIUsageLog log, CancellationToken ct = default) { _store[log.Id] = log; return Task.FromResult(log); }
    public Task<IReadOnlyList<AIUsageLog>> GetByExecutionAsync(Guid executionId, CancellationToken ct = default)
    {
        IReadOnlyList<AIUsageLog> list = _store.Values.Where(l => l.WorkflowExecutionId == executionId).ToList();
        return Task.FromResult(list);
    }
}

/// <summary>In-memory stub implementation of <see cref="IUserRepository"/>.</summary>
public sealed class StubUserRepository : IUserRepository
{
    private static readonly ConcurrentDictionary<Guid, User> _store = new();

    public Task<User?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct = default)
        => Task.FromResult(_store.Values.FirstOrDefault(u => u.Email == email && u.TenantId == tenantId));
    public Task<User?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => Task.FromResult(_store.Values.FirstOrDefault(u => u.Id == id && u.TenantId == tenantId));
    public Task<User> CreateAsync(User user, CancellationToken ct = default) { _store[user.Id] = user; return Task.FromResult(user); }
}

/// <summary>In-memory stub for <see cref="OrchestAI.Engine.IEngineExecutionRepository"/>.</summary>
public sealed class StubEngineExecutionRepository : OrchestAI.Engine.IEngineExecutionRepository
{
    private static readonly ConcurrentDictionary<Guid, WorkflowExecution> _execs = new();
    private static readonly ConcurrentDictionary<Guid, NodeExecution> _nodes = new();
    private static readonly ConcurrentDictionary<Guid, WorkflowVersion> _versions = new();
    private static readonly ConcurrentDictionary<Guid, ApprovalRequest> _approvals = new();

    public Task<WorkflowExecution?> GetExecutionAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_execs.GetValueOrDefault(id));
    public Task UpdateExecutionAsync(WorkflowExecution execution, CancellationToken ct = default) { _execs[execution.Id] = execution; return Task.CompletedTask; }
    public Task<WorkflowVersion?> GetWorkflowVersionAsync(Guid versionId, CancellationToken ct = default) => Task.FromResult(_versions.GetValueOrDefault(versionId));
    public Task<NodeExecution> CreateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default) { _nodes[nodeExecution.Id] = nodeExecution; return Task.FromResult(nodeExecution); }
    public Task UpdateNodeExecutionAsync(NodeExecution nodeExecution, CancellationToken ct = default) { _nodes[nodeExecution.Id] = nodeExecution; return Task.CompletedTask; }
    public Task<NodeExecution?> GetNodeExecutionAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_nodes.GetValueOrDefault(id));
    public Task<IReadOnlyList<NodeExecution>> GetNodeExecutionsAsync(Guid executionId, CancellationToken ct = default)
    {
        IReadOnlyList<NodeExecution> l = _nodes.Values.Where(n => n.WorkflowExecutionId == executionId).OrderBy(n => n.Step).ToList();
        return Task.FromResult(l);
    }
    public Task<ApprovalRequest> CreateApprovalAsync(ApprovalRequest approval, CancellationToken ct = default) { _approvals[approval.Id] = approval; return Task.FromResult(approval); }
}

/// <summary>In-memory stub implementation of <see cref="INodePresetRepository"/>.</summary>
public sealed class StubNodePresetRepository : INodePresetRepository
{
    private readonly ConcurrentDictionary<Guid, NodePreset> _store = new();

    public Task<NodePreset?> GetAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => Task.FromResult(_store.Values.FirstOrDefault(p => p.Id == id && p.TenantId == tenantId));

    public Task<IReadOnlyList<NodePreset>> ListByNodeTypeAsync(Guid tenantId, string? nodeType, CancellationToken ct = default)
    {
        var q = _store.Values.Where(p => p.TenantId == tenantId);
        if (!string.IsNullOrEmpty(nodeType)) q = q.Where(p => p.NodeType == nodeType);
        IReadOnlyList<NodePreset> list = q.OrderBy(p => p.Name).ToList();
        return Task.FromResult(list);
    }

    public Task<NodePreset> CreateAsync(NodePreset preset, CancellationToken ct = default) { _store[preset.Id] = preset; return Task.FromResult(preset); }
    public Task UpdateAsync(NodePreset preset, CancellationToken ct = default) { _store[preset.Id] = preset; return Task.CompletedTask; }
    public Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default) { _store.TryRemove(id, out _); return Task.CompletedTask; }
}
