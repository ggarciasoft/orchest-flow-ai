# How To: AI Builder (Workflow & Form Generator)

OrchestFlowAI has two AI-powered builders — one for workflows and one for forms. Both use your configured LLM provider (OpenAI, Anthropic, Azure, or Ollama).

---

## Prerequisites

Before using the AI builders, configure an AI provider:

1. Go to **Settings → AI Providers**
2. Enter your API key for the desired provider (e.g. OpenAI)
3. Click **Test connection** to verify it works
4. Click **Set as default provider**

If no provider is configured, the AI panels show an amber warning and the input is disabled.

---

## Workflow AI Assistant

### Opening

1. Open the **Workflow Designer**
2. Click **✨ Sparkles** in the toolbar

The panel shows the active provider and model (e.g. `openai` · `gpt-4o-mini`) at the top.

### What you can do

**Create from scratch:**
> "Create a workflow that reads a PDF, extracts key information, and sends a Slack summary"

**Modify an existing workflow:**
> "Add an approval step before the final email is sent"

**Ask about nodes:**
> "What node should I use to wait for an external system to POST data?"

### How it works

1. Type your request and press Enter (or click Send)
2. The AI generates or modifies the workflow definition
3. The assistant responds with an explanation and list of changes
4. Click **Preview** to see the result on the canvas without committing
5. Click **Accept** to apply the changes and trigger a Save

### Reading the response

Each AI response shows:
- The explanation of what changed
- A list of specific changes made
- Footer: provider · model · token count (e.g. `openai` · `gpt-4o-mini` · 1,842 tokens)

### Chat history

Every session is saved to **Settings → AI History**. You can browse past sessions, see the full message thread, and review token usage.

---

## Form AI Assistant

### Opening

1. Go to **Forms** → open or create a form
2. Click **✨ AI** in the top toolbar

### What you can do

**Generate from a description:**
> "Create a customer intake form with name, email, phone, and company fields"

**Generate from a schema:**
> "Create a form with this: [{ "key": "name", "type": "text", "required": true }, ...]"

**Modify existing fields:**
> "Add a file upload field for photo ID"

**Supported field types:** `text`, `number`, `select`, `date`, `email`, `boolean`, `file`

### How it works

1. The AI generates a field definition array
2. Click **Apply** to replace the current fields with the AI result
3. Review and edit fields in the builder as needed
4. Click **Save** to persist

### Tips

- The AI preserves existing fields when you ask for additions or modifications
- `file` fields upload via the Documents API and store `{ id, filename, mimeType }` as the value
- `select` fields can have static `options` or a dynamic `optionsFrom` key referencing upstream node output
- Regex validation is available on any field type (`validationRegex` + `validationMessage`)

---

## Switching Providers

To change the AI provider used by both builders:

1. **Settings → AI Providers**
2. Click a provider card (OpenAI, Anthropic, Azure, Ollama)
3. Click **Set as default provider**

The change takes effect immediately — no restart needed. The active provider badge in both panels updates on next open.
