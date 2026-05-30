# How To: Workflow Configuration

The **Workflow Configuration** store is a persistent key-value store scoped to your tenant. Workflows can read and write values that survive between runs — useful for tracking state like "last processed date", counters, or any value that should carry over from one execution to the next.

---

## Managing Configuration Entries

Go to **Settings → Configuration** to manage your config store:

- **Add Entry** — key, value, type (string/number/boolean/json/datetime), optional description
- **Edit** — click Edit on any row to update the value or description inline
- **Delete** — click Delete, confirm, and the entry is removed
- Values are displayed truncated; hover for the full value (JSON is pretty-printed)

---

## Using Config in Workflows

Two system nodes are available in the **System** category of the node palette:

### `system.read-config` — Read Config

Reads a value from the store and makes it available to downstream nodes.

**Config:**
| Field | Required | Description |
|-------|----------|-------------|
| `key` | Yes | The config key to read, e.g. `gmail.last_sync_date` |
| `defaultValue` | No | Value to use if the key doesn't exist yet |

**Outputs:**
| Output | Type | Description |
|--------|------|-------------|
| `value` | Any | The stored value, coerced to the declared type |
| `found` | Boolean | `true` if the key exists |
| `key` | String | The key that was read |
| `valueType` | String | The declared value type |

### `system.write-config` — Write Config

Writes a value to the store. Creates the key if it doesn't exist; updates it if it does.

**Config:**
| Field | Required | Description |
|-------|----------|-------------|
| `key` | Yes | The config key to write |
| `value` | No | Literal value to write (ignored if a `value` input is wired) |
| `valueType` | No | Type hint: `string` \| `number` \| `boolean` \| `json` \| `datetime` |

**Input:**
- `value` — wire from an upstream node to use its output as the written value (takes priority over the literal config value)

**Outputs:**
| Output | Type | Description |
|--------|------|-------------|
| `key` | String | The key that was written |
| `newValue` | String | The value that was written |
| `previousValue` | String | The value before this write (null if key was new) |

---

## Value Types

| Type | Behaviour |
|------|-----------|
| `string` | Stored and returned as-is |
| `number` | Coerced to `double` on read |
| `boolean` | Must be `true`/`false`/`1`/`0`; coerced to `bool` on read |
| `json` | Stored as raw JSON string; passed through as string |
| `datetime` | Parsed to ISO-8601 on read |

---

## Example: Email Sync with State

A workflow that downloads emails since the last run date and updates the date when done.

**Workflow design:**
```
Start
  ↓
Read Config (key: gmail.last_sync_date, defaultValue: 2020-01-01)
  ↓
Gmail Read (sinceDate: {{value}})
  ↓
... process emails ...
  ↓
Write Config (key: gmail.last_sync_date)
  ← wire from a Set Variable node that computed today's date
  ↓
End
```

**Step by step:**
1. **Read Config** reads `gmail.last_sync_date` → outputs `value = "2026-05-29"`
2. **Gmail Read** uses `{{value}}` as the `sinceDate` parameter
3. After processing, a **Set Variable** node computes today's date: `2026-05-30`
4. **Write Config** receives `value = "2026-05-30"` from the Set Variable input and writes it back

Next time the workflow runs, it starts from `2026-05-30`.

---

## Example: Counter

Increment a counter each time a workflow runs:

```
Start → Read Config (key: run_count, defaultValue: 0) → [compute count+1] → Write Config (key: run_count) → End
```

Use a **Set Variable** node with expression `{{value + 1}}` between Read and Write.

---

## API Access

You can also read and write config from external systems:

```bash
# Read a value
curl -H "Authorization: Bearer <token>" \
  http://localhost:5080/api/config/gmail.last_sync_date

# Write a value
curl -X PUT -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"value": "2026-05-30"}' \
  http://localhost:5080/api/config/gmail.last_sync_date
```
