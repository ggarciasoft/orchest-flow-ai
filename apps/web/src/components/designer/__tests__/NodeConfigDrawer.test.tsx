import React from 'react';
import { render, fireEvent } from '@testing-library/react';
import '@testing-library/jest-dom';
import { NodeConfigDrawer } from '../NodeConfigDrawer';

describe('NodeConfigDrawer', () => {
  const mockOnClose = jest.fn();
  const mockOnConfigChange = jest.fn();

  const defaultProps = {
    node: {
      data: {
        descriptor: { type: 'test', displayName: 'Test Node', description: 'A test node', configuration: [], inputs: [{ key: 'input1', displayName: 'Input 1', required: true }] },
        config: {}
      }
    },
    catalog: [{ type: 'test', displayName: 'Test Node', description: 'A test node', configuration: [] }],
    onClose: mockOnClose,
    onConfigChange: mockOnConfigChange,
  };

  afterEach(() => {
    jest.clearAllMocks();
  });

  test('renders with correct title and description', () => {
    const { getByText } = render(<NodeConfigDrawer {...defaultProps} />);

    expect(getByText('Test Node')).toBeInTheDocument();
    expect(getByText('A test node')).toBeInTheDocument();
  });

  test('calls onClose when close button is clicked', () => {
    const { getByRole } = render(<NodeConfigDrawer {...defaultProps} />);

    fireEvent.click(getByRole('button', { name: /close/i }));
    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });

  test('calls onConfigChange when a select value is changed', () => {
    const updatedProps = {
      ...defaultProps,
      node: {
        data: {
          descriptor: {
            type: 'test',
            displayName: 'Test Node',
            description: 'A test node',
            configuration: [{ key: 'setting1', displayName: 'Setting 1', required: true, allowedValues: ['A', 'B'], defaultValue: 'A' }], inputs: [{ key: 'input1', displayName: 'Input 1', required: true }]
              { key: 'setting1', displayName: 'Setting 1', required: true, allowedValues: ['A', 'B'], defaultValue: 'A' },
            ]
          },
          config: { setting1: 'A' },
        },
      },
    };

    const { getByLabelText } = render(<NodeConfigDrawer {...updatedProps} />);

    fireEvent.change(getByLabelText('Setting 1 *'), { target: { value: 'B' } });
    expect(mockOnConfigChange).toHaveBeenCalledWith({ setting1: 'B' });
  });
});