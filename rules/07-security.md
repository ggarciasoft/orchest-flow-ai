# Rules: Security

1. Authentication is required for all non-public endpoints. There are no unauthenticated data endpoints.

2. Tenant resolution happens **once per request** from the auth token (JWT). The resolved `TenantContext` is injected into all downstream services. Never accept `tenant_id` as a user-supplied request parameter.

3. Role-based authorization is enforced in the **Application layer** (use case policies), not only in controllers. Controllers are not the security boundary.

4. HTTPS-only in non-local environments. HTTP is acceptable only for local Docker Compose dev.

5. Rate limiting per tenant and per IP. Default: 100 req/min per tenant. Configurable.

6. Redact secrets and sensitive patterns in logs. The logging pipeline runs a redaction middleware that scrubs `api_key`, `authorization`, `secret`, `token`, `password`, and similar patterns.

7. **Secrets never live in workflow JSON, log entries, or source code.** Provider keys, DB passwords, and JWT signing keys come from environment variables or a vault provider.

8. Document storage paths are tenant-prefixed: `tenants/{tenant_id}/uploads/{document_id}.ext`. Every read/download endpoint verifies tenant + ownership before streaming.

9. AI prompt injection mitigations:
   - System prompts include explicit instructions to ignore content inside user delimiters.
   - User content is always wrapped in named delimiters (`<contract>`, `<input>`, etc.).
   - Structured output schemas constrain what AI outputs can contain.
   - No AI output directly triggers irreversible external actions without human approval.

10. In production: HSTS, secure HTTP-only cookies, `SameSite=Lax` (or `Strict` for admin paths), strict CSP.

11. All approval decisions are authenticated. An approval carries `responded_by` (user id) and is recorded in the audit log.

12. Security-relevant events are written to `audit_logs` immediately and synchronously (not async/eventual). Events include: login, logout, workflow CRUD, version activation, execution trigger, approval decision, settings changes, user management.

13. File upload validation: check MIME type against allowlist, enforce max size, store by generated UUID (never by user-supplied filename).

14. Security issues are reported privately — never via public GitHub issues. See `docs/SECURITY.md` §11.
