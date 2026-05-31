# How To: Form Builder

The **Form Builder** lets you create custom forms that can pause a workflow and collect input from a user. Once submitted, the form values flow downstream as named node outputs (e.g. `{{name}}`, `{{amount}}`).

---

## Creating a Form

1. Go to **Forms** in the sidebar
2. Click **+ New Form**
3. Fill in:
   - **Name** — human-readable label (e.g. "Customer Intake")
   - **Slug** — auto-generated from the name; used as the node type `form.<slug>`
   - **Description** — optional notes shown on the fill page
4. Add fields (see below)
5. Click **Save**

The form is immediately available as a node type in the workflow designer under **Forms**.

---

## Adding Fields

Click **Add Field** to open the field editor. Each field has:

| Property | Description |
|----------|-------------|
| **Key** | Snake_case identifier — becomes the output variable name, e.g. `full_name` → `{{full_name}}` |
| **Label** | Human-readable label shown on the form |
| **Type** | See field types below |
| **Required** | Whether the field must be filled before submission |
| **Placeholder** | Hint text inside the input |

### Field Types

| Type | Description |
|------|-------------|
| `text` | Single-line text input |
| `number` | Numeric input |
| `email` | Email address with format validation |
| `date` | Date picker |
| `boolean` | Checkbox (yes/no) |
| `select` | Dropdown with fixed options |
| `file` | File upload — stores `{ id, filename, mimeType }` via the Documents API |

### Select Fields

For `select` type you have two options:

- **Static options** — enter comma-separated values: `Option A, Option B, Option C`
- **Dynamic options** — set `optionsFrom` to an output key from a previous node (e.g. a `data.db-query` node outputting `rows = ["Food", "Transport"]`)

### Regex Validation

Any field type supports optional regex validation:
- **Regex Pattern** — e.g. `^\d{4}-\d{2}-\d{2}$` for dates
- **Validation Message** — shown to the user when the pattern doesn't match

### File Fields

For `file` type, set **Accepted file types** to restrict uploads:
- Examples: `.pdf,.png`, `image/*`, `application/pdf`
- Leave blank to allow any file

---

## Using AI to Generate Fields

Click **✨ AI** in the form builder toolbar to open the AI assistant:

- **Describe the form:** *"A customer intake form with name, email, company, and a file upload for their contract"*
- **Generate from a schema:** *"Create fields from this: [{ key: 'name', type: 'text', required: true }, ...]"*
- **Modify existing fields:** *"Add a phone number field and make email required"*

Click **Apply** to replace the current fields with the AI result. Always review before saving.

> ⚠️ AI can make mistakes — check field keys, types, and required flags after applying.

---

## Previewing a Form

Click **Preview** in the toolbar to see exactly how the form will look to a user filling it in during a workflow.

---

## Versioning

Every time you save, a new **version** is created. The **Version History** panel (clock icon on the form page) lets you:
- See all saved versions with timestamps
- Load a version to restore it to the editor
- Activate a version to make it the live one

> **Important:** When a form node is already in a running workflow, the version used at execution time is snapshotted. Changing the form doesn't affect in-progress executions.

---

## Using a Form in a Workflow

1. Open the **Workflow Designer**
2. In the Node Palette, look under **Forms** — your form appears as `form.<slug>`
3. Drag it onto the canvas and connect it in the flow

**How it works at runtime:**
1. The workflow reaches the form node and pauses
2. The user sees the form in the Approvals inbox (or via a direct link)
3. After submitting, each field value becomes a named output: `{{full_name}}`, `{{email}}`, etc.
4. The workflow resumes and downstream nodes can reference those values

---

## Example: Multi-Step Onboarding

```
Start
  ↓
form.pg-personal-info   (Full Name, Email, Date of Birth)
  ↓
form.pg-employment      (Company, Job Title, Start Date)
  ↓
form.pg-preferences     (Newsletter, Timezone, Notes)
  ↓
End
```

Each form pauses the workflow in turn. The collected data is available as outputs throughout.

> **Try it:** Go to **Playground → Form Playground** for a live demo of this exact workflow.
