# How To: Visual Workflow Designer

The workflow designer is a full-screen drag-and-drop canvas for building automation pipelines without writing code.

---

## Opening the Designer

1. Go to **Workflows** in the sidebar
2. Click an existing workflow name, or click **New Workflow**
3. Click **Designer** to open the canvas

---

## Building Your First Workflow

Every workflow needs a **Start** and an **End** node.

1. From the **Node Palette** on the left, find `system` → drag **Start** onto the canvas
2. Drag **End** onto the canvas
3. Connect them by hovering over the bottom handle of Start until a blue dot appears, then drag to the top handle of End
4. Click **Save** in the toolbar

---

## Adding Nodes

1. Browse or search the **Node Palette** — nodes are grouped by category (AI, Logic, Human, Data, Integrations, System)
2. Drag a node onto the canvas, or click it to place it at a random position
3. Click the node to open the **Config Drawer** on the right
4. Fill in the required config fields (marked with ★)
5. Connect nodes by dragging from the bottom handle (source) to the top handle (target) of another node

---

## Configuring a Node

Click any node to open its config panel:

- **Required fields** are marked — the workflow won't save if they're empty
- **Sensitive fields** (API keys, passwords) show a masked input; use `{{secret:name}}` to reference a stored secret from **Settings → Secrets**
- **Model fields** on AI nodes show a dropdown populated from your configured providers

---

## Connecting Nodes

- Drag from the **bottom handle** of one node to the **top handle** of another
- Right-click a connection line → **Delete Line** to remove it
- Press `Delete` or `Backspace` with a connection selected to remove it

---

## Wiring Outputs to Inputs

Upstream node outputs automatically flow to downstream node inputs. Reference them in config fields using `{{outputKey}}` syntax:

```
{{text}}          ← output from an Extract PDF node
{{approved}}      ← output from a Human Approval node
{{rows}}          ← output from a Database Query node
```

---

## Using the AI Assistant

Click the **✨ Sparkles** button in the toolbar to open the AI Assistant panel:

1. Type what you want in plain English: *"Add an HTTP node that calls the GitHub API to list open issues"*
2. The AI generates or modifies the workflow definition
3. Click **Preview** to see the changes on the canvas, or **Accept** to apply and save
4. The panel shows which AI provider and model was used, plus token count

> The AI assistant requires an API key configured in **Settings → AI Providers**.

---

## Saving and Versioning

- Click **Save** — creates a new version and activates it
- Click **History** (clock icon) to see all saved versions
- Click **Load** on any version to restore it to the canvas (doesn't activate yet)
- Click **Activate** to make a version the live one

---

## Running a Workflow

- Click **▶ Run** in the toolbar
- Fill in any required inputs in the modal
- The execution appears in **Executions** or in **Workflows → History**

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Delete` / `Backspace` | Delete selected node or connection |
| Right-click node | Context menu (Delete) |
| Right-click connection | Context menu (Delete Line) |

---

## Tips

- Nodes are color-coded by category — purple = AI, green = Documents, yellow = Logic, red = Human, blue = Integrations
- Every node shows its icon and display name on the canvas
- The **Trigger** button lets you set Manual, Webhook, or Cron scheduling for the workflow
