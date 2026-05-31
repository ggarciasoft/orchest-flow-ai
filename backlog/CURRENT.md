# OrchestFlowAI — Current Backlog

> Things that are partially done, known issues, or next up.

---

## User Invitations, Role Management & Transactional Email

### What was built (Phase 1 & 2 — complete)

**Email delivery**
- ✅ `IEmailService` — interface with `SendAsync`
- ✅ `SmtpEmailService` — System.Net.Mail SMTP; config via `Email:Smtp:*` / `Email__Smtp__Host` env var
- ✅ `LogEmailService` — dev fallback (logs email content when SMTP not configured)
- ✅ Invite email template (HTML + text, accept URL, expiry)
- ✅ Welcome/registration email template
- ✅ Email sent after invite creation (async, non-blocking)
- ✅ Welcome email sent after registration (fire-and-forget)
- ✅ DI auto-wiring: SMTP when `Email:Smtp:Host` is set, log fallback otherwise
- ✅ `appsettings.json` — `App:WebBaseUrl` + `Email:*` section added

**API improvements**
- ✅ `GET /api/tenants/{id}/invite/preview?token=` (anonymous) — returns email, tenant name, role, expiry
- ✅ `POST /api/tenants/{id}/invite/accept` — now returns JWT for auto-login
- ✅ Email normalized to lowercase on invite
- ✅ Duplicate invite/member check before creating invite
- ✅ `TenantInviteResponse` — token removed from response body
- ✅ `GET /api/tenants/{id}/members` — list users (AdminOnly)
- ✅ `PUT /api/tenants/{id}/members/{userId}/role` — change role (AdminOnly)
- ✅ `DELETE /api/tenants/{id}/members/{userId}` — remove member (AdminOnly)
- ✅ `GET /api/tenants/{id}/invites` — list pending invites (AdminOnly)
- ✅ `DELETE /api/tenants/{id}/invites/{inviteId}` — revoke invite (AdminOnly)

**Frontend**
- ✅ `/settings/team` — member list with role editing, pending invites with revoke, invite form
- ✅ Accept invite page — shows workspace name + invited email + role from preview API
- ✅ Accept invite — auto-logs in (JWT stored) and redirects to `/workflows`
- ✅ Onboarding Step 2 — shows "Invite sent ✓" confirmation instead of copy-paste link
- ✅ Team card added to settings hub page
- ✅ Team entry added to sidebar navigation (admin only)
- ✅ `api.ts` — new endpoints: `invitePreview`, `listMembers`, `updateMemberRole`, `removeMember`, `listInvites`, `revokeInvite`

---

_Nothing active right now._

---

## Next / Phase 3

See [`backlog/FUTURE.md`](./FUTURE.md) for the polish items:
- BCrypt/Argon2 password migration
- Rate limiting on auth/invite endpoints
- Rich HTML email templates with branding
- SendGrid / Mailgun alternative provider
- Resend invite endpoint
- Audit log for user management events
