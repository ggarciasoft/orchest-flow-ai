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

---

## Unit Test Maintenance — Mandatory on Every Code Change

14. **Every backend (.NET) code change must:**
    - Run `dotnet test` before and after the change.
    - Update existing tests broken by the change — do not delete tests to make them pass.
    - Add new unit tests for any new class, method, or behavior introduced.
    - Ensure `dotnet test` passes with **0 failures** before committing.

15. **Every frontend (Next.js/React/TypeScript) code change must:**
    - Run `npm test` before and after the change.
    - Update existing tests broken by the change — do not delete tests to make them pass.
    - Add new unit tests for any new component, page, hook, or lib function introduced.
    - Ensure `npm test` passes with **0 failures** before committing.

16. **New files require tests:**
    | New file type | Required test file |
    |---|---|
    | `.cs` class or service | `tests/OrchestAI.Tests/<Namespace>/<ClassName>Tests.cs` |
    | `.tsx` component or page | `src/**/__tests__/<ComponentName>.test.tsx` |
    | `.ts` lib/utility | `src/lib/__tests__/<filename>.test.ts` |
    | `.cs` node (`IWorkflowNode`) | `tests/OrchestAI.Tests/NodeTests/<NodeName>Tests.cs` |

17. **Test coverage gates (per PR):**
    - New classes/components must have ≥ 1 happy-path test and ≥ 1 edge-case or error-path test.
    - Refactors must not reduce total passing test count.
    - PRs that delete tests without a documented reason in the PR description are rejected.

18. **Agent/AI code generation rule:** Any agent or AI tool generating production code must also generate the corresponding unit tests in the same commit or PR. Code without tests is considered incomplete.

19. **Test-first for bug fixes:** When fixing a bug, first write a failing test that reproduces it, then fix the code until the test passes. Commit both together.

20. **Do not comment out or skip tests** (`[Fact(Skip=...)]`, `it.skip(...)`, `xit(...)`) without a linked issue. Skipped tests must be resolved within the same sprint.
