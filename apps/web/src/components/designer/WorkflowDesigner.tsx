'use client';
import { useState, useCallback, useEffect } from 'react';
import {
  ReactFlow, Background, Controls, MiniMap,
  addEdge, useNodesState, useEdgesState,
  type Connection, type Node, type Edge,
  type NodeMouseHandler,
  type EdgeMouseHandler,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import type { Workflow, NodeDescriptor } from '@/lib/api';
import { NodePalette } from './NodePalette';
import { NodeConfigDrawer } from './NodeConfigDrawer';
import { VersionHistoryPanel } from './VersionHistoryPanel';
import { AiAssistPanel } from './AiAssistPanel';
import { RunWorkflowModal } from '../RunWorkflowModal';
import { api } from '@/lib/api';
import { Save, Play, Undo2, Redo2, History, Sparkles } from 'lucide-react';
import { useHistory } from '@/hooks/useHistory';

interface Props {
  /** Workflow metadata (id, name, description). */
  workflow: Workflow;
  /** Available node types from the node catalog API. */
  nodeCatalog: NodeDescriptor[];
  /** Optional existing definition JSON to hydrate the canvas on load. */
  initialDefinitionJson?: string;
  /** Active version number to display in the toolbar. */
  activeVersionNumber?: number;
}

/** Maps node categories to their canvas background colors. */
const CATEGORY_COLORS: Record<string, string> = {
  ai: '#818cf8', documents: '#34d399', logic: '#fbbf24', human: '#f87171',
  system: '#94a3b8', integrations: '#60a5fa', data: '#a78bfa',
};

interface ContextMenu { x: number; y: number; nodeId: string; }
interface EdgeContextMenu { x: number; y: number; edgeId: string; }

/**
 * WorkflowDesigner — full-screen canvas for building workflow graphs.
 * Supports node drag-and-drop, edge connections, config drawer, delete,
 * save (persists to API), load (hydrates from saved definition), and execution.
 *
 * @param workflow - Workflow metadata (id, name, description)
 * @param nodeCatalog - Available node types from /api/nodes
 * @param initialDefinitionJson - Optional saved definition to restore on mount
 */
export function WorkflowDesigner({ workflow, nodeCatalog, initialDefinitionJson, activeVersionNumber }: Props) {
  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [selectedEdgeId, setSelectedEdgeId] = useState<string | null>(null);
  // Always derive selected node from current nodes state so config changes are reflected immediately
  const selected = selectedId ? (nodes.find(n => n.id === selectedId) ?? null) : null;
  const [showRunModal, setShowRunModal] = useState(false);
  const [saving, setSaving] = useState(false);
  const [savedAt, setSavedAt] = useState<string | null>(null);
  const [contextMenu, setContextMenu] = useState<ContextMenu | null>(null);
  const [edgeContextMenu, setEdgeContextMenu] = useState<EdgeContextMenu | null>(null);

  const { pushSnapshot, undo: historyUndo, redo: historyRedo, canUndo, canRedo } = useHistory({ nodes: [], edges: [] });
  const [showVersionHistory, setShowVersionHistory] = useState(false);
  const [showAiAssist, setShowAiAssist] = useState(false);
  const [canvasVersionNumber, setCanvasVersionNumber] = useState<number | undefined>(activeVersionNumber);

  // Hydrate canvas from saved definition when the page loads
  useEffect(() => {
    if (!initialDefinitionJson) return;
    try {
      const def = JSON.parse(initialDefinitionJson) as {
        nodes?: Array<{ id: string; type: string; position: { x: number; y: number }; data: Record<string, unknown> }>;
        edges?: Array<{ id: string; source: string; target: string; condition?: string }>;
      };

      let restoredNodes: Node[] = [];
      if (def.nodes?.length) {
        // Restore node styles from category colors
        // Always use type 'default' for React Flow rendering — actual node type lives in data.descriptor
        restoredNodes = def.nodes.map(n => {
          const descriptor = nodeCatalog.find(d => d.type === ((n.data?.descriptor as NodeDescriptor | undefined)?.type ?? n.type));
          const category = descriptor?.category ?? 'system';
          return {
            ...n,
            type: 'default',   // React Flow type — always 'default'; actual type is in data.descriptor
            data: {
              ...n.data,
              label: descriptor?.displayName ?? (n.data?.label as string) ?? n.type,
              descriptor: descriptor ?? n.data?.descriptor,
            },
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
      // Push initial snapshot so undo doesn't go past the loaded state
      const restoredEdgesForSnapshot = def.edges?.map(e => ({ ...e, type: 'default' })) ?? [];
      pushSnapshot({ nodes: restoredNodes ?? [], edges: restoredEdgesForSnapshot });
    } catch {
      // Malformed definition JSON — start with empty canvas
      console.warn('Could not parse workflow definition JSON');
    }
  }, [initialDefinitionJson, nodeCatalog, setNodes, setEdges]);

  const onConnect = useCallback(
    (conn: Connection) => setEdges(eds => {
      const next = addEdge(conn, eds);
      pushSnapshot({ nodes, edges: next });
      return next;
    }),
    [setEdges, nodes, pushSnapshot]
  );

  const onNodeClick: NodeMouseHandler = (_evt, node) => {
    setContextMenu(null);
    setSelectedId(node.id);
  };

  /** Removes an edge by id. */
  const deleteEdge = useCallback((edgeId: string) => {
    setEdges(eds => {
      const next = eds.filter(e => e.id !== edgeId);
      pushSnapshot({ nodes, edges: next });
      return next;
    });
    setSelectedEdgeId(null);
  }, [setEdges, nodes, pushSnapshot]);

  /** Removes a node and all its connected edges by id. */
  const deleteNode = useCallback((nodeId: string) => {
    setNodes(nds => {
      const nextNodes = nds.filter(n => n.id !== nodeId);
      setEdges(eds => {
        const nextEdges = eds.filter(e => e.source !== nodeId && e.target !== nodeId);
        pushSnapshot({ nodes: nextNodes, edges: nextEdges });
        return nextEdges;
      });
      return nextNodes;
    });
    setSelectedId(prev => (prev === nodeId ? null : prev));
    setContextMenu(null);
  }, [setNodes, setEdges, pushSnapshot]);

  /** Undo: restore previous canvas state. */
  const handleUndo = useCallback(() => {
    const snapshot = historyUndo();
    if (!snapshot) return;
    setNodes(snapshot.nodes);
    setEdges(snapshot.edges);
    setSelectedId(null);
    setSelectedEdgeId(null);
  }, [historyUndo, setNodes, setEdges]);

  /** Redo: restore next canvas state. */
  const handleRedo = useCallback(() => {
    const snapshot = historyRedo();
    if (!snapshot) return;
    setNodes(snapshot.nodes);
    setEdges(snapshot.edges);
    setSelectedId(null);
    setSelectedEdgeId(null);
  }, [historyRedo, setNodes, setEdges]);

  /** Delete/Backspace removes the selected node or edge; Ctrl+Z/Y for undo/redo. Skips when typing in inputs. */
  const onKeyDown = useCallback((evt: React.KeyboardEvent) => {
    const tag = (evt.target as HTMLElement).tagName;
    const inInput = tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT';
    if ((evt.ctrlKey || evt.metaKey) && evt.key === 'z' && !evt.shiftKey) {
      evt.preventDefault();
      if (!inInput) handleUndo();
      return;
    }
    if ((evt.ctrlKey || evt.metaKey) && (evt.key === 'y' || (evt.key === 'z' && evt.shiftKey))) {
      evt.preventDefault();
      if (!inInput) handleRedo();
      return;
    }
    if (evt.key === 'Delete' || evt.key === 'Backspace') {
      if (inInput) return;
      if (selectedEdgeId) { deleteEdge(selectedEdgeId); return; }
      if (selected) { deleteNode(selected.id); }
    }
  }, [selected, selectedEdgeId, deleteNode, deleteEdge, handleUndo, handleRedo]);

  const onEdgeClick: EdgeMouseHandler = useCallback((_evt, edge) => {
    setSelectedId(null);
    setSelectedEdgeId(edge.id);
    setContextMenu(null);
    setEdgeContextMenu(null);
  }, []);

  const onEdgeContextMenu: EdgeMouseHandler = useCallback((evt, edge) => {
    evt.preventDefault();
    setSelectedEdgeId(edge.id);
    setEdgeContextMenu({ x: evt.clientX, y: evt.clientY, edgeId: edge.id });
    setContextMenu(null);
  }, []);

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
    setNodes(nds => {
      const next = [...nds, newNode];
      pushSnapshot({ nodes: next, edges });
      return next;
    });
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
      await api.workflows.activateVersion(workflow.id, result.id);

      setSavedAt(new Date().toLocaleTimeString());
    } catch (e) {
      alert('Save failed: ' + (e as Error).message);
    } finally {
      setSaving(false);
    }
  };

  /** Hydrates the canvas from a parsed definition object. */
  const hydrate = useCallback((def: { nodes?: Array<{ id: string; type: string; position: { x: number; y: number }; data: Record<string, unknown> }>; edges?: Array<{ id: string; source: string; target: string }> }) => {
    if (def.nodes?.length) {
      const restoredNodes = def.nodes.map(n => {
        const descriptor = nodeCatalog.find(d => d.type === ((n.data?.descriptor as { type?: string } | undefined)?.type ?? n.type));
        const category = (descriptor?.category ?? 'system') as string;
        return {
          ...n, type: 'default' as const,
          data: { ...n.data, label: descriptor?.displayName ?? (n.data?.label as string) ?? n.type, descriptor: descriptor ?? n.data?.descriptor },
          style: { background: (CATEGORY_COLORS as Record<string, string>)[category] ?? '#94a3b8', color: 'white', border: 'none', borderRadius: 8, padding: '8px 16px', fontSize: 13, fontWeight: 500 },
        };
      });
      setNodes(restoredNodes);
      const restoredEdges = (def.edges ?? []).map(e => ({ ...e, type: 'default' as const }));
      setEdges(restoredEdges);
      pushSnapshot({ nodes: restoredNodes, edges: restoredEdges });
    }
    setSelectedId(null);
  }, [nodeCatalog, setNodes, setEdges, pushSnapshot]);

  /** Opens the Run Workflow modal for the current workflow. */
  const handleExecute = () => {
    setShowRunModal(true);
  };

  return (
    <div className="flex h-screen overflow-hidden" onKeyDown={onKeyDown} tabIndex={0} style={{ outline: 'none' }}>
      <NodePalette catalog={nodeCatalog} onAddNode={addNode} />
      <div className="flex-1 flex flex-col min-w-0">
        <div className="flex items-center justify-between px-5 py-3 border-b bg-white shrink-0">
          <div>
            <div className="flex items-center gap-2"><h2 className="font-semibold text-gray-900">{workflow.name}</h2>{canvasVersionNumber != null && (<span className="text-xs text-slate-500 border border-slate-200 rounded px-2 py-0.5 bg-slate-50">v{canvasVersionNumber}</span>)}</div>
            <p className="text-xs text-gray-400">
              Click a node to configure · Right-click or <kbd className="text-xs bg-gray-100 border rounded px-1">Del</kbd> to delete · <kbd className="text-xs bg-gray-100 border rounded px-1">Ctrl+Z</kbd> undo · <kbd className="text-xs bg-gray-100 border rounded px-1">Ctrl+Y</kbd> redo
              {savedAt && <span className="ml-3 text-green-600">✓ Saved at {savedAt}</span>}
            </p>
          </div>
          <div className="flex gap-2">
            {/* Undo/Redo buttons */}
            <button
              className={`border text-sm px-2 py-1.5 rounded-lg flex items-center gap-1 transition-colors ${
                canUndo ? 'hover:bg-gray-50 text-gray-600' : 'opacity-30 cursor-not-allowed text-gray-400'
              }`}
              onClick={handleUndo}
              disabled={!canUndo}
              title="Undo (Ctrl+Z)"
            >
              <Undo2 size={14} />
            </button>
            <button
              className={`border text-sm px-2 py-1.5 rounded-lg flex items-center gap-1 transition-colors ${
                canRedo ? 'hover:bg-gray-50 text-gray-600' : 'opacity-30 cursor-not-allowed text-gray-400'
              }`}
              onClick={handleRedo}
              disabled={!canRedo}
              title="Redo (Ctrl+Y)"
            >
              <Redo2 size={14} />
            </button>
            <button
              className={`border text-sm px-2 py-1.5 rounded-lg flex items-center gap-1.5 transition-colors ${
                showVersionHistory ? 'bg-slate-100 text-slate-800' : 'hover:bg-gray-50 text-gray-600'
              }`}
              onClick={() => { setShowVersionHistory(v => !v); setShowAiAssist(false); }}
              title="Version history"
            >
              <History size={14} /> History
            </button>
            <button
              className={`border text-sm px-2 py-1.5 rounded-lg flex items-center gap-1.5 transition-colors ${
                showAiAssist ? 'bg-purple-100 text-purple-800 border-purple-300' : 'hover:bg-gray-50 text-gray-600'
              }`}
              onClick={() => { setShowAiAssist(v => !v); setShowVersionHistory(false); }}
              title="AI Workflow Assistant"
            >
              <Sparkles size={14} /> AI
            </button>
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
                !canvasVersionNumber ? 'bg-gray-400 cursor-not-allowed' : 'bg-emerald-600 hover:bg-emerald-700'
              } text-white`}
              onClick={handleExecute}
              disabled={!canvasVersionNumber}
            >
              <Play size={14} />Run
            </button>
          </div>
        </div>
        <div className="flex-1" onClick={() => setContextMenu(null)}>
          <ReactFlow
            nodes={nodes} edges={edges.map(e => ({
              ...e,
              style: e.id === selectedEdgeId
                ? { stroke: '#ef4444', strokeWidth: 3 }
                : { stroke: '#94a3b8', strokeWidth: 2 },
              animated: e.id === selectedEdgeId,
              label: e.id === selectedEdgeId ? '✕ Del' : undefined,
            }))}
            onNodesChange={onNodesChange} onEdgesChange={onEdgesChange}
            onConnect={onConnect}
            onNodeClick={onNodeClick}
            onEdgeClick={onEdgeClick}
            onEdgeContextMenu={onEdgeContextMenu}
            onNodeContextMenu={onNodeContextMenu}
            onPaneClick={() => { setSelectedId(null); setSelectedEdgeId(null); setContextMenu(null); setEdgeContextMenu(null); }}
            fitView>
            <Background />
            <Controls />
            <MiniMap />
          </ReactFlow>
        </div>
      </div>

      {/* Config drawer with delete button */}
      {selected && !showVersionHistory && !showAiAssist && (
        <NodeConfigDrawer
          node={selected}
          catalog={nodeCatalog}
          onClose={() => setSelectedId(null)}
          onDelete={() => deleteNode(selected.id)}
          onConfigChange={(config) =>
            setNodes(nds => {
              const next = nds.map(n =>
                n.id === selected.id ? { ...n, data: { ...n.data, config } } : n
              );
              pushSnapshot({ nodes: next, edges });
              return next;
            })
          }
        />
      )}

      {/* Version history panel */}
      {showVersionHistory && (
        <VersionHistoryPanel
          workflowId={workflow.id}
          currentVersionNumber={canvasVersionNumber}
          onClose={() => setShowVersionHistory(false)}
          onLoadVersion={(definitionJson, versionNumber) => {
            try {
              const def = JSON.parse(definitionJson) as {
                nodes?: Array<{ id: string; type: string; position: { x: number; y: number }; data: Record<string, unknown> }>;
                edges?: Array<{ id: string; source: string; target: string }>;
              };
              hydrate(def);
              setCanvasVersionNumber(versionNumber);
            } catch { /* ignore malformed */ }
          }}
          onActivated={() => {
            api.workflows.getActiveVersion(workflow.id).then(v => setCanvasVersionNumber(v.versionNumber)).catch(() => {});
          }}
        />
      )}

      {/* AI Assist panel */}
      {showAiAssist && (
        <AiAssistPanel
          workflowId={workflow.id}
          workflowName={workflow.name}
          nodeCatalog={nodeCatalog}
          onClose={() => setShowAiAssist(false)}
          onPreview={(def) => {
            hydrate(def as { nodes?: Array<{ id: string; type: string; position: { x: number; y: number }; data: Record<string, unknown> }>; edges?: Array<{ id: string; source: string; target: string }> });
          }}
          onAccept={(def) => {
            hydrate(def as { nodes?: Array<{ id: string; type: string; position: { x: number; y: number }; data: Record<string, unknown> }>; edges?: Array<{ id: string; source: string; target: string }> });
            handleSave();
          }}
          getCurrentDefinitionJson={() => JSON.stringify({
            id: workflow.id,
            name: workflow.name,
            version: 1,
            nodes: nodes.map(n => ({
              id: n.id,
              type: (n.data?.descriptor as NodeDescriptor | undefined)?.type ?? n.type,
              position: n.position,
              data: n.data,
              config: (n.data?.config as Record<string, unknown>) ?? {},
            })),
            edges: edges.map(e => ({ id: e.id, source: e.source, target: e.target })),
          })}
        />
      )}

      {showRunModal && (
        <RunWorkflowModal workflow={workflow} onClose={() => setShowRunModal(false)} />
      )}

      {/* Right-click context menu — nodes */}
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

      {/* Right-click context menu — edges */}
      {edgeContextMenu && (
        <div
          className="fixed z-50 bg-white border rounded-lg shadow-lg py-1 min-w-[140px]"
          style={{ top: edgeContextMenu.y, left: edgeContextMenu.x }}
          onMouseLeave={() => setEdgeContextMenu(null)}
        >
          <button
            className="w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50 flex items-center gap-2"
            onClick={() => deleteEdge(edgeContextMenu.edgeId)}
          >
            <span>🗑️</span> Delete Line
          </button>
        </div>
      )}
    </div>
  );
}


