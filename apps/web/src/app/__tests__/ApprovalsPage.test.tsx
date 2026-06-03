import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import ApprovalsPage from "../(app)/approvals/page";

jest.mock("@/lib/api", () => ({
  api: {
    approvals: {
      list: jest.fn().mockResolvedValue({ total: 0, items: [] }),
      approve: jest.fn().mockResolvedValue({}),
      reject: jest.fn().mockResolvedValue({}),
    },
  },
}));

jest.mock("@/lib/utils", () => ({
  formatDate: jest.fn(() => "2024-01-01"),
}));

jest.mock("lucide-react", () => ({
  CheckCircle: () => <span>CheckCircle</span>,
  XCircle: () => <span>XCircle</span>,
}));

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
    {children}
  </QueryClientProvider>
);

describe("ApprovalsPage", () => {
  it("renders without crashing", () => {
    render(<ApprovalsPage />, { wrapper });
    expect(document.body).toBeTruthy();
  });

  it("renders the page heading", () => {
    render(<ApprovalsPage />, { wrapper });
    expect(screen.getByText("Task Inbox")).toBeInTheDocument();
  });
});
