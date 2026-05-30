'use client';
import { useState } from 'react';
import {
  ChevronDown, ChevronRight,
  PlayCircle, StopCircle, GitBranch, Timer, GitMerge, Shuffle,
  UserCheck, Brain, FileText, Globe, Mail, Send, Webhook, Clock,
  Repeat, Database, Code2, RefreshCw, FileSearch, Download,
  CheckSquare, Cpu, Layers, type LucideIcon,
} from 'lucide-react';
import type { NodeDescriptor } from '@/lib/api';

interface Props {
  /** All available node types fetched from the node catalog API. */
  catalog: NodeDescriptor[];
  /** Callback invoked when the user clicks a node to add it to the canvas. */
  onAddNode: (descriptor: NodeDescriptor) => void;
}

/** Display order for node categories in the palette. */
const CATEGORY_ORDER = ['system', 'logic', 'human', 'ai', 'documents', 'integrations', 'data'];

/** Maps node categories to their canvas background colors. */
const CATEGORY_COLORS: Record<string, string> = {
  ai: '#818cf8', documents: '#34d399', logic: '#fbbf24', human: '#f87171',
  system: '#94a3b8', integrations: '#60a5fa', data: '#a78bfa',
};

const NODE_ICON_MAP: Record<string, LucideIcon> = {
  'play':           PlayCircle,
  'stop':           StopCircle,
  'start':          PlayCircle,
  'end':            StopCircle,
  'condition':      GitBranch,
  'delay':          Timer,
  'merge':          GitMerge,
  'switch':         Shuffle,
  'approval':       UserCheck,
  'human-approval': UserCheck,
  'brain':          Brain,
  'ai':             Brain,
  'document':       FileText,
  'file-text':      FileText,
  'globe':          Globe,
  'http':           Globe,
  'mail':           Mail,
  'email':          Mail,
  'send':           Send,
  'webhook':        Webhook,
  'clock':          Clock,
  'repeat':         Repeat,
  'foreach':        Repeat,
  'database':       Database,
  'db':             Database,
  'code':           Code2,
  'transform':      Code2,
  'json':           Code2,
  'refresh':        RefreshCw,
  'extract':        FileSearch,
  'download':       Download,
  'clipboard-list': CheckSquare,
  'cpu':            Cpu,
  'layers':         Layers,
  'data-checkpoint': Download,
  'set-variable':   Code2,
};

function getNodeIcon(iconKey?: string): LucideIcon | null {
  if (!iconKey) return null;
  return NODE_ICON_MAP[iconKey.toLowerCase()] ?? null;
}

/**
 * NodePalette — categorized sidebar listing all available workflow node types.
 * Categories are collapsible. Clicking a node invokes onAddNode to place it on the canvas.
 *
 * @param catalog - Array of node descriptors from the API registry.
 * @param onAddNode - Called with the selected descriptor when the user adds a node.
 */
export function NodePalette({ catalog, onAddNode }: Props) {
  // Track which categories are expanded; default system and ai open
  const [open, setOpen] = useState<Record<string, boolean>>({ system: true, ai: true, documents: true });

  /** Groups node descriptors by category: known categories first in order, then any unknown ones appended. */
  const allCategories = [
    ...CATEGORY_ORDER,
    ...Array.from(new Set(catalog.map(n => n.category))).filter(c => !CATEGORY_ORDER.includes(c)),
  ];
  const grouped = allCategories.reduce<Record<string, NodeDescriptor[]>>((acc, cat) => {
    acc[cat] = catalog.filter(n => n.category === cat);
    return acc;
  }, {});

  return (
    <div className="w-64 border-r bg-white overflow-y-auto flex flex-col shrink-0">
      <div className="p-4 border-b"><h3 className="font-semibold text-sm text-gray-700">Node Palette</h3></div>
      {allCategories.map(cat => {
        const nodes = grouped[cat] ?? [];
        // Skip categories that have no registered nodes
        if (!nodes.length) return null;
        return (
          <div key={cat}>
            <button
              onClick={() => setOpen(o => ({ ...o, [cat]: !o[cat] }))}
              className="w-full flex items-center justify-between px-4 py-2 text-xs font-semibold uppercase text-gray-500 hover:bg-gray-50"
            >
              {cat}
              {open[cat] ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
            </button>
            {open[cat] && nodes.map(n => {
              const Icon = getNodeIcon(n.iconKey);
              return (
                <button
                  key={n.type}
                  onClick={() => onAddNode(n)}
                  className="w-full text-left px-3 py-2 text-xs hover:bg-slate-50 rounded-lg transition-colors flex items-center gap-2 group"
                >
                  <span
                    className="w-6 h-6 rounded flex items-center justify-center shrink-0 text-white"
                    style={{ background: CATEGORY_COLORS[n.category] ?? '#94a3b8' }}
                  >
                    {Icon ? <Icon size={12} /> : <span className="text-[10px] font-bold">{n.displayName[0]}</span>}
                  </span>
                  <span className="flex-1 truncate text-slate-700 group-hover:text-slate-900">{n.displayName}</span>
                </button>
              );
            })}
          </div>
        );
      })}
      {catalog.length === 0 && (
        <div className="p-4 text-sm text-gray-400 text-center">
          <p>No nodes available.</p>
          <p className="text-xs mt-1">Start the API to load the catalog.</p>
        </div>
      )}
    </div>
  );
}
