# Agent: AI Runtime

## Purpose
Own the LLM provider abstraction, structured output handling, prompt versioning, and AI usage tracking.

## Reads
- [`docs/AI-RUNTIME.md`](../docs/AI-RUNTIME.md)
- [`docs/SECURITY.md`](../docs/SECURITY.md)
- [`rules/05-ai-nodes.md`](../rules/05-ai-nodes.md)
- [`rules/07-security.md`](../rules/07-security.md)

## Write Scope
- `services/OrchestFlowAI.AI/`
- `services/OrchestFlowAI.AI/Prompts/` (versioned prompt templates)
- AI provider implementations under `packages/OrchestFlowAI.Infrastructure/AI/`
- AI-specific node provider code (nodes call the runtime; the runtime calls providers)
- AI usage recording logic

## Responsibilities
- Implement `ILLMProvider` for:
  - OpenAI
  - Azure OpenAI
  - Anthropic
  - (Post-MVP) local/self-hosted
- Implement `LLMProviderRouter` (selects provider/model by node config → tenant default → platform default → fallback).
- `GenerateTextAsync` and `GenerateStructuredAsync<T>` with schema validation.
- Structured output self-correction on schema mismatch (one retry with correction message).
- Versioned prompt templates (`services/OrchestFlowAI.AI/Prompts/`).
- `IAIUsageRecorder` — tracks provider, model, prompt version, tokens, cost.
- Rate limiting + circuit breaker per provider.
- PII redaction middleware (configurable per tenant).

## Guardrails
- **Never log API keys or raw secrets.**
- Truncate prompt/response logs to configured max size.
- Always use `ILLMProvider` — no direct SDK calls in node code.
- Structured output (`GenerateStructuredAsync<T>`) is **mandatory** for any AI output that drives a business decision (condition, approval, routing).
- Provider API keys come from environment/vault only — never from workflow JSON.
- User-supplied content must be wrapped in delimited tags in prompts to mitigate prompt injection.

## Prompt Template Convention

```csharp
// services/OrchestFlowAI.AI/Prompts/ContractRiskPrompt.cs
public static class ContractRiskPrompt
{
    public const string Version = "2026.05.0";  // YYYY.MM.N
    public const string System = "…";
    public static string User(string contractText) => $"…{contractText}…";
}
```

Every prompt has a `Version`. Prompt version is stored in `ai_usage_logs.prompt_version`.

## Testing

- Use `FakeLLMProvider` with canned responses.
- Snapshot tests for prompt rendering.
- Contract tests verifying structured outputs match schema.
- Cost regression tests for fixture inputs.
