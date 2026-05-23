# Node SDK

The Node SDK is the contract that every workflow node implements. It's deliberately small so that anyone — internal teams or third parties — can author nodes without touching the engine.

---

## 1. Concepts

| Concept              | Meaning                                                                |
| -------------------- | ---------------------------------------------------------------------- |
| **Node type**        | Globally unique string id, e.g. `ai.contract-risk-analysis`.           |
| **Descriptor**       | Static metadata: name, description, category, inputs, outputs, config. |
| **Node**             | Runtime implementation of execution logic.                             |
| **Configuration**    | Per-instance settings declared in workflow JSON.                       |
| **Inputs / Outputs** | Typed ports the engine wires between nodes.                            |

Nodes are **stateless**. They never persist their own execution state. The engine owns state.

---

## 2. Interfaces

### `IWorkflowNode`

```csharp
public interface IWorkflowNode
{
    string Type { get; }

    Task<NodeExecutionResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken);
}
```

### `IWorkflowNodeDescriptor`

```csharp
public interface IWorkflowNodeDescriptor
{
    string Type { get; }
    string DisplayName { get; }
    string Description { get; }
    string Category { get; }            // ai | documents | logic | human | integrations | system
    string Version { get; }             // semver of the node implementation
    string? IconKey { get; }            // UI hint

    IReadOnlyCollection<NodeInputDefinition>  Inputs        { get; }
    IReadOnlyCollection<NodeOutputDefinition> Outputs       { get; }
    IReadOnlyCollection<NodeConfigDefinition> Configuration { get; }
}
```

### Port and Config Definitions

```csharp
public sealed record NodeInputDefinition(
    string Key,
    string DisplayName,
    string Description,
    DataType Type,
    bool Required,
    object? DefaultValue = null);

public sealed record NodeOutputDefinition(
    string Key,
    string DisplayName,
    string Description,
    DataType Type);

public sealed record NodeConfigDefinition(
    string Key,
    string DisplayName,
    string Description,
    DataType Type,
    bool Required,
    object? DefaultValue = null,
    IReadOnlyCollection<string>? AllowedValues = null);

public enum DataType
{
    String, Number, Boolean, Json, Binary, DocumentRef, Enum
}
```

`DocumentRef` is a typed reference (`{ documentId, mimeType, sizeBytes }`) so nodes don't pass raw bytes around the engine.

---

## 3. Authoring a Node

### Step-by-step

1. **Pick a type id**. `category.kebab-name`, e.g. `document.extract-pdf-text`.
2. **Write the descriptor.** Declare inputs, outputs, configuration.
3. **Implement `IWorkflowNode`.** Pure logic; reach into `context.Services` for dependencies.
4. **Register.** Add the node to the registry module for its category.
5. **Test.** Unit tests with a fake `WorkflowExecutionContext`.
6. **Document.** Add a brief doc page or extend `docs/NODES.md`.

### Example: a simple AI summarize node

```csharp
public sealed class AiSummarizeNodeDescriptor : IWorkflowNodeDescriptor
{
    public string Type => "ai.summarize";
    public string DisplayName => "AI Summarize";
    public string Description => "Summarizes input text using an LLM.";
    public string Category => "ai";
    public string Version => "0.1.0";
    public string? IconKey => "sparkle";

    public IReadOnlyCollection<NodeInputDefinition> Inputs { get; } = new[]
    {
        new NodeInputDefinition("text", "Text", "Text to summarize.", DataType.String, Required: true)
    };

    public IReadOnlyCollection<NodeOutputDefinition> Outputs { get; } = new[]
    {
        new NodeOutputDefinition("summary", "Summary", "The generated summary.", DataType.String),
        new NodeOutputDefinition("tokensUsed", "Tokens used", "LLM token usage.", DataType.Number)
    };

    public IReadOnlyCollection<NodeConfigDefinition> Configuration { get; } = new[]
    {
        new NodeConfigDefinition("model", "Model", "LLM model identifier.", DataType.String, Required: false, DefaultValue: "default"),
        new NodeConfigDefinition("maxTokens", "Max tokens", "Maximum output tokens.", DataType.Number, Required: false, DefaultValue: 512)
    };
}

public sealed class AiSummarizeNode(ILLMProvider llm, IAIUsageRecorder usage) : IWorkflowNode
{
    public string Type => "ai.summarize";

    public async Task<NodeExecutionResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken)
    {
        var text = context.GetInput<string>("text");

        var req = new LLMRequest
        {
            Prompt = $"Summarize concisely:\n\n{text}",
            Model  = context.GetConfig<string>("model") ?? "default",
            MaxTokens = context.GetConfig<int?>("maxTokens") ?? 512,
        };

        var resp = await llm.GenerateTextAsync(req, cancellationToken);

        await usage.RecordAsync(context, resp.Usage, cancellationToken);

        return new NodeExecutionResult
        {
            Success = true,
            Status  = NodeExecutionStatus.Succeeded,
            Outputs = new()
            {
                ["summary"]    = resp.Text,
                ["tokensUsed"] = resp.Usage.TotalTokens,
            },
        };
    }
}
```

---

## 4. Registry

Each category exports a registration extension:

```csharp
public static IServiceCollection AddAiNodes(this IServiceCollection services)
{
    services.AddSingleton<IWorkflowNodeDescriptor, AiSummarizeNodeDescriptor>();
    services.AddTransient<IWorkflowNode, AiSummarizeNode>();
    // …
    return services;
}
```

The Api exposes the union of all descriptors at `GET /api/nodes/catalog`. The frontend designer renders configuration forms dynamically from this metadata.

---

## 5. Configuration Schema for the UI

A node's `Configuration` collection drives the configuration drawer in the designer. Recommended UI mapping:

| `DataType`     | UI Control                  |
| -------------- | --------------------------- |
| `String`       | text field / textarea       |
| `Number`       | number input                |
| `Boolean`      | toggle                      |
| `Json`         | code editor (Monaco)        |
| `Binary`       | file picker                 |
| `DocumentRef`  | document picker             |
| `Enum`         | dropdown (`AllowedValues`)  |

Default values are pre-filled; required fields block saving.

---

## 6. Error Handling

Nodes must **not** swallow errors. They should:

- Throw `NodeExecutionException` for explicit, descriptive failures.
- Throw the original exception for unexpected failures (engine wraps it).
- Mark errors as retryable using `NodeExecutionException.Retryable = true` for transient cases.

```csharp
throw new NodeExecutionException("Failed to call OpenAI", inner)
{
    Retryable = true,
    Code = "llm.transient"
};
```

---

## 7. Testing

```csharp
[Fact]
public async Task Summarizes_text()
{
    var llm = Substitute.For<ILLMProvider>();
    llm.GenerateTextAsync(Arg.Any<LLMRequest>(), default)
       .Returns(new LLMResponse("Short summary.", new LLMUsage(50, 20, 70)));

    var node = new AiSummarizeNode(llm, new NullAIUsageRecorder());
    var ctx = TestContext.WithInputs(new() { ["text"] = "Long text…" })
                         .WithConfig(new() { ["model"] = "gpt-4o-mini" });

    var result = await node.ExecuteAsync(ctx, default);

    result.Success.Should().BeTrue();
    result.Outputs["summary"].Should().Be("Short summary.");
}
```

The SDK ships a `TestContext` helper so node tests don't reproduce engine plumbing.

---

## 8. Versioning

- Node descriptors carry a semver `Version`.
- Breaking changes (changed input/output keys/types) require a new `Type` (e.g. `ai.summarize` → `ai.summarize@2` or `ai.summarize.v2`).
- Workflows pin to node types; bumping a node's minor/patch is transparent to existing workflows.

---

## 9. Do / Don't

**Do**

- Keep node code small and focused.
- Use DI for providers (LLM, HTTP, storage).
- Log structured fields with the execution and node id.
- Prefer structured outputs.
- Respect cancellation tokens.

**Don't**

- Persist anything to the workflow execution tables yourself.
- Make irreversible external actions without human approval upstream.
- Store secrets in workflow JSON (read from configuration providers instead).
- Block on synchronous I/O.
- Share mutable state across executions.

---

## 10. Skill: Create a New Node (for AI Coding Agents)

When asked to create a new node:

1. Pick the `Type` id (`category.kebab-name`).
2. Implement the descriptor.
3. Define inputs, outputs, configuration with proper `DataType` and `Required`.
4. Implement `IWorkflowNode.ExecuteAsync`.
5. Wire DI registration into the category's `Add{Category}Nodes` extension.
6. Add unit tests (happy path + at least one failure path).
7. Update [`NODES.md`](./NODES.md) with the new entry.
8. (Optional) Add a sample workflow that uses the node under `samples/`.
