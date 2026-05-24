import { renderHook, act, waitFor } from '@testing-library/react';
import { useExecutionStream } from '../../hooks/useExecutionStream';

// ---------------------------------------------------------------------------
// Mock @microsoft/signalr
// ---------------------------------------------------------------------------
const mockOn = jest.fn();
const mockStart = jest.fn().mockResolvedValue(undefined);
const mockStop = jest.fn().mockResolvedValue(undefined);
const mockInvoke = jest.fn().mockResolvedValue(undefined);

const mockBuild = jest.fn(() => ({
  on: mockOn,
  start: mockStart,
  stop: mockStop,
  invoke: mockInvoke,
}));

const mockWithUrl = jest.fn().mockReturnThis();
const mockWithAutomaticReconnect = jest.fn().mockReturnThis();

jest.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: jest.fn().mockImplementation(() => ({
    withUrl: mockWithUrl,
    withAutomaticReconnect: mockWithAutomaticReconnect,
    build: mockBuild,
  })),
}));

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Retrieves the registered SignalR event handler by name.
 */
function getHandler(eventName: string): ((...args: unknown[]) => void) | undefined {
  const calls = (mockOn as jest.Mock).mock.calls;
  const found = calls.find(([name]) => name === eventName);
  return found?.[1];
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('useExecutionStream', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockStart.mockResolvedValue(undefined);
    mockStop.mockResolvedValue(undefined);
    mockInvoke.mockResolvedValue(undefined);
  });

  it('connects to the hub and joins the execution group on mount', async () => {
    const { result } = renderHook(() => useExecutionStream('exec-123'));

    await waitFor(() => {
      expect(mockStart).toHaveBeenCalledTimes(1);
    });

    expect(mockInvoke).toHaveBeenCalledWith('JoinExecution', 'exec-123');
    expect(result.current.events).toHaveLength(0);
  });

  it('accumulates NodeStarted events', async () => {
    const { result } = renderHook(() => useExecutionStream('exec-123'));

    await waitFor(() => expect(mockStart).toHaveBeenCalled());

    const handler = getHandler('NodeStarted');
    expect(handler).toBeDefined();

    act(() => {
      handler?.({ nodeId: 'ne-1', nodeType: 'ai.summarize' });
    });

    expect(result.current.events).toHaveLength(1);
    expect(result.current.events[0].type).toBe('NodeStarted');
    expect(result.current.events[0].nodeType).toBe('ai.summarize');
    expect(result.current.events[0].timestamp).toBeDefined();
  });

  it('accumulates multiple events in order', async () => {
    const { result } = renderHook(() => useExecutionStream('exec-abc'));

    await waitFor(() => expect(mockStart).toHaveBeenCalled());

    act(() => {
      getHandler('NodeStarted')?.({ nodeId: 'ne-1', nodeType: 'ai.summarize' });
      getHandler('NodeCompleted')?.({ nodeId: 'ne-1', nodeType: 'ai.summarize' });
      getHandler('ExecutionCompleted')?.({ status: 'Completed' });
    });

    expect(result.current.events).toHaveLength(3);
    expect(result.current.events[0].type).toBe('NodeStarted');
    expect(result.current.events[1].type).toBe('NodeCompleted');
    expect(result.current.events[2].type).toBe('ExecutionCompleted');
    expect(result.current.events[2].status).toBe('Completed');
  });

  it('accumulates NodeFailed events with error message', async () => {
    const { result } = renderHook(() => useExecutionStream('exec-fail'));

    await waitFor(() => expect(mockStart).toHaveBeenCalled());

    act(() => {
      getHandler('NodeFailed')?.({ nodeId: 'ne-2', nodeType: 'http.request', error: 'timeout' });
    });

    expect(result.current.events).toHaveLength(1);
    expect(result.current.events[0].type).toBe('NodeFailed');
    expect(result.current.events[0].error).toBe('timeout');
  });

  it('disconnects and leaves the group on unmount', async () => {
    const { unmount } = renderHook(() => useExecutionStream('exec-123'));

    await waitFor(() => expect(mockStart).toHaveBeenCalled());

    unmount();

    await waitFor(() => {
      expect(mockStop).toHaveBeenCalledTimes(1);
    });
  });
});
