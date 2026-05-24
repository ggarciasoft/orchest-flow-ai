import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import OnboardingPage from '../../app/(app)/onboarding/page';

// Mock the api module
jest.mock('@/lib/api', () => ({
  api: {
    tenants: {
      create: jest.fn(),
      invite: jest.fn(),
      acceptInvite: jest.fn(),
    },
  },
}));

// eslint-disable-next-line @typescript-eslint/no-require-imports
const { api } = require('@/lib/api');

describe('OnboardingPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders step 1 with workspace name input on initial load', () => {
    render(<OnboardingPage />);
    expect(screen.getByText('Name your workspace')).toBeInTheDocument();
    expect(screen.getByLabelText('Workspace name')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /continue/i })).toBeInTheDocument();
  });

  it('shows all three step indicators', () => {
    render(<OnboardingPage />);
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
  });

  it('shows validation error when workspace name is empty', async () => {
    render(<OnboardingPage />);
    fireEvent.click(screen.getByRole('button', { name: /continue/i }));
    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
      expect(screen.getByText('Workspace name is required.')).toBeInTheDocument();
    });
  });

  it('proceeds to step 2 after successful tenant creation', async () => {
    api.tenants.create.mockResolvedValueOnce({ id: 'tenant-123', name: 'Acme Inc.', createdAt: new Date().toISOString() });

    render(<OnboardingPage />);
    fireEvent.change(screen.getByLabelText('Workspace name'), { target: { value: 'Acme Inc.' } });
    fireEvent.click(screen.getByRole('button', { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByText('Invite your team')).toBeInTheDocument();
      expect(screen.getByLabelText('Invitee email address')).toBeInTheDocument();
    });
  });

  it('shows error message when tenant creation fails', async () => {
    api.tenants.create.mockRejectedValueOnce(new Error('Server error'));

    render(<OnboardingPage />);
    fireEvent.change(screen.getByLabelText('Workspace name'), { target: { value: 'Test Org' } });
    fireEvent.click(screen.getByRole('button', { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Server error');
    });
  });
});
