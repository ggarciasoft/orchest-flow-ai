# AI Runtime

The AI Runtime is the layer that lets nodes call LLMs **without** binding to a single provider. It also enforces structured outputs, tracks usage/cost, and versions prompts.

---

## 1. Goals

- Provider-agnostic LLM calls (OpenAI, Azure OpenAI, Anthropic, others).
- **Structured output first.** Free-form text only when business decisions don't depend on it.
- Token + cost tracking on every call.
- Prompt templates with explicit versions.
- Safe handling of sensitive data.
- Graceful degradation on rate limits / outages.

---

## 2. Provider Abstraction

```csharp
public interface ILLMProvider
{
    string Id { get; }                 // "openai", "azure-openai", "anthropic", "local"
    IReadOnlyCollection<string> Models { get; }

    Task<LLMResponse> GenerateTextAsync(
        LLMRequest request,
        CancellationToken cancellationToken);

    Task<LLMResponse<TOutput>> GenerateStructuredAsync<TOutput>(
        LLMRequest request,
        JsonSchema outputSchema,
        CancellationToken cancellationToken);
}

public sealed record LLMRequest
{
    public string Prompt { get; init; } = default!;
    public string? SystemPrompt { get; init; }
    public string Model { get; init; } = "default";
    public double? Temperature { get; init; }
    public int? MaxTokens { get; init; }
    public IReadOnlyCollection<LLMMessage>? History { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public sealed record LLMResponse(string Text, LLMUsage Usage);
public sealed record LLMResponse<T>(T Output, string RawText, LLMUsage Usage);

public sealed record LLMUsage(int PromptTokens, int CompletionTokens, int TotalTokens, decimal? EstimatedCostUsd = null);
```

A `LLMProviderRouter` chooses the provider/model based on:

1. Explicit node config (`config.model`)
2. Tenant-level defaults
3. Platform-level defaults
4. Fallback chain on failure

---

## 3. Structured Output

For any AI node that drives a business decision, use `GenerateStructuredAsync<T>` with a JSON Schema.

### Pattern

```csharp
public sealed record ContractRisk(
    string RiskLevel,
    string Summary,
    IReadOnlyList<ContractKeyClause> KeyClauses,
    string RecommendedAction);

public sealed record ContractKeyClause(string Title, string Risk, string Reason);
```

The runtime:

1. Renders the prompt template.
2. Calls the provider with the schema.
3. Validates the response against the schema.
4. On schema-validation failure, retries **once** with a self-correction message ("the previous response did not match the schema; here is the schema; return only valid JSON").
5. On second failure, returns a typed error.

### Example: contract risk

Schema:

```json
{
  "type": "object",
  "required": ["riskLevel", "summary", "keyClauses", "recommendedAction"],
  "properties": {
    "riskLevel": { "type": "string", "enum": ["Low", "Medium", "High"] },
    "summary": { "type": "string", "minLength": 20 },
    "keyClauses": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["title", "risk", "reason"],
        "properties": {
          "title": { "type": "string" },
          "risk":  { "type": "string", "enum": ["Low", "Medium", "High"] },
          "reason": { "type": "string" }
        }
      }
    },
    "recommendedAction": { "type": "string" }
  }
}
```

---

## 4. Prompt Templates

Prompts live in code (initially) under `services/OrchestFlowAI.AI/Prompts/` as versioned templates.

```csharp
public static class ContractRiskPrompt
{
    public const string Version = "2026.05.0";

    public const string System = """
        You are a senior commercial lawyer reviewing contracts.
        Produce a strict JSON response matching the provided schema.
        Be conservative: prefer Medium over Low when uncertain.
    """;

    public static string User(string contractText) =>
        $"""
         Review the following contract and return a structured risk assessment.

         <contract>
         {contractText}
         </contract>
         """;
}
```

Rules:

- Every prompt has a `Version`.
- AI usage logs record both `model` and `promptVersion`.
- Changes to a prompt bump the version (semver-ish: `YYYY.MM.N`).
- Tests pin to a prompt version.

---

## 5. Usage Tracking

Every LLM call records:

```
ai_usage_logs (
  id,
  tenant_id,
  workflow_execution_id,
  node_execution_id,
  provider,
  model,
  prompt_version,
  prompt_tokens,
  completion_tokens,
  total_tokens,
  estimated_cost,
  created_at
)
```

The `IAIUsageRecorder` is injected into nodes; the AI Runtime calls it transparently inside `GenerateStructuredAsync` so node authors don't have to remember.

---

## 6. Safety & Governance

- **AI is never the legal authority.** High-risk decisions must route through human approval.
- **PII / sensitive data:** the runtime supports a `redact` middleware that scrubs known patterns (SSNs, card numbers) before sending text to providers. Configurable per tenant.
- **Provider isolation:** API keys are stored in environment / vault; never in workflow JSON.
- **Audit:** prompt + response are stored at the node-execution level (truncated to a configurable max size; full payloads optionally to object storage).
- **Confidence surfaces:** AI nodes expose risk/confidence where relevant and the engine can branch on them.

---

## 7. Rate Limits and Resilience

- Per-provider client uses a token bucket + concurrency limit.
- 429s with `Retry-After` are honored; the engine treats them as retryable.
- A circuit breaker opens on sustained provider failures and routes the next call to a configured fallback model (e.g. Azure OpenAI when OpenAI is down).
- Long calls respect cancellation; the worker may cancel after a per-node timeout.

---

## 8. Tool Calling (Future)

`ai.agent-executor` (Phase 13) will support tool calling:

- Tools are workflow nodes exposed by id to the agent.
- The agent loop is bounded (max steps, max tokens, max wall-clock).
- Every tool call is persisted as a child node execution.
- Tool calls inherit tenant + execution context.

---

## 9. RAG (Future)

`ai.rag-search` (Phase 12) will return cited passages from a knowledge base:

- Embedding model is part of provider abstraction.
- Vector store is pluggable (pgvector first, others later).
- Citations include document id + page/chunk references.
- Retrieval-augmented prompts log retrieved chunks alongside usage.

---

## 10. Testing AI Nodes

- Use a `FakeLLMProvider` that returns canned `LLMResponse<T>` instances.
- Snapshot tests verify prompt template rendering.
- Contract tests verify structured outputs match the schema.
- Cost regression tests assert token usage stays within expected bands for fixture inputs.

---

## 11. Configuration

Provider configuration is read from environment / config:

```
OrchestFlowAI_LLM__DEFAULT_PROVIDER=openai
OrchestFlowAI_LLM__DEFAULT_MODEL=gpt-4o-mini
OrchestFlowAI_LLM__OPENAI__API_KEY=...
OrchestFlowAI_LLM__AZURE__ENDPOINT=https://...
OrchestFlowAI_LLM__AZURE__API_KEY=...
OrchestFlowAI_LLM__ANTHROPIC__API_KEY=...
OrchestFlowAI_LLM__FALLBACKS=azure-openai,anthropic
```

Per-tenant overrides live in DB (`tenant_settings`) and take precedence over platform defaults.
