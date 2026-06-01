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
      invitePreview: jest.fn().mockResolvedValue({
        email: 'user@example.com',
        tenantName: 'Acme Corp',
        role: 'Member',
        expiresAt: '2099-01-01T00:00:00Z',
      }),
      acceptInvite: jest.fn(),
    },
  },
}));

jest.mock('@/lib/auth', () => ({
  setToken: jest.fn(),
}));

// eslint-disable-next-line @typescript-eslint/no-require-imports
const { api } = require('@/lib/api');

/** Wait for the invite preview to resolve and the form to appear. */
async function renderAndWaitForForm() {
  render(<AcceptInvitePage />);
  await waitFor(() => screen.getByLabelText('Password'));
}

describe('AcceptInvitePage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the password form', async () => {
    await renderAndWaitForForm();
    expect(screen.getByText('Create your account')).toBeInTheDocument();
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
    expect(screen.getByLabelText('Confirm password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /create account/i })).toBeInTheDocument();
  });

  it('shows error when password is empty', async () => {
    await renderAndWaitForForm();
    const form = document.querySelector('form')!;
    fireEvent.submit(form);
    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Password is required.');
    });
  });

  it('shows error when passwords do not match', async () => {
    await renderAndWaitForForm();
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'password123' } });
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: 'different456' } });
    fireEvent.click(screen.getByRole('button', { name: /create account/i }));
    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Passwords do not match.');
    });
  });

  it('shows error when password is too short', async () => {
    await renderAndWaitForForm();
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'short' } });
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: 'short' } });
    fireEvent.click(screen.getByRole('button', { name: /create account/i }));
    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('at least 8 characters');
    });
  });

  it('shows success state after account creation', async () => {
    api.tenants.acceptInvite.mockResolvedValueOnce({
      token: 'jwt-token',
      user: { id: 'u1', email: 'user@example.com', displayName: 'User', role: 'Member' },
    });
    await renderAndWaitForForm();
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'securepassword' } });
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: 'securepassword' } });
    fireEvent.click(screen.getByRole('button', { name: /create account/i }));
    await waitFor(() => {
      expect(screen.getByText(/Welcome to/i)).toBeInTheDocument();
    });
  });

  it('shows error when api call fails', async () => {
    api.tenants.acceptInvite.mockRejectedValueOnce(new Error('Token expired'));
    await renderAndWaitForForm();
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'securepassword' } });
    fireEvent.change(screen.getByLabelText('Confirm password'), { target: { value: 'securepassword' } });
    fireEvent.click(screen.getByRole('button', { name: /create account/i }));
    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Token expired');
    });
  });
});
