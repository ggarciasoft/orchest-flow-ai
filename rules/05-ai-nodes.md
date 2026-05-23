# Rules: AI Nodes

1. **Log provider and model on every call.** Every LLM call records provider, model, and prompt version.

2. **Track token usage.** Every LLM call records prompt tokens, completion tokens, total tokens, and estimated cost via `IAIUsageRecorder`. The AI Runtime handles this transparently — node authors must not bypass it.

3. **Version all prompts.** Prompt templates live in `services/OrchestAI.AI/Prompts/` and carry a version string (`YYYY.MM.N`). Changes to a prompt bump the version. The version is stored in `ai_usage_logs.prompt_version`.

4. **Structured output is mandatory for business decisions.** Any AI output that drives a workflow decision (condition, routing, risk level, classification) must use `GenerateStructuredAsync<T>` with a JSON schema. Schema validation is required.

5. **Structured output self-correction.** On schema validation failure, the runtime retries once with a self-correction message. On second failure, return a typed error — do not pass invalid data downstream.

6. **Expose confidence and risk when relevant.** AI nodes that assess risk or classify should include a confidence or risk level in outputs so the engine can branch on them.

7. **AI must not perform irreversible actions.** AI nodes must not send emails, post webhooks, or modify external systems. These actions must be in integration nodes, downstream of a human approval if appropriate.

8. **User-supplied content is wrapped in delimited tags.** To mitigate prompt injection, all user-provided content (document text, form inputs) is wrapped in `<contract>`, `<input>`, or similar tags in prompts.

9. **Never call provider SDKs directly in node code.** All LLM calls go through `ILLMProvider`.

10. **Never log full prompts or responses above configured size limits.** Truncate to configurable max chars. Full payloads may be stored to object storage if configured, but never in plain application logs.

11. **PII redaction.** If tenant has PII redaction enabled, run the redaction middleware before sending text to providers.

12. **Respect per-tenant and per-execution token budgets.** Check limits before dispatching. Return a clear error if budget is exceeded.
