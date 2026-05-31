# How To: Manage Your Team

This guide covers inviting new team members, assigning roles, managing existing members, and configuring email for invitation delivery.

---

## Prerequisites

- You must be an **Admin** of your workspace.
- If sending real invitation emails, [configure SMTP](#configure-email).

---

## 1. Invite a Team Member

1. Go to **Settings → Team** (visible to Admins only).
2. In the **Invite a new member** form at the top:
   - Enter the invitee's email address.
   - Choose a **Role**:
     | Role | What they can do |
     |------|-----------------|
     | **Admin** | Everything — including inviting others and changing settings |
     | **Editor** | Create, edit, run workflows and forms |
     | **Approver** | Approve / reject human approval tasks |
     | **Viewer** | Read-only access to workflows, executions, and approvals |
3. Click **Send invite**.
4. The invitee receives an email with a link to `/invite/{tenantId}?token=…`. The link expires in **24 hours**.

> **No SMTP configured?** In development, the invite link is printed to the server console. Share it manually with the invitee.

---

## 2. What the Invitee Sees

1. The invitee opens the link in their browser.
2. A card shows the **workspace name**, their **email**, and the **role** they are being assigned.
3. They set a password (minimum 8 characters) and click **Create account & join**.
4. They are automatically logged in and redirected to the Workflows dashboard.

---

## 3. View Pending Invites

On the **Settings → Team** page, the **Pending invites** section lists all invites that have not yet been accepted, ordered newest-first.

Each row shows:
- The invitee's email
- Their assigned role badge
- The expiry date
- A **✕ Revoke** button

---

## 4. Revoke an Invite

If you sent an invite to the wrong address (or it expired and you want to re-send), click the **✕** button next to the invite. This deletes the invite record. You can then create a new invite for the correct address.

---

## 5. Manage Existing Members

The **Members** section on the Team page lists every user in the workspace.

### Change a Member's Role

1. Click the **role badge** next to a member's name.
2. A dropdown appears — select the new role.
3. Click **Save**.

> You cannot change your own role.

### Remove a Member

Click the trash icon next to a member. This removes them from the workspace immediately. They will no longer be able to log in to this workspace.

> You cannot remove yourself.

---

## 6. Configure Email

### Using SMTP

Add the following to `appsettings.json` (or use environment variables):

```json
"App": {
  "WebBaseUrl": "https://your-domain.com"
},
"Email": {
  "FromAddress": "noreply@your-domain.com",
  "FromName": "YourApp",
  "Smtp": {
    "Host": "smtp.mailprovider.com",
    "Port": 587,
    "Username": "your-smtp-user",
    "Password": "your-smtp-password",
    "UseSsl": true
  }
}
```

Or via environment variables:

```
App__WebBaseUrl=https://your-domain.com
Email__FromAddress=noreply@your-domain.com
Email__FromName=YourApp
Email__Smtp__Host=smtp.mailprovider.com
Email__Smtp__Port=587
Email__Smtp__Username=your-smtp-user
Email__Smtp__Password=your-smtp-password
Email__Smtp__UseSsl=true
```

Restart the API server after changing email config.

### Development (no SMTP)

When `Email:Smtp:Host` is empty (the default), emails are **not sent** — they are logged to the console instead:

```
[DEV EMAIL] To=alice@example.com | Subject=You've been invited...
Hi, you've been invited as Editor. Accept here: http://localhost:3000/invite/...
```

This lets you develop and test the invite flow without an email provider.

---

## 7. Role Summary

| Action | Viewer | Approver | Editor | Admin |
|--------|:------:|:--------:|:------:|:-----:|
| View workflows & executions | ✓ | ✓ | ✓ | ✓ |
| Approve / reject tasks | — | ✓ | ✓ | ✓ |
| Create / edit workflows | — | — | ✓ | ✓ |
| Run workflows | — | — | ✓ | ✓ |
| Invite / manage team | — | — | — | ✓ |
| Settings (providers, secrets, tenant) | — | — | — | ✓ |

---

## 8. Troubleshooting

**"An active invite for that email already exists"**
The invitee already has a pending (non-expired) invite. Revoke it on the Team page, then re-invite.

**"A user with that email already belongs to this tenant"**
The email is already registered in this workspace. Use "Change role" instead of inviting them again.

**Invite link expired**
Invite links are valid for 24 hours. Revoke the old invite and send a new one.

**Email not received**
1. Check the server logs — in dev mode, the link is logged there.
2. In production, verify SMTP credentials and that the `FromAddress` domain is allowed by your SMTP provider.
3. Check the invitee's spam/junk folder.
