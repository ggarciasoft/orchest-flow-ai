import React from "react";
import { render, fireEvent, screen } from "@testing-library/react";
import "@testing-library/jest-dom";
import { NodeConfigDrawer } from "../NodeConfigDrawer";

jest.mock("@xyflow/react", () => ({}));
jest.mock("lucide-react", () => ({
  X: () => <span>X</span>,
}));

describe("NodeConfigDrawer", () => {
  const mockOnClose = jest.fn();
  const mockOnConfigChange = jest.fn();

  const defaultProps = {
    node: {
      id: "node1",
      position: { x: 0, y: 0 },
      data: {
        descriptor: {
          type: "test",
          displayName: "Test Node",
          description: "A test node",
          configuration: [
            {
              key: "setting1",
              displayName: "Setting 1",
              description: "Choose an option",
              required: true,
              allowedValues: ["Option A", "Option B"],
              defaultValue: "Option A",
            },
          ],
        },
        config: {},
      },
    },
    catalog: [
      {
        type: "test",
        displayName: "Test Node",
        description: "A test node",
        category: "ai",
        version: "1.0",
        iconKey: "",
        inputs: [],
        outputs: [],
        configuration: [
          {
            key: "setting1",
            displayName: "Setting 1",
            description: "Choose an option",
            required: true,
            allowedValues: ["Option A", "Option B"],
          },
        ],
      },
    ],
    onClose: mockOnClose,
    onConfigChange: mockOnConfigChange,
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("renders the drawer with node title", () => {
    render(<NodeConfigDrawer {...defaultProps} />);
    expect(screen.getByText("Test Node")).toBeInTheDocument();
  });

  it("renders the description", () => {
    render(<NodeConfigDrawer {...defaultProps} />);
    expect(screen.getByText("A test node")).toBeInTheDocument();
  });

  it("calls onClose when the close button is clicked", () => {
    render(<NodeConfigDrawer {...defaultProps} />);
    const closeButton = screen.getByRole("button");
    fireEvent.click(closeButton);
    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });

  it("renders configuration fields", () => {
    render(<NodeConfigDrawer {...defaultProps} />);
    expect(screen.getByText(/Setting 1/)).toBeInTheDocument();
  });
});
