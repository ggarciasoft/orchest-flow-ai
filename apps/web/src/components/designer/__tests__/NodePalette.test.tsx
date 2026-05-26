import { render, screen, fireEvent } from "@testing-library/react";
import { NodePalette } from "../NodePalette";
import type { NodeDescriptor } from "@/lib/api";

const makeNode = (type: string, category: string, displayName: string): NodeDescriptor => ({
  type, category, displayName,
  description: `${displayName} description`,
  version: '1.0.0',
  inputs: [], outputs: [], configuration: [],
});

describe("NodePalette Component", () => {
  it("renders without crashing", () => {
    render(<NodePalette catalog={[]} onAddNode={() => {}} />);
    expect(screen.getByText(/Node Palette/i)).toBeInTheDocument();
  });

  it("displays correct title", () => {
    render(<NodePalette catalog={[]} onAddNode={() => {}} />);
    expect(screen.getByText(/Node Palette/i)).toBeInTheDocument();
  });

  it("handles empty catalog gracefully", () => {
    render(<NodePalette catalog={[]} onAddNode={() => {}} />);
    expect(screen.getByText("No nodes available.")).toBeInTheDocument();
  });

  it("renders data category nodes when expanded", () => {
    const catalog = [
      makeNode('data.db-query', 'data', 'Database Query'),
      makeNode('data.db-execute', 'data', 'Database Execute'),
    ];
    render(<NodePalette catalog={catalog} onAddNode={() => {}} />);
    // data category is collapsed by default — click to expand
    fireEvent.click(screen.getByText('data'));
    expect(screen.getByText('Database Query')).toBeInTheDocument();
    expect(screen.getByText('Database Execute')).toBeInTheDocument();
  });

  it("renders unknown categories not in CATEGORY_ORDER when expanded", () => {
    const catalog = [makeNode('custom.foo', 'custom', 'Custom Node')];
    render(<NodePalette catalog={catalog} onAddNode={() => {}} />);
    fireEvent.click(screen.getByText('custom'));
    expect(screen.getByText('Custom Node')).toBeInTheDocument();
  });

  it("calls onAddNode when a node is clicked", () => {
    const onAddNode = jest.fn();
    const descriptor = makeNode('system.start', 'system', 'Start');
    render(<NodePalette catalog={[descriptor]} onAddNode={onAddNode} />);
    fireEvent.click(screen.getByText('Start'));
    expect(onAddNode).toHaveBeenCalledWith(descriptor);
  });
});