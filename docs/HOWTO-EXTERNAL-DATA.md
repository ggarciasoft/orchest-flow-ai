# How To: External Data Intake

The **External Data Intake** pattern lets a workflow pause and wait for an external system to POST data before continuing. No form is shown to a user — everything is API-driven.

---

## Core Concept

A `system.data-checkpoint` node:
1. Creates a one-time **resume URL** when the workflow reaches it
2. Pauses the execution
3. Waits for an external system to POST JSON to that URL
4. Resumes with the posted data available as node outputs for downstream nodes

---

## Quick Start: External Data Intake Playground

The fastest way to try this is the built-in playground:

1. Go to **Playground → External Data** in the sidebar
2. *(Optional)* Configure your database nodes — enter a connection string and SQL statement for each checkpoint, or click **Skip DB setup**
3. Click **Start Playground**
4. The workflow seeds and starts automatically
5. When it pauses at the **Customer** checkpoint, the playground shows the resume URL
6. Fill in the JSON payload and click **Send** — or copy the curl command and run it from a terminal
7. The workflow resumes, processes the data, and pauses at the **Order** checkpoint
8. Repeat for the second checkpoint
9. The workflow completes

---

## Building Your Own Data Intake Workflow

### Step 1 — Add a data-checkpoint node

In the workflow designer:

1. From the Node Palette → **System** → drag **Data Checkpoint** onto the canvas
2. Connect it to the previous node
3. Click the node to open Config:
   - **Name** — label shown in the execution timeline (e.g. "Customer Data")
   - **Description** — documents what the external system should POST
   - **Fields** — JSON array defining expected fields with validation (see below)

### Step 2 — Define expected fields (optional but recommended)

In the **Fields** config, enter a JSON array:

```json
[
  { "key": "name",   "type": "string",  "required": true  },
  { "key": "email",  "type": "string",  "required": true  },
  { "key": "amount", "type": "number",  "required": false }
]
```

**Supported types:**

| Type | Behaviour |
|------|-----------|
| `string` | Validates presence only |
| `number` | Must parse as decimal; coerced to `double` in outputs |
| `boolean` | Must be `true`, `false`, `1`, or `0`; coerced to `bool` in outputs |
| `any` | No type check; value passed through as-is |

### Step 3 — Use checkpoint outputs in downstream nodes

After the checkpoint resumes, all posted fields are available as `{{fieldName}}` in downstream node configs:

```sql
INSERT INTO customers (name, email) VALUES (@name, @email)
```

```
Send email to {{email}} with subject "Welcome {{name}}"
```

---

## Sending Data to a Checkpoint

When the workflow is paused at a checkpoint, the resume URL is available in:
- The **execution timeline** (click the paused node)
- The **External Data playground** (shown automatically)
- The **Approval inbox** (shows URL + curl snippet instead of Approve/Reject)

### Using curl

```bash
curl -X POST "http://localhost:5080/api/webhooks/resume/YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{"name": "Jane Smith", "email": "jane@example.com"}'
```

### Using any HTTP client

- Method: `POST`
- URL: the full resume URL from the timeline
- Body: JSON object with your data fields
- Content-Type: `application/json`
- No authentication required

---

## Validation Errors

If the `fields` config is set and validation fails, the endpoint returns **400** without consuming the token:

```json
{
  "error": "Validation failed. Token not consumed — fix the payload and retry.",
  "errors": [
    "Missing required field: 'name'",
    "Field 'amount' must be a number (got 'abc')"
  ]
}
```

**Fix the payload and POST to the same URL again** — the token is still valid.

---

## Token Behaviour

| Scenario | Response |
|----------|----------|
| Valid payload, first POST | `200 { "status": "resumed" }` — workflow advances |
| Invalid payload (validation fails) | `400` with errors — token NOT consumed, retry allowed |
| Token already used | `410 Gone` — start a new execution |
| Token not found | `404 Not Found` |

---

## Multiple Checkpoints in Sequence

You can chain multiple checkpoints:

```
Start → Checkpoint 1 → Process → Checkpoint 2 → Process → End
```

Each checkpoint has its own unique token. The workflow advances one step at a time — Checkpoint 2's token is only generated after Checkpoint 1 is successfully resumed.

---

## Combining with Database Nodes

A common pattern: checkpoint + `data.db-execute` to save each batch of incoming data:

```
Checkpoint (Customer) → DB Execute (INSERT customers) → Checkpoint (Order) → DB Execute (INSERT orders) → End
```

The `data.db-execute` node reads its SQL parameters from upstream node outputs — no manual wiring needed.

```sql
INSERT INTO customers (name, email) VALUES (@name, @email)
```

`@name` and `@email` are automatically bound from the checkpoint's outputs.
