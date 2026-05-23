import { render, screen } from "@testing-library/react";
import NodePalette from "../NodePalette";

describe("NodePalette Component", () => {
  it("renders without crashing", () => {
    render(<NodePalette />);
    const paletteElement = screen.getByTestId("node-palette");
    expect(paletteElement).toBeInTheDocument();
  });

  it("displays correct title", () => {
    render(<NodePalette />);
    const titleElement = screen.getByText(/Node Palette/i);
    expect(titleElement).toBeInTheDocument();
  });
});