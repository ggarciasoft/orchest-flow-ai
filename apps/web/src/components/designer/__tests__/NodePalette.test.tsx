import { render, screen } from "@testing-library/react";
import { NodePalette } from "../NodePalette";

describe("NodePalette Component", () => {
  it("renders without crashing", () => {
    render(<NodePalette catalog={[]} onAddNode={() => {}} />);
    const paletteElement = screen.getByText(/Node Palette/i);
    expect(paletteElement).toBeInTheDocument();
  });

  it("displays correct title", () => {
    render(<NodePalette catalog={[]} onAddNode={() => {}} />);
    const titleElement = screen.getByText(/Node Palette/i);
    expect(titleElement).toBeInTheDocument();
  });

  it("handles empty catalog gracefully", () => {
    render(<NodePalette catalog={[]} onAddNode={() => {}} />);
    const emptyMessage = screen.getByText("No nodes available.");
    expect(emptyMessage).toBeInTheDocument();
  });
});