'use client';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, WorkflowVersionSummary } from '@/lib/api';
import { X, CheckCircle2, Clock, Eye, RotateCcw } from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';

interface Props {
  workflowId: string;
  currentVersionNumber?: number;
  onClose: () => void;
  /** Called when the user clicks "Load" on a version — passes the definition JSON */
  onLoadVersion: (definitionJson: string, versionNumber: number) => void;
  /** Called when a version is activated — so the toolbar version badge refreshes */
  onActivated: () => void;
}

/**
 * VersionHistoryPanel — side drawer listing all saved versions of a workflow.
 * Supports viewing a version's definition on canvas and restoring (activating) old versions.
 */
export function VersionHistoryPanel({ workflowId, currentVersionNumber, onClose, onLoadVersion, onActivated }: Props) {
  const { canEdit } = useAuth();
  const queryClient = useQueryClient();
  const [loadingVersionId, setLoadingVersionId] = useState<string | null>(null);

  const { data: versions, isLoading } = useQuery({
    queryKey: ['workflow-versions', workflowId],
    queryFn: () => api.workflows.listVersions(workflowId),
  });

  const activateMutation = useMutation({
    mutationFn: (versionId: string) => api.workflows.activateVersion(workflowId, versionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workflow-versions', workflowId] });
      onActivated();
    },
  });

  const handleLoad = async (v: WorkflowVersionSummary) => {
    setLoadingVersionId(v.id);
    try {
      const detail = await api.workflows.getVersion(workflowId, v.id);
      onLoadVersion(detail.definitionJson, v.versionNumber);
    } finally {
      setLoadingVersionId(null);
    }
  };

  return (
    <div className="w-80 border-l bg-white flex flex-col shrink-0 overflow-hidden shadow-lg">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b">
        <div>
          <h3 className="font-semibold text-sm text-slate-900">Version History</h3>
          <p className="text-xs text-slate-400 mt-0.5">Click Load to preview · Activate to set as current</p>
        </div>
        <button onClick={onClose} className="p-1 hover:bg-gray-100 rounded" title="Close">
          <X size={16} />
        </button>
      </div>

      {/* Version list */}
      <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
        {isLoading && (
          <div className="p-6 text-center text-sm text-slate-400">Loading versions…</div>
        )}
        {!isLoading && (!versions || versions.length === 0) && (
          <div className="p-6 text-center text-sm text-slate-400">No versions saved yet.</div>
        )}
        {versions?.map(v => {
          const isCurrentOnCanvas = v.versionNumber === currentVersionNumber;
          const isActivating = activateMutation.isPending && activateMutation.variables === v.id;
          const isLoading_ = loadingVersionId === v.id;

          return (
            <div key={v.id} className={`p-4 ${isCurrentOnCanvas ? 'bg-blue-50' : ''}`}>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  {v.isActive ? (
                    <CheckCircle2 size={14} className="text-emerald-500 shrink-0" />
                  ) : (
                    <Clock size={14} className="text-slate-300 shrink-0" />
                  )}
                  <span className="font-semibold text-sm text-slate-800">
                    v{v.versionNumber}
                    {v.isActive && <span className="ml-1.5 text-xs text-emerald-600 font-medium">active</span>}
                    {isCurrentOnCanvas && !v.isActive && (
                      <span className="ml-1.5 text-xs text-blue-500 font-medium">on canvas</span>
                    )}
                  </span>
                </div>
                <div className="flex gap-1">
                  {/* Load onto canvas */}
                  <button
                    className="flex items-center gap-1 text-xs px-2 py-1 rounded border text-slate-600 hover:bg-slate-50 disabled:opacity-40"
                    onClick={() => handleLoad(v)}
                    disabled={isLoading_}
                    title="Load this version onto the canvas (doesn't activate)"
                  >
                    <Eye size={11} />
                    {isLoading_ ? '…' : 'Load'}
                  </button>
                  {/* Activate — Editors/Admins only */}
                  {canEdit && !v.isActive && (
                    <button
                      className="flex items-center gap-1 text-xs px-2 py-1 rounded border text-emerald-700 hover:bg-emerald-50 disabled:opacity-40"
                      onClick={() => activateMutation.mutate(v.id)}
                      disabled={isActivating}
                      title="Set as the active version"
                    >
                      <RotateCcw size={11} />
                      {isActivating ? '…' : 'Activate'}
                    </button>
                  )}
                </div>
              </div>
              <p className="text-xs text-slate-400 mt-1 ml-5">
                {new Date(v.createdAt).toLocaleString()}
              </p>
            </div>
          );
        })}
      </div>
    </div>
  );
}
