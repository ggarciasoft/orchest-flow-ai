import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import PresetsPage from '../(app)/settings/presets/page';

jest.mock('@/lib/api', () => ({
  api: {
    presets: {
      list: jest.fn().mockResolvedValue([]),
      create: jest.fn().mockResolvedValue({}),
      update: jest.fn().mockResolvedValue({}),
      delete: jest.fn().mockResolvedValue(undefined),
    },
  },
}));

jest.mock('lucide-react', () => ({
  Plus: () => <span>Plus</span>,
  Pencil: () => <span>Pencil</span>,
  Trash2: () => <span>Trash2</span>,
  ArrowLeft: () => <span>ArrowLeft</span>,
  Save: () => <span>Save</span>,
  X: () => <span>X</span>,
}));

jest.mock('next/link', () => ({
  __esModule: true,
  default: ({ children, href }: { children: React.ReactNode; href: string }) => <a href={href}>{children}</a>,
}));

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
    {children}
  </QueryClientProvider>
);

describe('PresetsPage', () => {
  it('renders without crashing', () => {
    render(<PresetsPage />, { wrapper });
    expect(document.body).toBeTruthy();
  });

  it('renders the page heading', async () => {
    render(<PresetsPage />, { wrapper });
    await waitFor(() => {
      expect(screen.getAllByText(/preset/i).length).toBeGreaterThan(0);
    });
  });
});
