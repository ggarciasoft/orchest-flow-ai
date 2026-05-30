import { render, screen } from "@testing-library/react";
import FormRenderer from "@/app/(app)/forms/_components/FormRenderer";
import type { FormFieldDefinition } from "@/lib/api";

// Mock the Documents API so file upload tests don't hit the network
jest.mock("@/lib/api", () => ({
  api: {
    documents: {
      upload: jest.fn().mockResolvedValue({
        id: "doc-1",
        filename: "test.pdf",
        mimeType: "application/pdf",
        sizeBytes: 1024,
        sha256: "abc",
      }),
    },
  },
}));

const fields: FormFieldDefinition[] = [
  { key: "name",     label: "Full Name",  type: "text",    required: true },
  { key: "age",      label: "Age",        type: "number" },
  { key: "dob",      label: "Birthdate",  type: "date" },
  { key: "email",    label: "Email",      type: "email" },
  { key: "agree",    label: "I agree",    type: "boolean" },
  { key: "category", label: "Category",   type: "select",  options: ["A", "B", "C"] },
  { key: "resume",   label: "Resume PDF", type: "file",    accept: ".pdf", required: true },
];

describe("FormRenderer", () => {
  it("renders form name and description", () => {
    render(
      <FormRenderer
        name="Test Form"
        description="A test description"
        fields={[]}
      />
    );
    expect(screen.getByText("Test Form")).toBeInTheDocument();
    expect(screen.getByText("A test description")).toBeInTheDocument();
  });

  it("shows empty state when no fields", () => {
    render(<FormRenderer name="Empty" fields={[]} />);
    expect(screen.getByText(/no fields/i)).toBeInTheDocument();
  });

  it("renders all field types", () => {
    render(<FormRenderer name="F" fields={fields} />);
    // Inputs are present — labels are siblings, not linked via for/id
    const inputs = document.querySelectorAll('input, select');
    expect(inputs.length).toBeGreaterThanOrEqual(fields.length - 1); // file input is sr-only + label input
    // Spot-check specific labels are visible
    expect(screen.getByText('Full Name')).toBeInTheDocument();
    expect(screen.getByText('Age')).toBeInTheDocument();
    expect(screen.getByText('Birthdate')).toBeInTheDocument();
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByRole('combobox')).toBeInTheDocument(); // select
  });

  it("marks required fields with asterisk", () => {
    render(<FormRenderer name="F" fields={fields} />);
    // Required fields have a * in their label area
    const labels = screen.getAllByText("*");
    expect(labels.length).toBeGreaterThanOrEqual(2); // name + resume
  });

  it("renders select options", () => {
    render(<FormRenderer name="F" fields={fields} />);
    expect(screen.getByRole("option", { name: "A" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "B" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "C" })).toBeInTheDocument();
  });

  it("renders file field with upload label", () => {
    render(<FormRenderer name="F" fields={fields} />);
    expect(screen.getByText(/choose a file/i)).toBeInTheDocument();
  });

  it("renders file field in preview mode as static placeholder", () => {
    const fileField: FormFieldDefinition = {
      key: "doc", label: "Document", type: "file",
    };
    render(<FormRenderer name="F" fields={[fileField]} preview />);
    expect(screen.getByText(/file upload/i)).toBeInTheDocument();
  });

  it("displays a per-field error message", () => {
    render(
      <FormRenderer
        name="F"
        fields={[{ key: "name", label: "Name", type: "text" }]}
        fieldErrors={{ name: "This field is required" }}
      />
    );
    expect(screen.getByText("This field is required")).toBeInTheDocument();
  });

  it("boolean field renders a checkbox", () => {
    const boolField: FormFieldDefinition = { key: "agree", label: "I agree", type: "boolean" };
    render(<FormRenderer name="F" fields={[boolField]} />);
    expect(screen.getByRole("checkbox")).toBeInTheDocument();
  });
});
