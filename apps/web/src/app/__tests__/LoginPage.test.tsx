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
        if (email === "user@example.com" && password === "password") {
          return { token: "mock-token" };
        }
        throw new Error("Invalid credentials");
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
    expect(screen.getByText("Sign In")).toBeInTheDocument();
  });

  it("handles successful login", async () => {
    const { setToken } = require("@/lib/auth");
    const { api } = require("@/lib/api");

    render(<LoginPage />);

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "user@example.com" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "password" } });
    fireEvent.click(screen.getByText("Sign In"));

    await waitFor(() => {
      expect(api.auth.login).toHaveBeenCalledWith("user@example.com", "password");
      expect(setToken).toHaveBeenCalledWith("mock-token");
      expect(mockPush).toHaveBeenCalledWith("/dashboard");
    });
  });

  it("displays an error for invalid credentials", async () => {
    render(<LoginPage />);

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "invalid@example.com" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "wrong-password" } });
    fireEvent.click(screen.getByText("Sign In"));

    await waitFor(() => {
      expect(screen.getByText("Invalid credentials")).toBeInTheDocument();
    });
  });
});
