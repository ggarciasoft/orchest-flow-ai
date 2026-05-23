'use client';
import { useState, useCallback } from 'react';
import {
  ReactFlow, Background, Controls, MiniMap,
  addEdge, useNodesState, useEdgesState,
  type Connection, type Node, type Edge,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import type { Workflow, NodeDescriptor } from '@/lib/api';
import { NodePalette } from './NodePalette';
import { NodeConfigDrawer } from './NodeConfigDrawer';
import { api } from '@/lib/api';
import { Save, Play } from 'lucide-react';

interface Props { workflow: Workflow; nodeCatalog: NodeDescriptor[]; }

const CATEGORY_COLORS: Record<string, string> = {
  ai: '#818cf8', documents: '#34d399', logic: '#fbbf24', human: '#f87171',
  system: '#94a3b8', integrations: '#60a5fa',
};

export function WorkflowDesigner({ workflow, nodeCatalog }: Props) {
  const [nodes, setNodes, onNodesChange] = useNodesState<Node>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const [selected, setSelected] = useState<Node | null>(null);
  const [executing, setExecuting] = useState(false);

  const onConnect = useCallback((conn: Connection) => setEdges(eds => addEdge(conn, eds)), [setEdges]);
  const onNodeClick = (_: React.MouseEvent, node: Node) => setSelected(node);

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

  const handleExecute = async () => {
    setExecuting(true);
    try {
      await api.workflows.execute(workflow.id, {});
      alert('Execution started! Check the Executions page for progress.');
    } catch (e) {
      alert('Execution failed: ' + (e as Error).message);
    } finally { setExecuting(false); }
  };

  return (
    <div className="flex h-screen overflow-hidden">
      <NodePalette catalog={nodeCatalog} onAddNode={addNode} />
      <div className="flex-1 flex flex-col min-w-0">
        <div className="flex items-center justify-between px-5 py-3 border-b bg-white shrink-0">
          <div>
            <h2 className="font-semibold text-gray-900">{workflow.name}</h2>
            <p className="text-xs text-gray-400">Drag nodes from the left panel to design your workflow</p>
          </div>
          <div className="flex gap-2">
            <button className="border text-sm px-3 py-1.5 rounded-lg hover:bg-gray-50 flex items-center gap-1.5 text-gray-600"
              onClick={() => alert('Save workflow coming in next phase!')}>
              <Save size={14} />Save
            </button>
            <button className={`text-sm px-4 py-1.5 rounded-lg flex items-center gap-1.5 font-medium ${executing ? 'bg-gray-400' : 'bg-blue-600 hover:bg-blue-700'} text-white`}
              onClick={handleExecute} disabled={executing}>
              <Play size={14} />{executing ? 'Starting…' : 'Execute'}
            </button>
          </div>
        </div>
        <div className="flex-1">
          <ReactFlow
            nodes={nodes} edges={edges}
            onNodesChange={onNodesChange} onEdgesChange={onEdgesChange}
            onConnect={onConnect} onNodeClick={onNodeClick} fitView>
            <Background />
            <Controls />
            <MiniMap />
          </ReactFlow>
        </div>
      </div>
      {selected && (
        <NodeConfigDrawer
          node={selected} catalog={nodeCatalog}
          onClose={() => setSelected(null)}
          onConfigChange={(config) =>
            setNodes(nds => nds.map(n => n.id === selected.id ? { ...n, data: { ...n.data, config } } : n))
          }
        />
      )}
    </div>
  );
}
