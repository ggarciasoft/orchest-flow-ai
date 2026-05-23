import { render, screen } from "@testing-library/react";
import DocumentsPage from "../(app)/documents/page";

jest.mock("@/lib/api", () => ({
  api: {
    documents: {
      upload: jest.fn().mockResolvedValue({ id: "doc-1" }),
    },
  },
}));

jest.mock("lucide-react", () => ({
  FileText: () => <span>FileText</span>,
  Upload: () => <span>Upload</span>,
  CheckCircle: () => <span>CheckCircle</span>,
}));

describe("DocumentsPage", () => {
  it("renders without crashing", () => {
    render(<DocumentsPage />);
    expect(document.body).toBeTruthy();
  });

  it("renders the Documents heading", () => {
    render(<DocumentsPage />);
    expect(screen.getByText("Documents")).toBeInTheDocument();
  });
});
