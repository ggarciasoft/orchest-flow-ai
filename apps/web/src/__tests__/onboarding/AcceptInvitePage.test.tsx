import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import AcceptInvitePage from '../../app/invite/[token]/page';

const mockPush = jest.fn();

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: mockPush }),
  useParams: () => ({ token: 'tenant-abc' }),
  useSearchParams: () => ({
    get: (key: string) => (key === 'token' ? 'valid-token-123' : null),
  }),
}));

jest.mock('@/lib/api', () => ({
  api: {
    tenants: {
      acceptInvite: jest.fn(),
    },
  },
}));

// eslint-disable-next-line @typescript-eslint/no-require-imports
const { api } = require('@/lib/api');

describe('AcceptInvitePage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders the password form', () => {
    render(<AcceptInvitePage />);
    expect(screen.getByText('Join your team on OrchestAI')).toBeInTheDocument();
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
    expect(screen.getByLabelText('Confirm password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /create account/i })).toBeInTheDocument();
  });

  it('shows error when password is empty', async () => {
    render(<AcceptInvitePage />);
    fireEvent.click(screen.getByRole('button', { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Password is required.');
    });
  });

  it('shows error when passwords do not match', async () => {
    render(<AcceptInvitePage />);
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'password123' } });
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: 'different456' } });
    fireEvent.click(screen.getByRole('button', { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Passwords do not match.');
    });
  });

  it('shows error when password is too short', async () => {
    render(<AcceptInvitePage />);
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'short' } });
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: 'short' } });
    fireEvent.click(screen.getByRole('button', { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('at least 8 characters');
    });
  });

  it('shows success state after account creation', async () => {
    api.tenants.acceptInvite.mockResolvedValueOnce({ message: 'Account created. You may now log in.' });

    render(<AcceptInvitePage />);
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'securepassword' } });
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: 'securepassword' } });
    fireEvent.click(screen.getByRole('button', { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText('Account created!')).toBeInTheDocument();
    });
  });

  it('shows error when api call fails', async () => {
    api.tenants.acceptInvite.mockRejectedValueOnce(new Error('Token expired'));

    render(<AcceptInvitePage />);
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'securepassword' } });
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: 'securepassword' } });
    fireEvent.click(screen.getByRole('button', { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Token expired');
    });
  });
});
