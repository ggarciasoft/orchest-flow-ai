# Rules: Backend (.NET / C#)

1. Domain entities are free from infrastructure concerns (no EF annotations on domain classes unless justified by ADR).

2. Application layer handles use cases (CQRS-style commands/queries). It does not contain infrastructure concerns.

3. Infrastructure implements abstractions defined in Application/Domain. It never leaks into Domain.

4. API controllers are thin — validate input, dispatch command/query, return response. No business logic.

5. Worker is a thin host — it picks messages from the queue and calls Application services. No duplicated business logic.

6. All async methods accept and respect `CancellationToken`.

7. All execution state changes must be persisted before the next step begins.

8. Failed node executions must store `error_message` and `error_code`.

9. Use idempotency for retried operations. Queue messages carry an idempotency key.

10. Multi-tenant filtering happens at the repository layer by default. Never rely on call-site `WHERE tenant_id = ?` discipline.

11. Validate inputs:
    - API request bodies → FluentValidation in command validators.
    - Workflow definitions → engine validation (graph rules, descriptor checks).

12. Use Serilog with structured fields. Every log entry for execution work includes `correlationId`, `executionId`, `nodeExecutionId`, `tenantId`.

13. Public C# APIs have XML documentation comments.

14. Avoid `static` mutable state. Use DI-scoped or transient services.

15. Prefer explicit error types (`NodeExecutionException` with `Code` and `Retryable`) over bare exceptions in node code.


---

## Unit Test Requirement

16. **Every .NET code change requires unit tests.** Before committing any backend change:
    - Run `dotnet test` and confirm **0 failures**
    - Add `<ClassName>Tests.cs` in `tests/OrchestAI.Tests/` for every new class
    - Update existing tests impacted by the change
    - Use xUnit + Moq + FluentAssertions. No bare `Assert`.
