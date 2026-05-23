# Rules: Frontend (Next.js / React / TypeScript)

1. **Render node forms dynamically from `/api/nodes/catalog`.** Do not hardcode configuration forms for specific node types in the UI.

2. Keep React Flow canvas state separate from the persisted workflow definition. Translate to canonical JSON only at save time.

3. Use the generated typed API client (from OpenAPI spec). Do not hand-write `fetch` calls for endpoints that exist in the spec.

4. Show validation errors **before** saving. Never allow the user to submit a workflow with known errors.

5. Execution and timeline views are **read-only**. Do not add edit affordances to these screens.

6. Approval actions (approve/reject) require an explicit confirmation step (modal with required comment on reject).

7. No `any` in TypeScript. Use types from the generated client or local Zod schemas. ESLint `@typescript-eslint/no-explicit-any` must pass.

8. Every screen handles all three states explicitly:
   - **Loading** — use skeletons, not spinners.
   - **Empty** — use an illustrated empty state with a primary action.
   - **Error** — toast notification + retry button; sanitize error messages shown to users.

9. Use Tailwind CSS + shadcn/ui primitives. Avoid custom CSS where a primitive exists.

10. No secrets in the browser bundle. `.env.local` values prefixed with `NEXT_PUBLIC_` are visible to the browser — only put non-sensitive config there.

11. Accessibility:
    - All interactive elements reachable by keyboard.
    - Form inputs have associated labels.
    - Focus managed when drawers/modals open.
    - Color contrast meets WCAG AA.

12. Avoid layout shift. Reserve space for content with skeletons before data arrives.

13. Wrap multiple external links in `<>` for Discord/chat surfaces to suppress embeds. (Applies to generated output, not UI code.)


---

## Unit Test Requirement

14. **Every frontend code change requires unit tests.** Before committing any frontend change:
    - Run `npm test` and confirm **0 failures**
    - Add `<ComponentName>.test.tsx` or `<filename>.test.ts` alongside new files
    - Update existing tests impacted by the change
    - Use Jest + React Testing Library. Mock external dependencies with `jest.mock`.
