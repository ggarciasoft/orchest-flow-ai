import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import ApprovalDetailPage from '../(app)/approvals/[id]/page';

const mockPush = jest.fn();

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: mockPush }),
  useParams: () => ({ id: 'test-approval-id' }),
}));

jest.mock('@/app/(app)/approvals/_components/ApprovalCommentThread', () => ({
  __esModule: true,
  default: () => <div data-testid="comment-thread" />,
}));

jest.mock('next/link', () => ({
  __esModule: true,
  default: ({ children, href }: { children: React.ReactNode; href: string }) => <a href={href}>{children}</a>,
}));

jest.mock('@/lib/api', () => ({
  api: {
    approvals: {
      get: jest.fn().mockResolvedValue({
        id: 'test-approval-id',
        status: 'Pending',
        payloadJson: JSON.stringify({ _approvalTitle: 'Review Contract', documentId: 'doc-1' }),
        requestedAt: '2026-05-23T10:00:00Z',
        workflowExecutionId: 'exec-1',
      }),
      approve: jest.fn().mockResolvedValue({}),
      reject: jest.fn().mockResolvedValue({}),
    },
    executions: {
      timeline: jest.fn().mockResolvedValue({ executionId: 'exec-1', nodes: [] }),
    },
  },
}));

jest.mock('@/lib/utils', () => ({
  formatDate: jest.fn(() => '2026-05-23'),
  statusColor: jest.fn(() => 'bg-gray-100 text-gray-600'),
  cn: (...args: (string | undefined | false | null)[]) => args.filter(Boolean).join(' '),
  statusVariant: jest.fn(() => 'default'),
}));

jest.mock('lucide-react', () => ({
  CheckCircle: () => <span>CheckCircle</span>,
  XCircle: () => <span>XCircle</span>,
  ArrowLeft: () => <span>ArrowLeft</span>,
  Clock: () => <span>Clock</span>,
}));

const wrapper = ({ children }: { children: React.ReactNode }) => (
  <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
    {children}
  </QueryClientProvider>
);

describe('ApprovalDetailPage', () => {
  beforeEach(() => jest.clearAllMocks());

  it('renders without crashing', () => {
    render(<ApprovalDetailPage />, { wrapper });
    expect(document.body).toBeTruthy();
  });

  it('renders back link', async () => {
    render(<ApprovalDetailPage />, { wrapper });
    await waitFor(() => {
      expect(screen.getByText('Back to Approval Inbox')).toBeInTheDocument();
    });
  });
});
