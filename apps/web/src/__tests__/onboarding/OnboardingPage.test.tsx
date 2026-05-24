import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import OnboardingPage from '../../app/(app)/onboarding/page';

// Module-level mock — jest.mock is hoisted before imports
const mockCreate = jest.fn();
const mockInvite = jest.fn();

jest.mock('@/lib/api', () => ({
  api: {
    tenants: {
      create: (...args: unknown[]) => mockCreate(...args),
      invite: (...args: unknown[]) => mockInvite(...args),
      acceptInvite: jest.fn(),
    },
  },
}));

describe('OnboardingPage', () => {
  beforeEach(() => {
    mockCreate.mockReset();
    mockInvite.mockReset();
  });

  it('renders step 1 with workspace name input on initial load', () => {
    render(<OnboardingPage />);
    // The heading is visible in step 1
    expect(screen.getAllByText('Name your workspace').length).toBeGreaterThan(0);
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
    mockCreate.mockResolvedValueOnce({ id: 'tenant-123', name: 'Acme Inc.', createdAt: new Date().toISOString() });

    render(<OnboardingPage />);
    fireEvent.change(screen.getByLabelText('Workspace name'), { target: { value: 'Acme Inc.' } });
    fireEvent.click(screen.getByRole('button', { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByLabelText('Invitee email address')).toBeInTheDocument();
    });
  });

  it('shows error message when tenant creation fails', async () => {
    mockCreate.mockRejectedValueOnce(new Error('Server error'));

    render(<OnboardingPage />);
    fireEvent.change(screen.getByLabelText('Workspace name'), { target: { value: 'Test Org' } });
    fireEvent.click(screen.getByRole('button', { name: /continue/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Server error');
    });
  });
});
