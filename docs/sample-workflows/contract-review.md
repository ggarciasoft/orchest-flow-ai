# Contract Review Workflow (MVP Sample)

This is the canonical end-to-end demo for the OrchestFlowAI MVP. It exercises every architectural pillar: workflow-as-data, node SDK, engine, AI runtime, structured outputs, human approval, persistence, and timeline visibility.

---

## 1. Goal

Process an uploaded contract PDF through an AI-driven review and produce a final report, pausing for human approval if risk is high.

```
Upload Contract PDF
  → Extract Text
    → AI Analyze Contract Risk
      → AI Generate Executive Summary
        → If Risk Is High → Human Approval
          → Generate Final Report
            → Complete
```

---

## 2. Definition

```json
{
  "id": "contract-review-v1",
  "name": "Contract Review Workflow",
  "version": 1,
  "nodes": [
    { "id": "start",       "type": "system.start",                "position": { "x":  100, "y": 100 }, "config": {} },
    { "id": "extractPdf",  "type": "document.extract-pdf-text",   "position": { "x":  350, "y": 100 }, "config": { "ocrFallback": false } },
    { "id": "analyzeRisk", "type": "ai.contract-risk-analysis",   "position": { "x":  600, "y": 100 }, "config": { "model": "default", "riskThreshold": "high", "language": "auto" } },
    { "id": "summary",     "type": "ai.executive-summary",        "position": { "x":  850, "y": 100 }, "config": { "model": "default", "maxWords": 250, "tone": "formal" } },
    { "id": "riskGate",    "type": "logic.condition",             "position": { "x": 1100, "y": 100 }, "config": { "expression": "riskLevel == 'High'" } },
    { "id": "approval",    "type": "human.approval",              "position": { "x": 1350, "y": 100 }, "config": { "title": "Approve high-risk contract", "assignees": [{ "role": "approver" }], "slaMinutes": 1440 } },
    { "id": "end",         "type": "system.end",                  "position": { "x": 1600, "y": 100 }, "config": {} }
  ],
  "edges": [
    { "source": "start",       "target": "extractPdf",  "map": { "document": "document" } },
    { "source": "extractPdf",  "target": "analyzeRisk", "map": { "text": "text" } },
    { "source": "analyzeRisk", "target": "summary",     "map": { "summary": "text" } },
    { "source": "summary",     "target": "riskGate" },
    { "source": "riskGate",    "target": "approval", "condition": "result == true",  "map": { "riskLevel": "riskLevel", "summary": "summary" } },
    { "source": "riskGate",    "target": "end",      "condition": "result == false" },
    { "source": "approval",    "target": "end" }
  ]
}
```

---

## 3. Inputs

```json
{
  "document": { "documentId": "<uploaded-document-uuid>" }
}
```

---

## 4. Expected Outputs (per Node)

### `extractPdf`
```json
{ "text": "…full extracted contract text…", "pageCount": 12 }
```

### `analyzeRisk`
```json
{
  "riskLevel": "High",
  "summary": "The contract contains broad liability clauses and missing termination protections.",
  "keyClauses": [
    { "title": "Liability", "risk": "High", "reason": "Liability is uncapped." }
  ],
  "recommendedAction": "Send to legal review before signing."
}
```

### `summary`
```json
{ "summary": "Executive summary text…" }
```

### `riskGate`
```json
{ "result": true }
```

### `approval` (after the human acts)
```json
{ "decision": "approved", "comment": "OK after legal sign-off.", "decidedBy": "user-uuid", "decidedAt": "2026-05-22T20:00:00Z" }
```

---

## 5. Step-by-Step Demo

1. Log in as the demo user.
2. **Upload** the sample contract PDF at `samples/contract-review-workflow/sample-contract.pdf`.
3. Open **Workflows → Contract Review Workflow**.
4. Click **Execute**, pick the uploaded document, **Run**.
5. Open **Execution Details**:
   - Watch nodes flip from Pending → Running → Succeeded.
   - At `approval`, the workflow pauses; status becomes `Paused`.
6. Open **Approvals**, find the pending request:
   - Risk level: High
   - AI recommendation visible
7. Click **Approve** with a comment.
8. Execution resumes; the timeline completes.

---

## 6. What This Proves

- A workflow runs end-to-end as **data**.
- The engine executes nodes through interfaces.
- A document node consumes a typed `DocumentRef`.
- The AI runtime produces a **structured output** that the engine can branch on.
- A `logic.condition` node drives conditional routing.
- A `human.approval` node pauses and resumes the workflow.
- Persistence captures every transition; the timeline shows it.
- AI usage is logged for cost tracking.

---

## 7. Variations to Try

- Upload a benign contract → `riskLevel` is Low → no approval requested → workflow completes straight through.
- Modify `analyzeRisk.config.riskThreshold` to `medium` and observe more workflows pausing for approval.
- Switch the model in node config and watch `ai_usage_logs` shift.

---

## 8. Files

- `samples/contract-review-workflow/workflow.json` — the definition above.
- `samples/contract-review-workflow/sample-contract.pdf` — fixture document.
- `samples/contract-review-workflow/README.md` — how to seed it.
