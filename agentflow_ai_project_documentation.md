# AgentFlow Enterprise — AI Project Documentation

> Working name: **AgentFlow Enterprise**  
> Alternative names: **OrchestAI**, **AgentBridge**, **FlowMind**  
> Purpose: Open-source enterprise AI workflow platform for orchestrating agents, tools, human approvals, documents, and business automation.

---

## 1. Product Vision

AgentFlow Enterprise is a modular AI workflow automation platform where users can create custom business workflows using reusable nodes/components.

The platform should allow users to:

- Build workflows visually using existing nodes.
- Execute workflows asynchronously.
- Use AI nodes for analysis, summarization, classification, extraction, reasoning, and tool calling.
- Add human approval steps.
- Integrate with external systems through connectors.
- Track execution history, audit logs, errors, retries, AI usage, and costs.
- Allow developers to create new nodes over time.

The long-term vision is an extensible enterprise workflow ecosystem, similar in spirit to workflow automation tools, but focused on AI-driven business automation.

---

## 2. Core Principles

### 2.1 Modular First

The platform must be designed around modular components:

- Core engine
- Node SDK
- Node registry
- Workflow designer
- Execution worker
- AI runtime
- Connectors
- Observability

The engine should not know implementation details of specific nodes.

### 2.2 Workflows Are Data

Workflows should be stored as structured definitions, not hardcoded logic.

A workflow definition should contain:

- Nodes
- Edges
- Inputs
- Outputs
- Configuration
- Version
- Metadata

### 2.3 Nodes Are Reusable Components

Each node should represent a reusable unit of work.

Examples:

- AI Summarize
- AI Classify
- Extract PDF Text
- Human Approval
- Send Email
- HTTP Request
- Condition
- Delay
- Webhook Trigger

### 2.4 Human-in-the-Loop Is First-Class

Enterprise AI workflows often require human review. Approval and manual review nodes must be part of the core MVP.

### 2.5 Auditability Matters

Every execution should be traceable:

- Who triggered it
- When it started
- Which node ran
- What input/output was produced
- Which AI model was used
- Token usage
- Errors
- Retries
- Approval decisions

### 2.6 AI Must Produce Structured Outputs

Avoid relying only on free-form text responses. AI nodes should prefer structured JSON outputs with schemas when possible.

### 2.7 Start Practical, Then Expand

Do not build a massive platform from day one. The MVP should focus on one strong business workflow: **Contract Review Workflow**.

---

## 3. Target MVP

### MVP Name

**Contract Review Workflow MVP**

### MVP Goal

Allow a user to upload a contract PDF, process it through an AI workflow, classify risk, generate a summary, and request human approval if needed.

### MVP Workflow

```txt
Upload Contract PDF
→ Extract Text
→ AI Analyze Contract Risk
→ AI Generate Executive Summary
→ Condition: If Risk Is High
    → Human Approval
→ Generate Final Report
→ Complete Execution
```

### MVP Capabilities

- User can create or use a predefined contract review workflow.
- User can upload a PDF document.
- System extracts text from the document.
- AI analyzes the document.
- AI returns structured output:
  - summary
  - key clauses
  - risk level
  - risk reasons
  - recommended action
- If risk is high, workflow pauses for approval.
- User can approve or reject.
- System stores full execution history.
- User can view node-by-node execution timeline.

---

## 4. Recommended Monorepo Structure

```txt
agentflow-enterprise/
│
├── apps/
│   ├── web/
│   └── docs/
│
├── services/
│   ├── AgentFlow.Api/
│   ├── AgentFlow.Worker/
│   └── AgentFlow.AI/
│
├── packages/
│   ├── AgentFlow.Domain/
│   ├── AgentFlow.Application/
│   ├── AgentFlow.Infrastructure/
│   ├── AgentFlow.Contracts/
│   ├── AgentFlow.Engine/
│   ├── AgentFlow.SDK/
│   └── AgentFlow.Observability/
│
├── nodes/
│   ├── ai/
│   ├── documents/
│   ├── human/
│   ├── logic/
│   ├── integrations/
│   └── system/
│
├── deploy/
│   ├── docker-compose.yml
│   ├── k8s/
│   └── terraform/
│
├── samples/
│   ├── contract-review-workflow/
│   ├── procurement-approval-workflow/
│   └── customer-support-workflow/
│
├── docs/
│   ├── architecture.md
│   ├── workflow-engine.md
│   ├── node-sdk.md
│   ├── ai-runtime.md
│   ├── security.md
│   ├── observability.md
│   ├── roadmap-mvp.md
│   └── roadmap-future.md
│
└── README.md
```

---

## 5. Backend Project Responsibilities

### 5.1 AgentFlow.Api

Main API used by the web application.

Responsibilities:

- Workflow management
- Workflow version management
- Execution triggering
- Execution history retrieval
- Approval actions
- Node metadata exposure
- Document upload endpoints
- Authentication and authorization entry point

Example endpoints:

```txt
GET    /api/workflows
POST   /api/workflows
GET    /api/workflows/{workflowId}
POST   /api/workflows/{workflowId}/execute
GET    /api/executions/{executionId}
GET    /api/executions/{executionId}/timeline
POST   /api/approvals/{approvalId}/approve
POST   /api/approvals/{approvalId}/reject
GET    /api/nodes/catalog
POST   /api/documents/upload
```

### 5.2 AgentFlow.Worker

Background execution service.

Responsibilities:

- Pick pending workflow executions.
- Execute nodes in order.
- Persist node execution state.
- Handle retries.
- Pause workflows awaiting human approval.
- Resume workflows after approvals.
- Publish execution events.

### 5.3 AgentFlow.AI

AI runtime service or module.

Responsibilities:

- LLM provider abstraction.
- Prompt execution.
- Structured outputs.
- AI classification.
- AI summarization.
- AI document analysis.
- Future tool calling.
- Future RAG integration.

### 5.4 AgentFlow.Engine

Core workflow runtime.

Responsibilities:

- Load workflow definition.
- Validate graph.
- Resolve executable nodes.
- Manage execution context.
- Route outputs from one node to another.
- Evaluate conditions.
- Handle terminal states.

### 5.5 AgentFlow.SDK

Developer SDK for creating nodes.

Responsibilities:

- Node interfaces.
- Node descriptor model.
- Input/output definitions.
- Configuration schema definitions.
- Node execution contracts.

### 5.6 AgentFlow.Contracts

Shared contracts.

Contains:

- DTOs
- Events
- API request/response models
- Common enums

### 5.7 AgentFlow.Domain

Pure domain layer.

Contains entities such as:

- Workflow
- WorkflowVersion
- WorkflowNode
- WorkflowEdge
- WorkflowExecution
- NodeExecution
- ApprovalRequest
- Document
- AIUsageLog
- AuditLog
- Tenant
- User

### 5.8 AgentFlow.Infrastructure

Infrastructure adapters.

Contains:

- PostgreSQL persistence
- Redis cache
- Queue provider
- File/document storage
- AI provider clients
- Email provider
- Logging/tracing adapters

### 5.9 AgentFlow.Observability

Cross-cutting observability.

Contains:

- Correlation ID middleware
- OpenTelemetry configuration
- Structured logging helpers
- Metrics definitions
- AI usage tracking abstractions

---

## 6. Node System

### 6.1 Node Categories

```txt
nodes/
├── ai/
├── documents/
├── human/
├── logic/
├── integrations/
└── system/
```

### 6.2 MVP Nodes

#### Documents

- `document.extract-pdf-text`

#### AI

- `ai.contract-risk-analysis`
- `ai.executive-summary`

#### Logic

- `logic.condition`

#### Human

- `human.approval`

#### System

- `system.start`
- `system.end`

### 6.3 Future Nodes

#### AI Nodes

- `ai.summarize`
- `ai.classify`
- `ai.extract-entities`
- `ai.sentiment-analysis`
- `ai.compare-documents`
- `ai.generate-email`
- `ai.agent-executor`
- `ai.rag-search`
- `ai.translate`
- `ai.policy-check`

#### Document Nodes

- `document.ocr`
- `document.classify`
- `document.split`
- `document.extract-tables`
- `document.generate-pdf-report`

#### Logic Nodes

- `logic.condition`
- `logic.loop`
- `logic.delay`
- `logic.parallel`
- `logic.switch`
- `logic.retry-policy`

#### Human Nodes

- `human.approval`
- `human.manual-review`
- `human.assign-task`

#### Integration Nodes

- `integration.email.send`
- `integration.webhook.call`
- `integration.slack.send-message`
- `integration.teams.send-message`
- `integration.jira.create-ticket`
- `integration.http.request`
- `integration.database.query`

---

## 7. Node Contract Design

### 7.1 Node Execution Interface

```csharp
public interface IWorkflowNode
{
    string Type { get; }

    Task<NodeExecutionResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken);
}
```

### 7.2 Node Descriptor Interface

```csharp
public interface IWorkflowNodeDescriptor
{
    string Type { get; }
    string DisplayName { get; }
    string Description { get; }
    string Category { get; }
    IReadOnlyCollection<NodeInputDefinition> Inputs { get; }
    IReadOnlyCollection<NodeOutputDefinition> Outputs { get; }
    IReadOnlyCollection<NodeConfigDefinition> Configuration { get; }
}
```

### 7.3 Node Execution Result

```csharp
public sealed class NodeExecutionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object?> Outputs { get; init; } = new();
    public NodeExecutionStatus Status { get; init; }
}
```

### 7.4 Node Execution Status

```csharp
public enum NodeExecutionStatus
{
    Succeeded,
    Failed,
    WaitingForApproval,
    Skipped,
    Cancelled
}
```

---

## 8. Workflow Definition Shape

Initial workflow JSON example:

```json
{
  "id": "contract-review-v1",
  "name": "Contract Review Workflow",
  "version": 1,
  "nodes": [
    {
      "id": "start",
      "type": "system.start",
      "position": { "x": 100, "y": 100 },
      "config": {}
    },
    {
      "id": "extractPdf",
      "type": "document.extract-pdf-text",
      "position": { "x": 350, "y": 100 },
      "config": {}
    },
    {
      "id": "analyzeRisk",
      "type": "ai.contract-risk-analysis",
      "position": { "x": 600, "y": 100 },
      "config": {
        "model": "default",
        "riskThreshold": "high"
      }
    },
    {
      "id": "approval",
      "type": "human.approval",
      "position": { "x": 850, "y": 100 },
      "config": {
        "requiredWhen": "riskLevel == 'High'"
      }
    },
    {
      "id": "end",
      "type": "system.end",
      "position": { "x": 1100, "y": 100 },
      "config": {}
    }
  ],
  "edges": [
    { "source": "start", "target": "extractPdf" },
    { "source": "extractPdf", "target": "analyzeRisk" },
    { "source": "analyzeRisk", "target": "approval" },
    { "source": "approval", "target": "end" }
  ]
}
```

---

## 9. Initial Database Model

### Required Tables for MVP

```txt
users
tenants
workflows
workflow_versions
workflow_executions
node_executions
approval_requests
documents
ai_usage_logs
audit_logs
```

### Suggested Important Fields

#### workflows

```txt
id
name
description
tenant_id
created_by
created_at
updated_at
is_deleted
```

#### workflow_versions

```txt
id
workflow_id
version_number
definition_json
created_at
created_by
is_active
```

#### workflow_executions

```txt
id
workflow_id
workflow_version_id
status
started_at
completed_at
triggered_by
input_json
output_json
error_message
correlation_id
```

#### node_executions

```txt
id
workflow_execution_id
node_id
node_type
status
started_at
completed_at
input_json
output_json
error_message
retry_count
```

#### approval_requests

```txt
id
workflow_execution_id
node_execution_id
status
requested_at
responded_at
requested_by
responded_by
decision
comment
```

#### ai_usage_logs

```txt
id
workflow_execution_id
node_execution_id
provider
model
prompt_tokens
completion_tokens
total_tokens
estimated_cost
created_at
```

---

## 10. Frontend Application

### Stack

- Next.js
- React
- TypeScript
- Tailwind CSS
- shadcn/ui
- React Flow
- TanStack Query

### Main Screens

```txt
Dashboard
Workflows
Workflow Designer
Executions
Execution Details
Approvals
Documents
Node Catalog
Settings
```

### MVP Screens

#### Dashboard

Shows:

- total workflows
- recent executions
- pending approvals
- failed executions
- AI usage summary

#### Workflows List

Shows:

- workflow name
- version
- status
- last execution
- created by

#### Workflow Designer

Visual canvas where users can:

- view nodes
- connect nodes
- configure nodes
- validate workflow
- save workflow version
- execute workflow

#### Execution Details

Shows:

- execution status
- timeline
- node inputs/outputs
- errors
- approvals
- AI usage

#### Approval Inbox

Shows:

- pending approval requests
- risk summary
- AI recommendation
- approve/reject actions

---

## 11. AI Runtime Guidelines

### 11.1 AI Provider Abstraction

Do not couple the engine directly to a single AI provider.

Use an abstraction such as:

```csharp
public interface ILLMProvider
{
    Task<LLMResponse<TOutput>> GenerateStructuredAsync<TOutput>(
        LLMRequest request,
        CancellationToken cancellationToken);
}
```

### 11.2 Structured Output First

For important business decisions, AI output should be structured.

Example contract risk output:

```json
{
  "riskLevel": "High",
  "summary": "The contract contains broad liability clauses and missing termination protections.",
  "keyClauses": [
    {
      "title": "Liability",
      "risk": "High",
      "reason": "Liability is uncapped."
    }
  ],
  "recommendedAction": "Send to legal review before signing."
}
```

### 11.3 AI Safety Rules

- AI should not be treated as the final legal authority.
- High-risk decisions must support human review.
- AI outputs must be stored with model/provider metadata.
- Prompts should be versioned.
- Sensitive data handling must be documented.

---

## 12. Security Requirements

### MVP Security

- Authentication required for app access.
- Tenant isolation in all queries.
- Role-based access for approvals and workflow editing.
- Uploaded documents must be associated with tenant and owner.
- Do not expose raw secrets to frontend.
- Store provider API keys securely.

### Future Security

- SSO/SAML/OIDC
- Per-node permissions
- Data retention policies
- Audit export
- Field-level encryption for sensitive data
- Secrets vault integration

---

## 13. Observability Requirements

### MVP Observability

- Structured logs
- Correlation IDs
- Execution timeline
- Node execution status
- Error details
- AI usage logs

### Future Observability

- OpenTelemetry traces
- Distributed tracing
- Grafana dashboards
- AI cost dashboards
- Retry metrics
- Node performance analytics

---

## 14. AI Coding Assistant Rules

Use these rules in Cursor, Antigravity, or any AI coding tool.

### 14.1 General Rules

1. Do not hardcode workflows in application logic.
2. Workflows must be represented as data.
3. The workflow engine must execute nodes through interfaces.
4. Nodes must be reusable and isolated.
5. Nodes must not persist workflow execution state directly.
6. The engine owns state management.
7. All API endpoints must validate tenant scope.
8. All AI outputs used for business decisions should be structured.
9. Avoid provider lock-in for AI integrations.
10. Prefer clean architecture boundaries.

### 14.2 Backend Rules

1. Keep domain entities free from infrastructure concerns.
2. Application layer handles use cases.
3. Infrastructure implements external dependencies.
4. API controllers should be thin.
5. Worker should use application services, not duplicate business logic.
6. Use cancellation tokens in async operations.
7. All execution state changes must be persisted.
8. Failed node executions should store error messages.
9. Use idempotency where execution retries are possible.
10. Do not store secrets in workflow JSON.

### 14.3 Frontend Rules

1. The workflow designer must render nodes dynamically from backend node metadata.
2. Do not hardcode all node forms if metadata can drive the UI.
3. Keep workflow canvas state separate from persisted workflow definition.
4. Use typed API clients where possible.
5. Show validation errors before saving workflows.
6. Execution views should be read-only.
7. Approval actions should require confirmation.

### 14.4 Node Development Rules

1. Every node must have:
   - type
   - display name
   - description
   - category
   - inputs
   - outputs
   - configuration schema
2. Node types must be globally unique.
3. Node outputs must be serializable.
4. Node errors must be explicit.
5. Nodes should be stateless.
6. Nodes should not know about UI concerns.
7. Nodes should be testable independently.

### 14.5 AI Node Rules

1. AI nodes must log provider and model.
2. AI nodes must track token usage when available.
3. AI prompts must be versionable.
4. AI nodes must prefer structured output.
5. AI nodes must expose confidence/risk when relevant.
6. AI nodes should not make irreversible actions without human approval unless explicitly configured.

---

## 15. Suggested AI Agents for Development

These are project-specific development agents for an AI coding environment.

### 15.1 Product Architect Agent

Purpose:

- Own product direction.
- Prevent scope creep.
- Ensure features align with MVP.

Responsibilities:

- Review feature requests.
- Keep MVP focused.
- Maintain roadmap.
- Ensure workflows remain data-driven.

### 15.2 Backend Architect Agent

Purpose:

- Own backend architecture.

Responsibilities:

- Define clean architecture boundaries.
- Review API design.
- Review workflow engine implementation.
- Ensure multi-tenant and security rules.

### 15.3 Workflow Engine Agent

Purpose:

- Build and maintain the workflow runtime.

Responsibilities:

- Graph validation.
- Node execution.
- Execution state transitions.
- Retry behavior.
- Waiting/resuming human approvals.

### 15.4 Node SDK Agent

Purpose:

- Own the developer experience for creating nodes.

Responsibilities:

- Define node interfaces.
- Define node descriptors.
- Ensure nodes are discoverable.
- Create sample nodes.

### 15.5 AI Runtime Agent

Purpose:

- Own AI provider abstraction and AI behavior.

Responsibilities:

- Implement LLM provider interfaces.
- Implement structured output handling.
- Implement prompt templates.
- Track AI usage.

### 15.6 Frontend Agent

Purpose:

- Build the web application.

Responsibilities:

- Workflow designer.
- Dashboard.
- Execution timeline.
- Approval inbox.
- Node catalog UI.

### 15.7 DevOps Agent

Purpose:

- Own local development and deployment.

Responsibilities:

- Docker Compose.
- Environment configuration.
- CI/CD.
- Database migrations.
- Observability setup.

### 15.8 QA Agent

Purpose:

- Ensure stability and correctness.

Responsibilities:

- Unit tests.
- Integration tests.
- Workflow execution tests.
- Node tests.
- Regression testing.

### 15.9 Documentation Agent

Purpose:

- Keep docs aligned with the system.

Responsibilities:

- README.
- Architecture docs.
- Node development guide.
- API docs.
- Sample workflows.

---

## 16. Suggested Skills for AI Development Environment

### Skill: Create New Node

When asked to create a new node:

1. Define the node type.
2. Define descriptor metadata.
3. Define inputs and outputs.
4. Define configuration schema.
5. Implement execution logic.
6. Add unit tests.
7. Add documentation.
8. Add sample workflow usage.

### Skill: Create Workflow Feature

When asked to add workflow functionality:

1. Check if it belongs in engine, node, API, or UI.
2. Avoid hardcoding workflow logic.
3. Update workflow schema if needed.
4. Add validation.
5. Add tests.
6. Update docs.

### Skill: Add AI Capability

When asked to add AI functionality:

1. Use AI provider abstraction.
2. Prefer structured output.
3. Add prompt template.
4. Track usage.
5. Store model/provider metadata.
6. Add tests with mocked provider.

### Skill: Add UI Screen

When asked to add a UI screen:

1. Define user goal.
2. Define data needed.
3. Use existing design system.
4. Keep components reusable.
5. Add loading/error/empty states.
6. Connect to typed API client.

### Skill: Improve Documentation

When asked to document a feature:

1. Explain purpose.
2. Explain how it works.
3. Include diagrams when useful.
4. Include examples.
5. Include known limitations.
6. Keep docs aligned with actual implementation.

---

## 17. MVP Roadmap

### Phase 0 — Repository Setup

Deliverables:

- Create monorepo.
- Add README.
- Add docs folder.
- Add base solution structure.
- Add Docker Compose skeleton.
- Add coding conventions.

Acceptance criteria:

- Repo builds locally.
- Documentation explains project purpose.
- Basic development setup is clear.

---

### Phase 1 — Domain and Workflow Schema

Deliverables:

- Domain entities.
- Workflow definition model.
- Workflow version model.
- Basic validation rules.
- Initial database schema.

Acceptance criteria:

- A workflow can be represented as JSON.
- Workflow definition can be validated.
- Workflow/version can be persisted.

---

### Phase 2 — Node SDK and Node Registry

Deliverables:

- IWorkflowNode interface.
- IWorkflowNodeDescriptor interface.
- Node registry.
- Node metadata endpoint.
- System start/end nodes.

Acceptance criteria:

- Backend can list available nodes.
- Nodes can be registered.
- UI can consume node metadata.

---

### Phase 3 — Workflow Execution Engine

Deliverables:

- Execution creation.
- Sequential node execution.
- Node input/output mapping.
- Execution state persistence.
- Node execution history.

Acceptance criteria:

- A simple workflow can execute from start to end.
- Each node execution is persisted.
- Failed node execution marks workflow as failed.

---

### Phase 4 — Document and AI MVP Nodes

Deliverables:

- PDF text extraction node.
- Contract risk analysis AI node.
- Executive summary AI node.
- Basic AI provider abstraction.
- AI usage logging.

Acceptance criteria:

- Uploaded contract can be processed.
- AI returns structured contract analysis.
- AI usage is logged.

---

### Phase 5 — Human Approval Node

Deliverables:

- Approval node.
- Approval request table.
- Pause workflow while waiting.
- Resume workflow after approval/rejection.
- Approval API endpoints.

Acceptance criteria:

- Workflow can pause for approval.
- User can approve or reject.
- Workflow resumes correctly.

---

### Phase 6 — Frontend MVP

Deliverables:

- Dashboard.
- Workflows list.
- Basic workflow designer.
- Execution details.
- Approval inbox.
- Document upload page.

Acceptance criteria:

- User can upload contract.
- User can execute contract review workflow.
- User can view execution timeline.
- User can approve/reject high-risk workflow.

---

### Phase 7 — Dockerized Demo

Deliverables:

- Docker Compose.
- PostgreSQL.
- Redis if needed.
- API.
- Worker.
- Web app.
- Seed sample workflow.

Acceptance criteria:

- New developer can run the MVP locally.
- Demo workflow works end-to-end.

---

### Phase 8 — Documentation and Public GitHub Polish

Deliverables:

- README with screenshots.
- Architecture docs.
- Node development guide.
- Contract review demo guide.
- Roadmap.
- Contribution guide.

Acceptance criteria:

- Repo looks professional.
- A developer understands how to run and extend the platform.

---

## 18. Future Roadmap

### Phase 9 — Visual Workflow Builder Improvements

Features:

- Drag-and-drop node canvas.
- Dynamic configuration forms.
- Workflow validation UI.
- Node search.
- Node categories.
- Workflow version comparison.

### Phase 10 — Advanced Logic Nodes

Nodes:

- Switch
- Loop
- Parallel
- Delay
- Retry policy
- Error handler

### Phase 11 — Integrations Pack

Nodes:

- Send email
- HTTP request
- Webhook trigger
- Slack message
- Microsoft Teams message
- Jira ticket

### Phase 12 — RAG and Knowledge Base

Features:

- Document ingestion.
- Embeddings.
- Vector search.
- Citations.
- Knowledge base management.
- RAG search node.

### Phase 13 — Agent System

Features:

- Agent configuration.
- Agent tools.
- Agent memory.
- Agent executor node.
- Multi-agent workflow support.

### Phase 14 — Enterprise Security

Features:

- SSO/OIDC.
- RBAC.
- Per-node permissions.
- Secrets management.
- Tenant-level policies.

### Phase 15 — Observability and Cost Management

Features:

- Cost dashboard.
- Token usage dashboard.
- Execution analytics.
- Node performance metrics.
- Failure analysis.

### Phase 16 — Developer Ecosystem

Features:

- Node package format.
- Plugin loading.
- Node marketplace concept.
- Node SDK documentation.
- Sample custom nodes.

### Phase 17 — AI Workflow Generator

Feature:

User describes a workflow in natural language, and AI generates a draft workflow definition.

Example prompt:

```txt
When a contract is uploaded, analyze it for risk, summarize it, and request legal approval if risk is high.
```

Output:

- Nodes
- Edges
- Suggested configuration
- Required approvals

---

## 19. Future Node Roadmap

### High Priority Future Nodes

1. `integration.email.send`
2. `integration.http.request`
3. `logic.switch`
4. `ai.summarize`
5. `ai.classify`
6. `ai.extract-entities`
7. `document.generate-pdf-report`
8. `integration.webhook.trigger`
9. `ai.rag-search`
10. `human.manual-review`

### Medium Priority Future Nodes

1. `integration.slack.send-message`
2. `integration.jira.create-ticket`
3. `document.ocr`
4. `logic.parallel`
5. `logic.delay`
6. `ai.translate`
7. `ai.compare-documents`
8. `integration.database.query`

### Advanced Future Nodes

1. `ai.agent-executor`
2. `ai.multi-agent-review`
3. `ai.workflow-planner`
4. `integration.salesforce.create-record`
5. `integration.sharepoint.upload-file`
6. `system.error-boundary`
7. `system.compensation-step`

---

## 20. README Draft

```md
# AgentFlow Enterprise

Open-source enterprise AI workflow platform for orchestrating agents, tools, human approvals, documents, and business automation.

## What is AgentFlow?

AgentFlow allows teams to build AI-powered workflows using reusable nodes. It combines workflow orchestration, AI reasoning, document processing, human approvals, and enterprise integrations.

## MVP Demo

The first demo workflow is Contract Review:

1. Upload contract PDF
2. Extract text
3. Analyze risk with AI
4. Generate executive summary
5. Request human approval if risk is high
6. Store execution history and audit trail

## Architecture

- .NET backend
- Next.js frontend
- PostgreSQL database
- Worker-based execution engine
- Modular node SDK
- AI provider abstraction

## Roadmap

- Visual workflow builder
- AI nodes
- Human approvals
- RAG
- Tool calling
- Integrations
- Plugin marketplace

## Status

Early MVP planning stage.
```

---

## 21. Prompt for Coding AI

Use this prompt in a coding agent to start the project.

```txt
You are a senior software architect and full-stack engineer helping build AgentFlow Enterprise.

AgentFlow Enterprise is an open-source enterprise AI workflow platform. The system allows users to create custom workflows using reusable nodes. Workflows are stored as data, executed by a backend workflow engine, and can include AI nodes, document nodes, logic nodes, integration nodes, and human approval nodes.

Build the system with these principles:

1. Use a modular monorepo structure.
2. Do not hardcode workflows in business logic.
3. Workflows must be represented as structured definitions with nodes and edges.
4. The workflow engine executes nodes through interfaces.
5. Nodes are reusable, stateless components.
6. The engine owns execution state, retries, and transitions.
7. The frontend workflow designer should render nodes dynamically from backend metadata.
8. AI provider integration must be abstracted.
9. AI nodes should prefer structured JSON outputs.
10. Human approval must be a first-class workflow step.
11. Execution history, audit logs, and AI usage tracking are required.
12. Keep the MVP focused on Contract Review Workflow.

Initial MVP workflow:

Upload Contract PDF
→ Extract Text
→ AI Analyze Contract Risk
→ AI Generate Executive Summary
→ If Risk Is High, Human Approval
→ Generate Final Report
→ Complete

Recommended stack:

Backend: .NET 9
Frontend: Next.js, React, TypeScript, Tailwind, shadcn/ui, React Flow
Database: PostgreSQL
Cache/Queue: Redis initially if needed
AI: Provider abstraction for OpenAI/Azure OpenAI/Anthropic
Deployment: Docker Compose for local development

Start by creating:

- Monorepo structure
- Backend solution structure
- Domain entities
- Workflow definition model
- Node SDK interfaces
- Node registry
- Basic API endpoints
- Worker skeleton
- Frontend app skeleton
- Initial documentation

Do not invent external integrations that are not implemented. Mark future features clearly as future roadmap items.
```

---

## 22. Prompt for UI/UX Design AI

Use this prompt with an AI design tool.

```txt
Design a modern enterprise SaaS web application UI for a product called AgentFlow Enterprise.

AgentFlow Enterprise is an AI workflow automation platform where business users and technical teams can create workflows using visual nodes. Workflows can include AI analysis, document processing, conditions, human approvals, and integrations.

The UI should feel premium, modern, clean, and enterprise-ready. It should look like a serious workflow automation and AI operations platform, not a playful chatbot app.

Design style:

- Clean SaaS dashboard
- Modern enterprise AI aesthetic
- Minimal but powerful
- Light mode first, with dark mode compatibility
- Rounded cards
- Soft shadows
- Clear hierarchy
- Good spacing
- Professional typography
- Subtle AI/automation visual language

Core screens to design:

1. Dashboard
2. Workflows List
3. Workflow Designer
4. Node Catalog Panel
5. Node Configuration Drawer
6. Execution Details / Timeline
7. Approval Inbox
8. Contract Review Demo Page
9. Settings / AI Providers

Most important screen:

Workflow Designer

The Workflow Designer should include:

- Left sidebar with node categories:
  - AI
  - Documents
  - Logic
  - Human
  - Integrations
  - System
- Main canvas with connected workflow nodes.
- Right-side configuration panel for selected node.
- Top toolbar with Save, Validate, Execute, Version, and Publish actions.
- Status indicators for validation errors.
- Mini-map or zoom controls if appropriate.

Example workflow to show in the design:

Start
→ Upload Contract PDF
→ Extract Text
→ AI Risk Analysis
→ Executive Summary
→ Condition: Risk is High
→ Human Approval
→ End

Execution Details screen should show:

- Workflow execution status
- Node-by-node timeline
- Inputs and outputs per node
- AI token usage
- Errors and retry attempts
- Approval decision history

Approval Inbox should show:

- Pending approvals
- Risk level
- AI recommendation
- Contract summary
- Approve / Reject buttons

Use realistic but fictional data. Do not use real company names or real private data.

The final design should be suitable for implementation with Next.js, Tailwind CSS, shadcn/ui, and React Flow.
```

---

## 23. Definition of Done for MVP

The MVP is done when:

- A developer can run the full platform locally.
- A user can upload a contract PDF.
- A predefined contract review workflow can execute.
- AI returns structured risk analysis.
- A high-risk contract creates a human approval request.
- User can approve or reject.
- Execution timeline is visible.
- Node execution history is persisted.
- AI usage is logged.
- README explains how to run the demo.
- Architecture docs explain how to add a new node.

---

## 24. Explicit Non-Goals for MVP

Do not include these in the MVP:

- Full marketplace
- Complex multi-agent system
- Full RAG platform
- Dozens of integrations
- Kubernetes deployment
- Enterprise SSO
- Billing
- Advanced analytics
- AI-generated workflows
- Public plugin ecosystem

These are future roadmap items.

---

## 25. Final Recommendation

Start with a narrow but impressive MVP: **Contract Review Workflow**.

The important goal is not to build every feature. The goal is to prove the architecture:

- Workflow as data
- Nodes as reusable components
- Engine-driven execution
- AI as structured workflow steps
- Human approval
- Full execution traceability

Once that foundation works, the platform can expand naturally into custom workflows, more nodes, integrations, AI agents, RAG, and marketplace-style extensibility.

