import { render, screen } from "@testing-library/react";
import { WorkflowDesigner } from "../WorkflowDesigner";

jest.mock("../NodePalette", () => ({
  NodePalette: ({ onAdd }: { onAdd: (type: string) => void }) => (
    <div data-testid="node-palette">NodePalette</div>
  ),
}));

jest.mock("../NodeConfigDrawer", () => ({
  NodeConfigDrawer: () => <div data-testid="node-config-drawer">NodeConfigDrawer</div>,
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
    },
  },
}));

jest.mock("lucide-react", () => ({
  Save: () => <span>Save</span>,
  Play: () => <span>Play</span>,
  Undo2: () => <span>Undo</span>,
  Redo2: () => <span>Redo</span>,
}));

const mockWorkflow = {
  id: "wf-1",
  name: "Test Workflow",
  description: "A sample workflow",
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
    render(<WorkflowDesigner workflow={mockWorkflow} nodeCatalog={mockNodeCatalog} />);
    expect(document.body).toBeTruthy();
  });

  it("shows the workflow name", () => {
    render(<WorkflowDesigner workflow={mockWorkflow} nodeCatalog={mockNodeCatalog} />);
    expect(screen.getByText("Test Workflow")).toBeInTheDocument();
  });

  it("renders the react flow canvas", () => {
    render(<WorkflowDesigner workflow={mockWorkflow} nodeCatalog={mockNodeCatalog} />);
    expect(screen.getByTestId("react-flow")).toBeInTheDocument();
  });
});
