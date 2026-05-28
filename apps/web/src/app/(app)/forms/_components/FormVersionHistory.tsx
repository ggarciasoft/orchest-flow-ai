'use client';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, FormFieldDefinition, FormVersionSummary } from '@/lib/api';
import { History, CheckCircle, RotateCcw, ChevronDown, ChevronUp } from 'lucide-react';
import { Badge } from '@/components/ui';

interface FormVersionHistoryProps {
  formId: string;
  /** Called when the user activates a version — parent should reload form fields */
  onActivate?: (version: FormVersionSummary) => void;
}

/**
 * FormVersionHistory — shows a collapsible list of all versions of a form.
 * Allows activating a previous version (rolls back the live definition).
 * Mirrors the VersionHistoryPanel in the workflow designer.
 */
export default function FormVersionHistory({ formId, onActivate }: FormVersionHistoryProps) {
  const [open, setOpen] = useState(false);
  const qc = useQueryClient();

  const { data: versions, isLoading } = useQuery({
    queryKey: ['form-versions', formId],
    queryFn: () => api.forms.listVersions(formId),
    enabled: open,
  });

  const activateMutation = useMutation({
    mutationFn: (versionId: string) => api.forms.activateVersion(formId, versionId),
    onSuccess: (_, versionId) => {
      qc.invalidateQueries({ queryKey: ['form-versions', formId] });
      qc.invalidateQueries({ queryKey: ['forms', formId] });
      const v = versions?.find(v => v.id === versionId);
      if (v) onActivate?.(v);
    },
  });

  return (
    <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
      {/* Header toggle */}
      <button
        type="button"
        onClick={() => setOpen(v => !v)}
        className="w-full flex items-center gap-2 px-5 py-3 text-sm font-medium text-slate-700 hover:bg-slate-50 transition-colors"
      >
        <History size={15} className="text-slate-400" />
        Version History
        {open ? <ChevronUp size={14} className="ml-auto text-slate-400" /> : <ChevronDown size={14} className="ml-auto text-slate-400" />}
      </button>

      {open && (
        <div className="border-t border-slate-100">
          {isLoading && (
            <p className="text-xs text-slate-400 text-center py-4">Loading…</p>
          )}
          {!isLoading && (!versions || versions.length === 0) && (
            <p className="text-xs text-slate-400 text-center py-4">No versions yet.</p>
          )}
          <div className="divide-y divide-slate-100">
            {versions?.map(v => {
              const fieldCount = (() => {
                try { return (JSON.parse(v.fieldsJson) as FormFieldDefinition[]).length; } catch { return '?'; }
              })();
              return (
                <div key={v.id} className="flex items-center gap-3 px-5 py-3">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-medium text-slate-800">v{v.versionNumber}</span>
                      {v.isActive && <Badge variant="success">Active</Badge>}
                    </div>
                    <p className="text-xs text-slate-400 mt-0.5">
                      {new Date(v.createdAt).toLocaleString()} · {fieldCount} field{fieldCount !== 1 ? 's' : ''}
                    </p>
                  </div>
                  {!v.isActive && (
                    <button
                      onClick={() => activateMutation.mutate(v.id)}
                      disabled={activateMutation.isPending}
                      title="Activate this version"
                      className="flex items-center gap-1 text-xs text-indigo-600 hover:text-indigo-800 border border-indigo-200 rounded px-2 py-1 disabled:opacity-40"
                    >
                      <RotateCcw size={12} /> Activate
                    </button>
                  )}
                  {v.isActive && (
                    <CheckCircle size={15} className="text-emerald-500 shrink-0" />
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
