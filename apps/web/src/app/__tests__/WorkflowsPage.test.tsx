import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import WorkflowsPage from "../(app)/workflows/page";

jest.mock("@/lib/api", () => ({
  api: {
    workflows: {
      list: jest.fn().mockResolvedValue({ total: 0, items: [] }),
    },
  },
}));

jest.mock("@/lib/utils", () => ({
  formatDate: jest.fn(() => "2024-01-01"),
}));

jest.mock("next/link", () => ({
  __esModule: true,
  default: ({ children, href }: { children: React.ReactNode; href: string }) => <a href={href}>{children}</a>,
}));

jest.mock("lucide-react", () => ({
  Plus: () => <span>Plus</span>,
  Search: () => <span>Search</span>,
  GitBranch: () => <span>GitBranch</span>,
  Play: () => <span>Play</span>,
}));

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
    {children}
  </QueryClientProvider>
);

describe("WorkflowsPage", () => {
  it("renders without crashing", () => {
    render(<WorkflowsPage />, { wrapper });
    expect(document.body).toBeTruthy();
  });

  it("renders the page heading", () => {
    render(<WorkflowsPage />, { wrapper });
    expect(screen.getByText("Workflows")).toBeInTheDocument();
  });
});
