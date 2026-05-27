import { useState, useCallback, useRef } from 'react';
import type { Node, Edge } from '@xyflow/react';

export interface DesignerSnapshot {
  nodes: Node[];
  edges: Edge[];
}

const MAX_HISTORY = 50;

/**
 * useHistory — undo/redo stack for the workflow designer canvas.
 *
 * Usage:
 *   const { pushSnapshot, undo, redo, canUndo, canRedo } = useHistory(initialSnapshot);
 *
 * Call `pushSnapshot` after any meaningful change (node add/delete, edge add/delete, config change).
 * The current position in the stack is tracked; undo/redo move the pointer and return the snapshot.
 */
export function useHistory(initial: DesignerSnapshot) {
  // past[0] is the oldest, past[past.length-1] is the most recent state before present
  const [past, setPast] = useState<DesignerSnapshot[]>([]);
  const [present, setPresent] = useState<DesignerSnapshot>(initial);
  const [future, setFuture] = useState<DesignerSnapshot[]>([]);

  // Debounce: don't push duplicate snapshots in rapid succession
  const lastPushRef = useRef<string>('');

  const pushSnapshot = useCallback((snapshot: DesignerSnapshot) => {
    const key = JSON.stringify({ n: snapshot.nodes.map(n => n.id), e: snapshot.edges.map(e => e.id) });
    if (key === lastPushRef.current) return;
    lastPushRef.current = key;

    setPast(prev => {
      const next = [...prev, present];
      return next.length > MAX_HISTORY ? next.slice(next.length - MAX_HISTORY) : next;
    });
    setPresent(snapshot);
    setFuture([]);
  }, [present]);

  const undo = useCallback((): DesignerSnapshot | null => {
    if (past.length === 0) return null;
    const previous = past[past.length - 1];
    const newPast = past.slice(0, past.length - 1);
    setPast(newPast);
    setFuture(f => [present, ...f]);
    setPresent(previous);
    lastPushRef.current = JSON.stringify({ n: previous.nodes.map(n => n.id), e: previous.edges.map(e => e.id) });
    return previous;
  }, [past, present]);

  const redo = useCallback((): DesignerSnapshot | null => {
    if (future.length === 0) return null;
    const next = future[0];
    const newFuture = future.slice(1);
    setFuture(newFuture);
    setPast(p => [...p, present]);
    setPresent(next);
    lastPushRef.current = JSON.stringify({ n: next.nodes.map(n => n.id), e: next.edges.map(e => e.id) });
    return next;
  }, [future, present]);

  return {
    pushSnapshot,
    undo,
    redo,
    canUndo: past.length > 0,
    canRedo: future.length > 0,
    present,
  };
}
