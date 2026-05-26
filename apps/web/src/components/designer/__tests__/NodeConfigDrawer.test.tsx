import { render, screen, waitFor } from '@testing-library/react';
import { NodeConfigDrawer } from '../NodeConfigDrawer';

jest.mock('@/lib/api', () => ({
  api: {
    presets: {
      list: jest.fn().mockResolvedValue([
        { id: 'preset1', name: 'Example Preset', nodeType: 'integrations.http', configJson: '{"url":"https://example.com"}', createdAt: '2026-05-23T12:00:00Z' }
      ]),
    },
  },
}));

jest.mock('lucide-react', () => ({
  X: () => <span>X</span>,
  Trash2: () => <span data-testid="trash-icon" />,
  Plus: () => <span>Plus</span>,
  Save: () => <span>Save</span>,
  ChevronDown: () => <span>ChevronDown</span>,
}));

const httpDescriptor = {
  type: 'integrations.http',
  displayName: 'HTTP Request',
  description: 'Calls any REST API endpoint.',
  category: 'integrations',
  version: '1.0.0',
  iconKey: 'globe',
  inputs: [],
  outputs: [],
  configuration: [
    { key: 'url', displayName: 'URL', description: 'Target URL', required: true },
    { key: 'authType', displayName: 'Auth Type', description: 'Authentication type', required: false, allowedValues: ['none', 'bearer', 'basic', 'api-key', 'oauth2-client-credentials'] },
  ],
};

const httpNode = {
  id: 'node-http-1',
  position: { x: 0, y: 0 },
  data: {
    descriptor: httpDescriptor,
    config: {},
  },
};

const defaultProps = {
  node: httpNode,
  catalog: [httpDescriptor],
  onClose: jest.fn(),
  onDelete: jest.fn(),
  onConfigChange: jest.fn(),
};

describe('NodeConfigDrawer', () => {
  beforeEach(() => jest.clearAllMocks());

  it('renders the drawer with node title', () => {
    render(<NodeConfigDrawer {...defaultProps} />);
    expect(screen.getByText('HTTP Request')).toBeInTheDocument();
  });

  it('calls onClose when the close button is clicked', () => {
    render(<NodeConfigDrawer {...defaultProps} />);
    const closeButton = screen.getByTitle('Close');
    closeButton.click();
    expect(defaultProps.onClose).toHaveBeenCalledTimes(1);
  });

  it('calls onDelete when the delete button is clicked', () => {
    render(<NodeConfigDrawer {...defaultProps} />);
    const deleteButton = screen.getByText('Delete Node');
    deleteButton.click();
    expect(defaultProps.onDelete).toHaveBeenCalledTimes(1);
  });

  it('renders configuration fields', () => {
    render(<NodeConfigDrawer {...defaultProps} />);
    expect(screen.getAllByText(/URL/).length).toBeGreaterThan(0);
  });

  it('shows auth type field for HTTP nodes', () => {
    render(<NodeConfigDrawer {...defaultProps} />);
    // Auth Type field comes from the descriptor configuration — no separate heading
    expect(screen.getByText('Auth Type')).toBeInTheDocument();
  });

  it('renders preset selector', async () => {
    render(<NodeConfigDrawer {...defaultProps} />);
    await waitFor(() => {
      expect(screen.getByText('Use Preset')).toBeInTheDocument();
    });
  });
});
