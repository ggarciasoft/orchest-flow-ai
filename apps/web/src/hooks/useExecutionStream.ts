import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';

/**
 * Represents a single real-time execution lifecycle event received over SignalR.
 */
export interface ExecutionEvent {
  /** The type of the event: 'NodeStarted' | 'NodeCompleted' | 'NodeFailed' | 'ExecutionCompleted' */
  type: 'NodeStarted' | 'NodeCompleted' | 'NodeFailed' | 'ExecutionCompleted';
  /** The node execution identifier (for node-level events). */
  nodeId?: string;
  /** The node type string (e.g. ai.summarize). */
  nodeType?: string;
  /** Error message, set only on NodeFailed events. */
  error?: string;
  /** Final status, set only on ExecutionCompleted events. */
  status?: string;
  /** ISO timestamp when this event was received on the client. */
  timestamp: string;
}

/**
 * useExecutionStream — subscribes to real-time execution lifecycle events via SignalR.
 *
 * Connects to `/hubs/execution`, joins the execution group, and accumulates incoming events.
 * Cleans up the connection when the component unmounts or `executionId` changes.
 *
 * @param executionId - The execution ID to stream events for.
 * @returns An object containing the accumulated list of events.
 */
export function useExecutionStream(executionId: string): { events: ExecutionEvent[] } {
  const [events, setEvents] = useState<ExecutionEvent[]>([]);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!executionId) return;

    const hubUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5080'}/hubs/execution`;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: false })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    /** Appends a new event to state with the current timestamp. */
    const addEvent = (partial: Omit<ExecutionEvent, 'timestamp'>) =>
      setEvents(prev => [...prev, { ...partial, timestamp: new Date().toISOString() }]);

    connection.on('NodeStarted', ({ nodeId, nodeType }) =>
      addEvent({ type: 'NodeStarted', nodeId, nodeType }));
    connection.on('NodeCompleted', ({ nodeId, nodeType }) =>
      addEvent({ type: 'NodeCompleted', nodeId, nodeType }));
    connection.on('NodeFailed', ({ nodeId, nodeType, error }) =>
      addEvent({ type: 'NodeFailed', nodeId, nodeType, error }));
    connection.on('ExecutionCompleted', ({ status }) =>
      addEvent({ type: 'ExecutionCompleted', status }));

    connection.start()
      .then(() => connection.invoke('JoinExecution', executionId))
      .catch(err => console.error('[useExecutionStream] SignalR connection error:', err));

    return () => {
      connection
        .invoke('LeaveExecution', executionId)
        .catch(() => { /* ignore on unmount */ })
        .finally(() => connection.stop());
    };
  }, [executionId]);

  return { events };
}
