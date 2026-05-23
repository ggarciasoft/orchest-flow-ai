import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import DashboardPage from "../(app)/dashboard/page";

jest.mock("@/lib/api", () => ({
  api: {
    workflows: { list: jest.fn().mockResolvedValue({ total: 5, items: [] }) },
    executions: { list: jest.fn().mockResolvedValue({ total: 3, items: [] }) },
    approvals: { list: jest.fn().mockResolvedValue({ total: 2, items: [] }) },
  },
}));

jest.mock("@/lib/utils", () => ({
  statusColor: jest.fn(() => "text-gray-500"),
  formatDate: jest.fn(() => "2024-01-01"),
}));

jest.mock("lucide-react", () => ({
  GitBranch: () => <span>GitBranch</span>,
  Play: () => <span>Play</span>,
  CheckSquare: () => <span>CheckSquare</span>,
  AlertCircle: () => <span>AlertCircle</span>,
  Clock: () => <span>Clock</span>,
  CheckCircle: () => <span>CheckCircle</span>,
  XCircle: () => <span>XCircle</span>,
}));

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
    {children}
  </QueryClientProvider>
);

describe("DashboardPage", () => {
  it("renders without crashing", () => {
    render(<DashboardPage />, { wrapper });
    expect(document.body).toBeTruthy();
  });

  it("renders stat cards", () => {
    render(<DashboardPage />, { wrapper });
    expect(screen.getByText("Total Workflows")).toBeInTheDocument();
    expect(screen.getAllByText(/Executions/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Pending Approvals/).length).toBeGreaterThan(0);
  });
});
