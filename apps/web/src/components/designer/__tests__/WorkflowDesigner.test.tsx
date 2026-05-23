import { render, screen } from "@testing-library/react";
import WorkflowDesigner from "../WorkflowDesigner";

describe("WorkflowDesigner Component", () => {
  it("renders without crashing", () => {
    render(<WorkflowDesigner />);
    const designerElement = screen.getByTestId("workflow-designer");
    expect(designerElement).toBeInTheDocument();
  });

  it("includes expected content", () => {
    render(<WorkflowDesigner />);
    const headerElement = screen.getByText(/Workflow Designer/i);
    expect(headerElement).toBeInTheDocument();
  });
});