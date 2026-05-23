using OrchestAI.SDK.Context;
namespace OrchestAI.SDK.Testing;

public sealed class TestContextBuilder
{
    private Dictionary<string, object?> _inputs = new();
    private Dictionary<string, object?> _config = new();
    private Dictionary<string, object?> _workflowInputs = new();
    private Guid _executionId = Guid.NewGuid();
    private Guid _tenantId = Guid.NewGuid();

    public TestContextBuilder WithInputs(Dictionary<string, object?> inputs) { _inputs = inputs; return this; }
    public TestContextBuilder WithConfig(Dictionary<string, object?> config) { _config = config; return this; }
    public TestContextBuilder WithWorkflowInputs(Dictionary<string, object?> inputs) { _workflowInputs = inputs; return this; }
    public TestContextBuilder WithExecutionId(Guid id) { _executionId = id; return this; }

    public WorkflowExecutionContext Build() => new()
    {
        ExecutionId = _executionId,
        TenantId = _tenantId,
        CorrelationId = Guid.NewGuid().ToString(),
        NodeInputs = _inputs,
        NodeConfig = _config,
        WorkflowInputs = _workflowInputs,
        Services = new TestServiceProvider(),
        CurrentNodeId = "test-node"
    };

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
