'use client';
import type { Node } from '@xyflow/react';
import type { NodeDescriptor } from '@/lib/api';
import { X, Trash2 } from 'lucide-react';

interface Props { node: Node; catalog: NodeDescriptor[]; onClose: () => void; onDelete: () => void; onConfigChange: (config: Record<string, unknown>) => void; }

export function NodeConfigDrawer({ node, catalog, onClose, onDelete, onConfigChange }: Props) {
  const data = node.data as { descriptor?: NodeDescriptor; config?: Record<string, unknown> };
  const descriptor = catalog.find(d => d.type === data.descriptor?.type);
  const config = data.config ?? {};
  if (!descriptor) return null;

  return (
    <div className="w-80 border-l bg-white flex flex-col shrink-0 overflow-hidden">
      <div className="flex items-center justify-between p-4 border-b">
        <div>
          <h3 className="font-semibold text-sm">{descriptor.displayName}</h3>
          <p className="text-xs text-gray-400 font-mono mt-0.5">{descriptor.type}</p>
        </div>
        <button onClick={onClose} className="p-1 hover:bg-gray-100 rounded" title="Close"><X size={16} /></button>
      </div>
      <div className="flex-1 overflow-y-auto p-4 space-y-5">
        <div>
          <p className="text-xs font-semibold text-gray-500 uppercase mb-1">Description</p>
          <p className="text-sm text-gray-600">{descriptor.description}</p>
        </div>
        {descriptor.configuration.length > 0 && (
          <div>
            <p className="text-xs font-semibold text-gray-500 uppercase mb-3">Configuration</p>
            <div className="space-y-3">
              {descriptor.configuration.map(cfg => (
                <div key={cfg.key}>
                  <label className="block text-xs font-medium text-gray-700 mb-1">
                    {cfg.displayName}{cfg.required && <span className="text-red-500 ml-1">*</span>}
                  </label>
                  {cfg.allowedValues ? (
                    <select className="w-full border rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                      value={String(config[cfg.key] ?? cfg.defaultValue ?? '')}
                      onChange={e => onConfigChange({ ...config, [cfg.key]: e.target.value })}>
                      {cfg.allowedValues.map(v => <option key={v} value={v}>{v}</option>)}
                    </select>
                  ) : (
                    <input className="w-full border rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder={String(cfg.defaultValue ?? '')}
                      value={String(config[cfg.key] ?? '')}
                      onChange={e => onConfigChange({ ...config, [cfg.key]: e.target.value })} />
                  )}
                  <p className="text-xs text-gray-400 mt-0.5">{cfg.description}</p>
                </div>
              ))}
            </div>
          </div>
        )}
        {descriptor.inputs.length > 0 && (
          <div>
            <p className="text-xs font-semibold text-gray-500 uppercase mb-2">Inputs</p>
            {descriptor.inputs.map(i => (
              <div key={i.key} className="text-xs py-1.5 border-b flex gap-2">
                <span className="font-mono font-medium text-gray-700">{i.key}</span>
                <span className="text-gray-400">({i.type})</span>
                {i.required && <span className="text-red-400">required</span>}
              </div>
            ))}
          </div>
        )}
        {descriptor.outputs.length > 0 && (
          <div>
            <p className="text-xs font-semibold text-gray-500 uppercase mb-2">Outputs</p>
            {descriptor.outputs.map(o => (
              <div key={o.key} className="text-xs py-1.5 border-b flex gap-2">
                <span className="font-mono font-medium text-gray-700">{o.key}</span>
                <span className="text-gray-400">({o.type})</span>
              </div>
            ))}
          </div>
        )}
      </div>
      {/* Delete button at bottom of drawer */}
      <div className="p-4 border-t shrink-0">
        <button
          onClick={onDelete}
          className="w-full flex items-center justify-center gap-2 text-sm text-red-600 hover:bg-red-50 border border-red-200 rounded-lg py-2 transition-colors"
        >
          <Trash2 size={14} /> Delete Node
        </button>
      </div>
    </div>
  );
}
