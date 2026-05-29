import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import LoginPage from "../login/page";

const mockPush = jest.fn();

jest.mock("next/navigation", () => ({
  useRouter: () => ({ push: mockPush }),
}));

jest.mock("@/lib/api", () => ({
  api: {
    auth: {
      login: jest.fn(async (email: string, password: string) => {
        if (email === "user@example.com" && password === "password123") {
          return { token: "mock-token" };
        }
        throw new Error("Invalid email or password.");
      }),
    },
  },
}));

jest.mock("@/lib/auth", () => ({
  setToken: jest.fn(),
}));

describe("LoginPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("renders the login form", () => {
    render(<LoginPage />);
    expect(screen.getByLabelText("Email")).toBeInTheDocument();
    expect(screen.getByLabelText("Password")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign in/i })).toBeInTheDocument();
  });

  it("shows a link to the sign-up page", () => {
    render(<LoginPage />);
    const link = screen.getByRole("link", { name: /create one/i });
    expect(link).toBeInTheDocument();
    expect(link).toHaveAttribute("href", "/signup");
  });

  it("handles successful login", async () => {
    const { setToken } = require("@/lib/auth");
    const { api } = require("@/lib/api");

    render(<LoginPage />);

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "user@example.com" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "password123" } });
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(api.auth.login).toHaveBeenCalledWith("user@example.com", "password123");
      expect(setToken).toHaveBeenCalledWith("mock-token");
      expect(mockPush).toHaveBeenCalledWith("/dashboard");
    });
  });

  it("displays an error for invalid credentials", async () => {
    render(<LoginPage />);

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "invalid@example.com" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "wrong-password" } });
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText("Invalid email or password.")).toBeInTheDocument();
    });
  });
});
