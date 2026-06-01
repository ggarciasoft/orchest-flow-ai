import { render, screen, waitFor } from "@testing-library/react";
import DocumentsPage from "../(app)/documents/page";

jest.mock("@/lib/api", () => ({
  api: {
    documents: {
      list: jest.fn().mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 20 }),
      upload: jest.fn().mockResolvedValue({ id: "doc-1" }),
    },
  },
  PagedResponse: {},
  DocumentMeta: {},
}));

jest.mock("lucide-react", () => ({
  FileText: () => <span>FileText</span>,
  Upload: () => <span>Upload</span>,
  Search: () => <span>Search</span>,
  ChevronLeft: () => <span>ChevronLeft</span>,
  ChevronRight: () => <span>ChevronRight</span>,
  Loader2: () => <span>Loader2</span>,
}));

jest.mock("@/components/ui", () => ({
  PageHeader: ({ title }: { title: string }) => <h1>{title}</h1>,
  EmptyState: ({ title }: { title: string }) => <div>{title}</div>,
}));

describe("DocumentsPage", () => {
  it("renders without crashing", async () => {
    render(<DocumentsPage />);
    expect(document.body).toBeTruthy();
  });

  it("renders the Documents heading", async () => {
    render(<DocumentsPage />);
    expect(screen.getByText("Documents")).toBeInTheDocument();
  });

  it("shows empty state when no documents", async () => {
    render(<DocumentsPage />);
    await waitFor(() => {
      expect(screen.getByText("No documents yet")).toBeInTheDocument();
    });
  });
});
