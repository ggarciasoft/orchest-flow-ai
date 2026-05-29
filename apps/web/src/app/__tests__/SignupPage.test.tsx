import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import SignupPage from "../signup/page";

const mockPush = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({ push: mockPush }),
}));

jest.mock("@/lib/api", () => ({
  api: {
    auth: {
      register: jest.fn(async (displayName: string, email: string, _password: string) => {
        if (email === "taken@example.com") {
          throw new Error("An account with that email already exists.");
        }
        return { token: "mock-token", user: { id: "u1", email, displayName, role: "Admin" } };
      }),
    },
  },
}));

jest.mock("@/lib/auth", () => ({
  setToken: jest.fn(),
}));

describe("SignupPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("renders all form fields", () => {
    render(<SignupPage />);
    expect(screen.getByLabelText(/full name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^email$/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^password/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /create account/i })).toBeInTheDocument();
  });

  it("shows a link back to the login page", () => {
    render(<SignupPage />);
    const link = screen.getByRole("link", { name: /sign in/i });
    expect(link).toBeInTheDocument();
    expect(link).toHaveAttribute("href", "/login");
  });

  it("handles successful registration and redirects to dashboard", async () => {
    const { setToken } = require("@/lib/auth");
    const { api } = require("@/lib/api");

    render(<SignupPage />);

    fireEvent.change(screen.getByLabelText(/full name/i), { target: { value: "Jane Smith" } });
    fireEvent.change(screen.getByLabelText(/^email$/i), { target: { value: "jane@example.com" } });
    fireEvent.change(screen.getByLabelText(/^password/i), { target: { value: "password123" } });
    fireEvent.change(screen.getByLabelText(/confirm password/i), { target: { value: "password123" } });
    fireEvent.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(api.auth.register).toHaveBeenCalledWith("Jane Smith", "jane@example.com", "password123");
      expect(setToken).toHaveBeenCalledWith("mock-token");
      expect(mockPush).toHaveBeenCalledWith("/dashboard");
    });
  });

  it("shows inline mismatch hint when passwords differ", () => {
    render(<SignupPage />);

    fireEvent.change(screen.getByLabelText(/^password/i), { target: { value: "password123" } });
    fireEvent.change(screen.getByLabelText(/confirm password/i), { target: { value: "different" } });

    expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument();
  });

  it("shows error and does not submit when password < 8 chars", async () => {
    const { api } = require("@/lib/api");

    render(<SignupPage />);

    fireEvent.change(screen.getByLabelText(/full name/i), { target: { value: "Jane" } });
    fireEvent.change(screen.getByLabelText(/^email$/i), { target: { value: "jane@example.com" } });
    fireEvent.change(screen.getByLabelText(/^password/i), { target: { value: "short" } });
    fireEvent.change(screen.getByLabelText(/confirm password/i), { target: { value: "short" } });
    fireEvent.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText(/at least 8 characters/i)).toBeInTheDocument();
    });
    expect(api.auth.register).not.toHaveBeenCalled();
  });

  it("displays API error for duplicate email", async () => {
    render(<SignupPage />);

    fireEvent.change(screen.getByLabelText(/full name/i), { target: { value: "Jane" } });
    fireEvent.change(screen.getByLabelText(/^email$/i), { target: { value: "taken@example.com" } });
    fireEvent.change(screen.getByLabelText(/^password/i), { target: { value: "password123" } });
    fireEvent.change(screen.getByLabelText(/confirm password/i), { target: { value: "password123" } });
    fireEvent.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText(/already exists/i)).toBeInTheDocument();
    });
  });
});
