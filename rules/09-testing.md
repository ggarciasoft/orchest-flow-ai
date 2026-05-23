# Rules: Testing

1. Domain logic (entity invariants, value objects, domain rules) has unit tests.

2. The workflow engine has integration tests using fake nodes, fake DI, and an in-memory or test-DB persistence layer.

3. Critical user journeys have E2E tests (Playwright):
   - Contract review: upload → execute → approve → timeline complete.
   - Low-risk contract: straight-through without approval.
   - Node failure: error visible in timeline.

4. Every node ships with at least:
   - One happy-path test.
   - One failure-path test (retryable or terminal error).

5. AI nodes use `FakeLLMProvider`. No real LLM API calls in unit or integration tests.

6. Prompt snapshot tests: assert prompt template renders exactly as expected for fixture inputs.

7. Structured output contract tests: assert the AI runtime's schema validation accepts valid payloads and rejects invalid ones.

8. Auth and tenant isolation tests: assert cross-tenant access is blocked at the API level.

9. Tests must be **deterministic**. No sleeps or timing-dependent assertions. Use explicit waits (Playwright `waitFor`, `TestServer` sync) where needed.

10. Do not modify production code paths just to make tests compile. Fix the test's setup or fix the actual code.

11. Naming convention:
    ```
    [Method/Scenario]_[State/Condition]_[ExpectedBehavior]
    // Example:
    ExecuteAsync_WhenRiskIsHigh_PausesForApproval()
    ```

12. Tests run in CI on every PR. A failing test blocks merge. A flaky test must be fixed or quarantined with a tracked issue — it must not be silently ignored.

13. Cost regression tests for AI nodes: assert token usage for fixture inputs stays within expected bounds (±10%). Alerts on unexpected token growth.
