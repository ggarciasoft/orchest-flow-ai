import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { WorkflowDesigner } from "../WorkflowDesigner";

const createWrapper = () => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={qc}>{children}</QueryClientProvider>
  );
};

jest.mock("../NodePalette", () => ({
  NodePalette: ({ onAdd }: { onAdd: (type: string) => void }) => (
    <div data-testid="node-palette">NodePalette</div>
  ),
}));

jest.mock("../NodeConfigDrawer", () => ({
  NodeConfigDrawer: () => <div data-testid="node-config-drawer">NodeConfigDrawer</div>,
}));

jest.mock("../VersionHistoryPanel", () => ({
  VersionHistoryPanel: () => <div data-testid="version-history-panel">VersionHistoryPanel</div>,
}));

jest.mock("../AiAssistPanel", () => ({
  AiAssistPanel: () => <div data-testid="ai-assist-panel">AiAssistPanel</div>,
}));

jest.mock("../../RunWorkflowModal", () => ({
  RunWorkflowModal: () => <div data-testid="run-workflow-modal">RunWorkflowModal</div>,
}));

jest.mock("@/contexts/AuthContext", () => ({
  useAuth: () => ({ canEdit: true }),
}));

jest.mock("@xyflow/react", () => ({
  ReactFlow: ({ children }: { children: React.ReactNode }) => <div data-testid="react-flow">{children}</div>,
  Background: () => null,
  Controls: () => null,
  MiniMap: () => null,
  addEdge: jest.fn(),
  useNodesState: () => [[], jest.fn(), jest.fn()],
  useEdgesState: () => [[], jest.fn(), jest.fn()],
}));

jest.mock("@xyflow/react/dist/style.css", () => ({}), { virtual: true });

jest.mock("@/lib/api", () => ({
  api: {
    workflows: {
      execute: jest.fn().mockResolvedValue({ executionId: "exec-1" }),
      update: jest.fn().mockResolvedValue({}),
    },
  },
}));

jest.mock("lucide-react", () => ({
  Save: () => <span>Save</span>,
  Play: () => <span>Play</span>,
  Undo2: () => <span>Undo</span>,
  Redo2: () => <span>Redo</span>,
  History: () => <span>History</span>,
  Sparkles: () => <span>Sparkles</span>,
  Zap: () => <span>Zap</span>,
  X: () => <span>X</span>,
  Check: () => <span>Check</span>,
  Pencil: () => <span>Pencil</span>,
  Send: () => <span>Send</span>,
  Loader2: () => <span>Loader2</span>,
  PlayCircle: () => <span>PlayCircle</span>,
  StopCircle: () => <span>StopCircle</span>,
  GitBranch: () => <span>GitBranch</span>,
  Timer: () => <span>Timer</span>,
  GitMerge: () => <span>GitMerge</span>,
  Shuffle: () => <span>Shuffle</span>,
  UserCheck: () => <span>UserCheck</span>,
  Brain: () => <span>Brain</span>,
  FileText: () => <span>FileText</span>,
  Globe: () => <span>Globe</span>,
  Mail: () => <span>Mail</span>,
  Webhook: () => <span>Webhook</span>,
  Clock: () => <span>Clock</span>,
  Repeat: () => <span>Repeat</span>,
  Database: () => <span>Database</span>,
  Code2: () => <span>Code2</span>,
  RefreshCw: () => <span>RefreshCw</span>,
  FileSearch: () => <span>FileSearch</span>,
  Download: () => <span>Download</span>,
  CheckSquare: () => <span>CheckSquare</span>,
  Cpu: () => <span>Cpu</span>,
  Layers: () => <span>Layers</span>,
}));

const mockWorkflow = {
  id: "wf-1",
  name: "Test Workflow",
  description: "A sample workflow",
  triggerType: "Manual" as const,
  cronExpression: null,
  webhookSecret: null,
  createdAt: "2024-01-01",
  updatedAt: "2024-01-02",
};

const mockNodeCatalog = [
  {
    type: "node1",
    displayName: "Node 1",
    description: "First Node",
    category: "ai",
    version: "1.0",
    iconKey: "",
    inputs: [],
    outputs: [],
    configuration: [],
  },
];

describe("WorkflowDesigner", () => {
  it("renders without crashing", () => {
    render(<WorkflowDesigner workflow={mockWorkflow} nodeCatalog={mockNodeCatalog} />, { wrapper: createWrapper() });
    expect(document.body).toBeTruthy();
  });

  it("shows the workflow name", () => {
    render(<WorkflowDesigner workflow={mockWorkflow} nodeCatalog={mockNodeCatalog} />, { wrapper: createWrapper() });
    expect(screen.getByText("Test Workflow")).toBeInTheDocument();
  });

  it("renders the react flow canvas", () => {
    render(<WorkflowDesigner workflow={mockWorkflow} nodeCatalog={mockNodeCatalog} />, { wrapper: createWrapper() });
    expect(screen.getByTestId("react-flow")).toBeInTheDocument();
  });

  it("renders AI assistant button", () => {
    render(<WorkflowDesigner workflow={mockWorkflow} nodeCatalog={mockNodeCatalog} />, { wrapper: createWrapper() });
    expect(screen.getByTitle("AI Workflow Assistant")).toBeInTheDocument();
  });
});
