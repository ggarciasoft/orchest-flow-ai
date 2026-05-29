'use client';
import { useState } from 'react';
import { X, XCircle, Loader2, AlertTriangle } from 'lucide-react';

interface Props {
  executionId: string;
  workflowName?: string | null;
  versionNumber?: number | null;
  onConfirm: () => Promise<void>;
  onClose: () => void;
}

/**
 * CancelExecutionModal — confirmation dialog before cancelling a workflow execution.
 * Shows workflow name/version and warns the user the action is irreversible.
 */
export function CancelExecutionModal({ executionId, workflowName, versionNumber, onConfirm, onClose }: Props) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  async function handleConfirm() {
    setLoading(true);
    setError('');
    try {
      await onConfirm();
      onClose();
    } catch (e) {
      setError((e as Error).message ?? 'Failed to cancel execution.');
      setLoading(false);
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b border-slate-200">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-full bg-red-100 flex items-center justify-center shrink-0">
              <XCircle size={18} className="text-red-600" />
            </div>
            <div>
              <h2 className="text-base font-semibold text-slate-900">Cancel Execution</h2>
              {(workflowName || versionNumber != null) && (
                <p className="text-xs text-slate-500 mt-0.5">
                  {[workflowName, versionNumber != null ? `v${versionNumber}` : null].filter(Boolean).join(' · ')}
                </p>
              )}
            </div>
          </div>
          <button onClick={onClose} disabled={loading} className="p-1.5 hover:bg-slate-100 rounded-lg text-slate-400 disabled:opacity-50">
            <X size={16} />
          </button>
        </div>

        {/* Body */}
        <div className="p-5 space-y-4">
          <div className="flex items-start gap-3 bg-amber-50 border border-amber-200 rounded-xl p-4">
            <AlertTriangle size={16} className="text-amber-500 shrink-0 mt-0.5" />
            <p className="text-sm text-amber-800">
              This will immediately stop the execution. Any in-progress nodes will be marked as cancelled and cannot be resumed.
            </p>
          </div>

          <div className="bg-slate-50 border border-slate-200 rounded-xl p-4">
            <p className="text-xs text-slate-400 mb-1">Execution ID</p>
            <p className="font-mono text-xs text-slate-700 break-all">{executionId}</p>
          </div>

          {error && (
            <p className="text-xs text-red-600 bg-red-50 border border-red-100 rounded-lg px-3 py-2">{error}</p>
          )}
        </div>

        {/* Footer */}
        <div className="flex gap-3 p-5 pt-0">
          <button
            onClick={onClose}
            disabled={loading}
            className="flex-1 px-4 py-2 border border-slate-200 rounded-lg text-sm text-slate-700 hover:bg-slate-50 disabled:opacity-50 transition-colors"
          >
            Keep Running
          </button>
          <button
            onClick={handleConfirm}
            disabled={loading}
            className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg text-sm font-medium hover:bg-red-700 disabled:opacity-50 flex items-center justify-center gap-2 transition-colors"
          >
            {loading ? <Loader2 size={14} className="animate-spin" /> : <XCircle size={14} />}
            {loading ? 'Cancelling…' : 'Cancel Execution'}
          </button>
        </div>
      </div>
    </div>
  );
}
