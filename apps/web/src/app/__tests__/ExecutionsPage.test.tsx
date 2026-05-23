import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import ExecutionsPage from "../(app)/executions/page";

jest.mock("@/lib/api", () => ({
  api: {
    executions: {
      list: jest.fn().mockResolvedValue({ total: 0, items: [] }),
    },
  },
}));

jest.mock("@/lib/utils", () => ({
  statusColor: jest.fn(() => "text-gray-500"),
  formatDate: jest.fn(() => "2024-01-01"),
}));

jest.mock("next/link", () => ({
  __esModule: true,
  default: ({ children, href }: { children: React.ReactNode; href: string }) => <a href={href}>{children}</a>,
}));

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
    {children}
  </QueryClientProvider>
);

describe("ExecutionsPage", () => {
  it("renders without crashing", () => {
    render(<ExecutionsPage />, { wrapper });
    expect(document.body).toBeTruthy();
  });

  it("renders the Executions heading", () => {
    render(<ExecutionsPage />, { wrapper });
    expect(screen.getByText("Executions")).toBeInTheDocument();
  });
});
