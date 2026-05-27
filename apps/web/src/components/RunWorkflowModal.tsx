'use client';
import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { X, Play, Loader2 } from 'lucide-react';
import { api, Workflow } from '@/lib/api';

interface Props {
  workflow: Workflow;
  onClose: () => void;
}

/**
 * RunWorkflowModal — prompts for workflow inputs before execution.
 * Parses the active version definition to discover expected input keys
 * from the system.start node's outgoing edges, then renders a form.
 */
export function RunWorkflowModal({ workflow, onClose }: Props) {
  const router = useRouter();
  const [inputs, setInputs] = useState<Record<string, string>>({});
  const [inputKeys, setInputKeys] = useState<string[]>([]);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    // Fetch active version to discover start node output keys
    api.workflows.getActiveVersion(workflow.id)
      .then(v => {
        try {
          const def = JSON.parse(v.definitionJson);
          // Find system.start node id
          const startNode = def.nodes?.find((n: { type: string }) => n.type === 'system.start');
          if (!startNode) { setInputKeys([]); return; }
          // Find all edges leaving system.start — their source keys are the workflow inputs
          const startEdges = def.edges?.filter((e: { source: string; map?: Record<string, string> }) => e.source === startNode.id) ?? [];
          // Collect unique target keys from edge maps, or use a default 'input' if no map
          const keys = new Set<string>();
          for (const edge of startEdges) {
            if (edge.map) {
              Object.values(edge.map).forEach((k: unknown) => keys.add(String(k)));
            }
          }
          // If no edge maps, offer a generic free-form JSON input
          setInputKeys(keys.size > 0 ? [...keys] : []);
        } catch {
          setInputKeys([]);
        }
      })
      .catch(() => setInputKeys([]))
      .finally(() => setLoading(false));
  }, [workflow.id]);

  const handleRun = async () => {
    setRunning(true);
    setError('');
    try {
      // Build input object; try to parse JSON values, fall back to string
      const inputObj: Record<string, unknown> = {};
      for (const [k, v] of Object.entries(inputs)) {
        try { inputObj[k] = JSON.parse(v); } catch { inputObj[k] = v; }
      }
      // Also add a raw freeform JSON field if user typed in it
      const freeformJson = inputs['__freeform__'];
      let finalInput = inputObj;
      if (freeformJson) {
        try { finalInput = { ...inputObj, ...JSON.parse(freeformJson) }; } catch { /* ignore parse error */ }
        delete finalInput['__freeform__'];
      }
      const execution = await api.workflows.execute(workflow.id, finalInput);
      router.push(`/executions/${execution.id}`);
    } catch (e) {
      setError((e as Error).message);
      setRunning(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b border-slate-200">
          <div>
            <h2 className="text-base font-semibold text-slate-900">Run Workflow</h2>
            <p className="text-xs text-slate-500 mt-0.5 truncate max-w-[280px]">{workflow.name}</p>
          </div>
          <button onClick={onClose} className="p-1.5 hover:bg-slate-100 rounded-lg text-slate-400">
            <X size={16} />
          </button>
        </div>

        {/* Body */}
        <div className="p-5 space-y-4">
          {loading ? (
            <div className="flex items-center gap-2 text-slate-400 text-sm py-4 justify-center">
              <Loader2 size={16} className="animate-spin" /> Loading workflow inputs…
            </div>
          ) : inputKeys.length > 0 ? (
            <>
              <p className="text-xs text-slate-500">Fill in the workflow inputs below.</p>
              {inputKeys.map(key => (
                <div key={key}>
                  <label className="block text-xs font-medium text-slate-700 mb-1">{key}</label>
                  <input
                    type="text"
                    placeholder={`Value for "${key}"`}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    value={inputs[key] ?? ''}
                    onChange={e => setInputs(prev => ({ ...prev, [key]: e.target.value }))}
                  />
                </div>
              ))}
            </>
          ) : (
            <>
              <p className="text-xs text-slate-500">
                This workflow has no declared inputs. You can optionally pass a JSON payload.
              </p>
              <div>
                <label className="block text-xs font-medium text-slate-700 mb-1">Input JSON <span className="text-slate-400">(optional)</span></label>
                <textarea
                  rows={4}
                  placeholder='{"key": "value"}'
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-indigo-500 resize-none"
                  value={inputs['__freeform__'] ?? ''}
                  onChange={e => setInputs(prev => ({ ...prev, '__freeform__': e.target.value }))}
                />
              </div>
            </>
          )}

          {error && (
            <p className="text-xs text-red-600 bg-red-50 border border-red-100 rounded-lg px-3 py-2">{error}</p>
          )}
        </div>

        {/* Footer */}
        <div className="flex gap-3 p-5 pt-0">
          <button
            onClick={onClose}
            className="flex-1 px-4 py-2 border border-slate-200 rounded-lg text-sm text-slate-700 hover:bg-slate-50 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleRun}
            disabled={running || loading}
            className="flex-1 px-4 py-2 bg-emerald-600 text-white rounded-lg text-sm font-medium hover:bg-emerald-700 disabled:opacity-50 flex items-center justify-center gap-2 transition-colors"
          >
            {running ? <Loader2 size={14} className="animate-spin" /> : <Play size={14} />}
            {running ? 'Starting…' : 'Run'}
          </button>
        </div>
      </div>
    </div>
  );
}
