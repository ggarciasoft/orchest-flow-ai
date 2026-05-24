'use client';
import { useState, useCallback, useEffect } from 'react';
import {
  ReactFlow, Background, Controls, MiniMap,
  addEdge, useNodesState, useEdgesState,
  type Connection, type Node, type Edge,
  type NodeMouseHandler,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import type { Workflow, NodeDescriptor } from '@/lib/api';
import { NodePalette } from './NodePalette';
import { NodeConfigDrawer } from './NodeConfigDrawer';
import { api } from '@/lib/api';
import { Save, Play, CheckCircle } from 'lucide-react';

interface Props {
  /** Workflow metadata (id, name, description). */
  workflow: Workflow;
  /** Available node types from the node catalog API. */
  nodeCatalog: NodeDescriptor[];
  /** Optional existing definition JSON to hydrate the canvas on load. */
  initialDefinitionJson?: string;
}

/** Maps node categories to their canvas background colors. */
const CATEGORY_COLORS: Record<string, string> = {
  ai: '#818cf8', documents: '#34d399', logic: '#fbbf24', human: '#f87171',
  system: '#94a3b8', integrations: '#60a5fa', data: '#a78bfa',
};

interface ContextMenu { x: number; y: number; nodeId: string; }

/**
 * WorkflowDesigner — full-screen canvas for building workflow graphs.
 * Supports node drag-and-drop, edge connections, config drawer, delete,
 * save (persists to API), load (hydrates from saved definition), and execution.
 *
 * @param workflow - Workflow metadata (id, name, description)
 * @param nodeCatalog - Available node types from /api/nodes
 * @param initialDefinitionJson - Optional saved definition to restore on mount
 */
export function WorkflowDesigner({ workflow, nodeCatalog, initialDefinitionJson }: Props) {
  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const [selected, setSelected] = useState<Node | null>(null);
  const [executing, setExecuting] = useState(false);
  const [saving, setSaving] = useState(false);
  const [savedAt, setSavedAt] = useState<string | null>(null);
  const [contextMenu, setContextMenu] = useState<ContextMenu | null>(null);

  // Hydrate canvas from saved definition when the page loads
  useEffect(() => {
    if (!initialDefinitionJson) return;
    try {
      const def = JSON.parse(initialDefinitionJson) as {
        nodes?: Array<{ id: string; type: string; position: { x: number; y: number }; data: Record<string, unknown> }>;
        edges?: Array<{ id: string; source: string; target: string; condition?: string }>;
      };

      if (def.nodes?.length) {
        // Restore node styles from category colors
        const restoredNodes: Node[] = def.nodes.map(n => {
          const descriptor = nodeCatalog.find(d => d.type === (n.data?.descriptor as NodeDescriptor | undefined)?.type);
          const category = descriptor?.category ?? 'system';
          return {
            ...n,
            style: {
              background: CATEGORY_COLORS[category] ?? '#94a3b8',
              color: 'white', border: 'none', borderRadius: 8,
              padding: '8px 16px', fontSize: 13, fontWeight: 500,
            },
          };
        });
        setNodes(restoredNodes);
      }

      if (def.edges?.length) {
        setEdges(def.edges.map(e => ({ ...e, type: 'default' })));
      }
    } catch {
      // Malformed definition JSON — start with empty canvas
      console.warn('Could not parse workflow definition JSON');
    }
  }, [initialDefinitionJson, nodeCatalog, setNodes, setEdges]);

  const onConnect = useCallback(
    (conn: Connection) => setEdges(eds => addEdge(conn, eds)),
    [setEdges]
  );

  const onNodeClick: NodeMouseHandler = (_evt, node) => {
    setContextMenu(null);
    setSelected(node);
  };

  /** Removes a node and all its connected edges by id. */
  const deleteNode = useCallback((nodeId: string) => {
    setNodes(nds => nds.filter(n => n.id !== nodeId));
    setEdges(eds => eds.filter(e => e.source !== nodeId && e.target !== nodeId));
    setSelected(prev => (prev?.id === nodeId ? null : prev));
    setContextMenu(null);
  }, [setNodes, setEdges]);

  /** Delete/Backspace removes the selected node; skips when typing in inputs. */
  const onKeyDown = useCallback((evt: React.KeyboardEvent) => {
    if ((evt.key === 'Delete' || evt.key === 'Backspace') && selected) {
      const tag = (evt.target as HTMLElement).tagName;
      if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
      deleteNode(selected.id);
    }
  }, [selected, deleteNode]);

  const onNodeContextMenu: NodeMouseHandler = useCallback((evt, node) => {
    evt.preventDefault();
    setContextMenu({ x: evt.clientX, y: evt.clientY, nodeId: node.id });
  }, []);

  /** Adds a new node to the canvas at a random position. */
  const addNode = (descriptor: NodeDescriptor) => {
    const newNode: Node = {
      id: `${descriptor.type}-${Date.now()}`,
      type: 'default',
      position: { x: 200 + Math.random() * 300, y: 100 + Math.random() * 300 },
      data: { label: descriptor.displayName, descriptor, config: {} },
      style: {
        background: CATEGORY_COLORS[descriptor.category] ?? '#94a3b8',
        color: 'white', border: 'none', borderRadius: 8, padding: '8px 16px',
        fontSize: 13, fontWeight: 500,
      },
    };
    setNodes(nds => [...nds, newNode]);
  };

  /**
   * Serializes the current canvas (nodes + edges) into the WorkflowDefinition
   * format and saves it as a new version via the API, then activates it.
   */
  const handleSave = async () => {
    setSaving(true);
    setSavedAt(null);
    try {
      // Build the definition object matching OrchestFlowAI.Engine.Models.WorkflowDefinition
      const definition = {
        id: workflow.id,
        name: workflow.name,
        version: 1,
        nodes: nodes.map(n => ({
          id: n.id,
          type: (n.data?.descriptor as NodeDescriptor | undefined)?.type ?? n.type ?? 'unknown',
          position: n.position,
          data: n.data,
          config: (n.data?.config as Record<string, unknown>) ?? {},
        })),
        edges: edges.map(e => ({
          id: e.id,
          source: e.source,
          target: e.target,
          condition: (e.data as Record<string, string> | undefined)?.condition,
          map: (e.data as Record<string, Record<string, string>> | undefined)?.map,
        })),
      };

      const result = await api.workflows.saveVersion(workflow.id, definition);

      // Activate the newly saved version so it's the one that gets executed
      await fetch(`/api/workflows/${workflow.id}/versions/${result.id}/activate`, { method: 'POST' });

      setSavedAt(new Date().toLocaleTimeString());
    } catch (e) {
      alert('Save failed: ' + (e as Error).message);
    } finally {
      setSaving(false);
    }
  };

  /** Enqueues a workflow execution using the currently active version. */
  const handleExecute = async () => {
    setExecuting(true);
    try {
      await api.workflows.execute(workflow.id, {});
      alert('Execution started! Check the Executions page for progress.');
    } catch (e) {
      alert('Execution failed: ' + (e as Error).message);
    } finally {
      setExecuting(false);
    }
  };

  return (
    <div className="flex h-screen overflow-hidden" onKeyDown={onKeyDown} tabIndex={0} style={{ outline: 'none' }}>
      <NodePalette catalog={nodeCatalog} onAddNode={addNode} />
      <div className="flex-1 flex flex-col min-w-0">
        <div className="flex items-center justify-between px-5 py-3 border-b bg-white shrink-0">
          <div>
            <h2 className="font-semibold text-gray-900">{workflow.name}</h2>
            <p className="text-xs text-gray-400">
              Click a node to configure · Right-click or <kbd className="text-xs bg-gray-100 border rounded px-1">Del</kbd> to delete
              {savedAt && <span className="ml-3 text-green-600">✓ Saved at {savedAt}</span>}
            </p>
          </div>
          <div className="flex gap-2">
            {/* Save button — serializes canvas and persists to API */}
            <button
              className={`border text-sm px-3 py-1.5 rounded-lg flex items-center gap-1.5 transition-colors ${
                saving ? 'opacity-50 cursor-not-allowed text-gray-400' : 'hover:bg-gray-50 text-gray-600'
              }`}
              onClick={handleSave}
              disabled={saving}
            >
              {saving ? <span className="animate-spin text-xs">⏳</span> : <Save size={14} />}
              {saving ? 'Saving…' : 'Save'}
            </button>
            <button
              className={`text-sm px-4 py-1.5 rounded-lg flex items-center gap-1.5 font-medium ${
                executing ? 'bg-gray-400 cursor-not-allowed' : 'bg-blue-600 hover:bg-blue-700'
              } text-white`}
              onClick={handleExecute}
              disabled={executing}
            >
              <Play size={14} />{executing ? 'Starting…' : 'Execute'}
            </button>
          </div>
        </div>
        <div className="flex-1" onClick={() => setContextMenu(null)}>
          <ReactFlow
            nodes={nodes} edges={edges}
            onNodesChange={onNodesChange} onEdgesChange={onEdgesChange}
            onConnect={onConnect}
            onNodeClick={onNodeClick}
            onNodeContextMenu={onNodeContextMenu}
            onPaneClick={() => { setSelected(null); setContextMenu(null); }}
            fitView>
            <Background />
            <Controls />
            <MiniMap />
          </ReactFlow>
        </div>
      </div>

      {/* Config drawer with delete button */}
      {selected && (
        <NodeConfigDrawer
          node={selected}
          catalog={nodeCatalog}
          onClose={() => setSelected(null)}
          onDelete={() => deleteNode(selected.id)}
          onConfigChange={(config) =>
            setNodes(nds => nds.map(n =>
              n.id === selected.id ? { ...n, data: { ...n.data, config } } : n
            ))
          }
        />
      )}

      {/* Right-click context menu */}
      {contextMenu && (
        <div
          className="fixed z-50 bg-white border rounded-lg shadow-lg py-1 min-w-[140px]"
          style={{ top: contextMenu.y, left: contextMenu.x }}
          onMouseLeave={() => setContextMenu(null)}
        >
          <button
            className="w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50 flex items-center gap-2"
            onClick={() => deleteNode(contextMenu.nodeId)}
          >
            <span>🗑️</span> Delete Node
          </button>
        </div>
      )}
    </div>
  );
}
