'use client';
import { useState } from 'react';
import { ChevronDown, ChevronRight } from 'lucide-react';
import type { NodeDescriptor } from '@/lib/api';

interface Props {
  /** All available node types fetched from the node catalog API. */
  catalog: NodeDescriptor[];
  /** Callback invoked when the user clicks a node to add it to the canvas. */
  onAddNode: (descriptor: NodeDescriptor) => void;
}

/** Display order for node categories in the palette. */
const CATEGORY_ORDER = ['system', 'logic', 'human', 'ai', 'documents', 'integrations'];

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

  /** Groups node descriptors from the catalog by their category in predefined order. */
  const grouped = CATEGORY_ORDER.reduce<Record<string, NodeDescriptor[]>>((acc, cat) => {
    acc[cat] = catalog.filter(n => n.category === cat);
    return acc;
  }, {});

  return (
    <div className="w-64 border-r bg-white overflow-y-auto flex flex-col shrink-0">
      <div className="p-4 border-b"><h3 className="font-semibold text-sm text-gray-700">Node Palette</h3></div>
      {CATEGORY_ORDER.map(cat => {
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
            {open[cat] && nodes.map(n => (
              <button
                key={n.type}
                onClick={() => onAddNode(n)}
                className="w-full text-left px-4 py-2.5 text-sm hover:bg-blue-50 border-b border-gray-50 transition-colors"
              >
                <div className="font-medium text-gray-800">{n.displayName}</div>
                <div className="text-xs text-gray-400 mt-0.5 overflow-hidden text-ellipsis whitespace-nowrap">{n.description}</div>
              </button>
            ))}
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
