using OrchestFlowAI.SDK.Context;
namespace OrchestFlowAI.SDK.Testing;

public sealed class TestContextBuilder
{
    private Dictionary<string, object?> _inputs = new();
    private Dictionary<string, object?> _config = new();
    private Dictionary<string, object?> _workflowInputs = new();
    private Dictionary<string, IReadOnlyDictionary<string, object?>> _nodeOutputs = new();
    private Guid _executionId = Guid.NewGuid();
    private Guid _tenantId = Guid.NewGuid();
    private IServiceProvider? _services;

    public TestContextBuilder WithInputs(Dictionary<string, object?> inputs) { _inputs = inputs; return this; }
    public TestContextBuilder WithConfig(Dictionary<string, object?> config) { _config = config; return this; }
    public TestContextBuilder WithWorkflowInputs(Dictionary<string, object?> inputs) { _workflowInputs = inputs; return this; }
    /// <summary>Adds simulated upstream node outputs keyed by node id.</summary>
    public TestContextBuilder WithNodeOutputs(Dictionary<string, IReadOnlyDictionary<string, object?>> nodeOutputs) { _nodeOutputs = nodeOutputs; return this; }
    public TestContextBuilder WithExecutionId(Guid id) { _executionId = id; return this; }
    /// <summary>Provides a custom <see cref="IServiceProvider"/> for nodes that resolve DI services.</summary>
    public TestContextBuilder WithServices(IServiceProvider services) { _services = services; return this; }

    public WorkflowExecutionContext Build() => new()
    {
        ExecutionId = _executionId,
        TenantId = _tenantId,
        CorrelationId = Guid.NewGuid().ToString(),
        NodeInputs = _inputs,
        NodeConfig = _config,
        WorkflowInputs = _workflowInputs,
        NodeOutputs = _nodeOutputs,
        Services = _services ?? new TestServiceProvider(),
        CurrentNodeId = "test-node"
    };

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
