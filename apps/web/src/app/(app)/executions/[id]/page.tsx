'use client';
import { useParams, useRouter } from 'next/navigation';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { useExecutionStream } from '@/hooks/useExecutionStream';
import Link from 'next/link';
import { ArrowLeft, XCircle, ClipboardCheck, RotateCcw } from 'lucide-react';
import { PageHeader, Badge, statusVariant, statusLabel } from '@/components/ui';
import { useState } from 'react';
import { CancelExecutionModal } from '@/components/CancelExecutionModal';
import { useAuth } from '@/contexts/AuthContext';

/**
 * ExecutionDetailPage - shows execution metadata, node timeline, and a live SignalR event log.
 */
export default function ExecutionDetailPage() {
  const { canEdit } = useAuth();
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [cancelling, setCancelling] = useState(false);
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [rerunning, setRerunning] = useState(false);
  const { data: exec } = useQuery({
    queryKey: ['execution', id],
    queryFn: () => api.executions.get(id),
    refetchInterval: 3000,
  });
  const { data: timeline } = useQuery({
    queryKey: ['timeline', id],
    queryFn: () => api.executions.timeline(id),
    refetchInterval: 3000,
  });

  const { events } = useExecutionStream(id ?? '');

  // If the execution is paused waiting for approval, fetch the pending approval for navigation
  const { data: pendingApproval } = useQuery({
    queryKey: ['execution-approval', id],
    queryFn: () => api.approvals.getByExecution(id),
    enabled: !!exec && exec.status === 'Paused',
    retry: false,
  });

  const isTerminal = exec && ['Completed', 'Failed', 'Cancelled'].includes(exec.status);
  const isActive = exec && ['Queued', 'Running', 'Paused'].includes(exec.status);

  async function handleRerun() {
    if (!id) return;
    setRerunning(true);
    try {
      const newExec = await api.executions.rerun(id);
      await queryClient.invalidateQueries({ queryKey: ['executions'] });
      router.push(`/executions/${newExec.id}`);
    } finally {
      setRerunning(false);
    }
  }

  async function handleCancel() {
    if (!id) return;
    setCancelling(true);
    try {
      await api.executions.cancel(id);
      await queryClient.invalidateQueries({ queryKey: ['execution', id] });
      await queryClient.invalidateQueries({ queryKey: ['executions'] });
    } finally {
      setCancelling(false);
    }
  }

  if (!exec) return (
    <div className="space-y-4">
      {[1,2,3].map(i => <div key={i} className="h-16 bg-slate-100 rounded-xl animate-pulse" />)}
    </div>
  );

  return (
    <>
      <div className="space-y-6">
      <Link href="/executions" className="flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700 mb-4">
        <ArrowLeft size={16} /> Back to Executions
      </Link>
      <PageHeader
        title="Execution Timeline"
        subtitle={[exec.workflowName ?? exec.workflowId.slice(0, 12) + '\u2026', exec.versionNumber != null ? `v${exec.versionNumber}` : null].filter(Boolean).join(' \u00b7 ')}
        action={
          <div className="flex items-center gap-3">
            <Badge variant={statusVariant(exec.status)}>{statusLabel(exec.status)}</Badge>
            {canEdit && isActive && (
              <button
                onClick={() => setShowCancelModal(true)}
                disabled={cancelling}
                className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-red-600 border border-red-300 rounded-lg hover:bg-red-50 disabled:opacity-50 transition-colors"
              >
                <XCircle size={14} />
                {cancelling ? 'Cancelling…' : 'Cancel Execution'}
              </button>
            )}
            {canEdit && isTerminal && (
              <button
                onClick={handleRerun}
                disabled={rerunning}
                className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-emerald-700 border border-emerald-300 rounded-lg hover:bg-emerald-50 disabled:opacity-50 transition-colors"
              >
                <RotateCcw size={14} />
                {rerunning ? 'Starting…' : 'Re-run'}
              </button>
            )}
          </div>
        }
      />

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Workflow', value: exec.workflowName ?? exec.workflowId.slice(0, 16) + '…' },
          { label: 'Version', value: exec.versionNumber != null ? `v${exec.versionNumber}` : '—' },
          { label: 'Started', value: formatDate(exec.startedAt) },
          { label: 'Completed', value: exec.completedAt ? formatDate(exec.completedAt) : '—' },
          { label: 'Correlation ID', value: exec.correlationId?.slice(0, 16) + '…' },
          { label: 'Execution ID', value: exec.id.slice(0, 16) + '…' },
        ].map(({ label, value }) => (
          <div key={label} className="bg-white border border-slate-200 rounded-xl p-4">
            <p className="text-xs text-slate-400">{label}</p>
            <p className="font-medium mt-1 text-sm">{value}</p>
          </div>
        ))}
      </div>

      {exec.errorMessage && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-4 text-red-700 text-sm">{exec.errorMessage}</div>
      )}

      {pendingApproval && (
        <Link
          href={`/approvals/${pendingApproval.id}`}
          className="flex items-center justify-between gap-4 bg-amber-50 border border-amber-200 rounded-xl p-4 hover:bg-amber-100 transition-colors group"
        >
          <div className="flex items-center gap-3">
            <ClipboardCheck size={18} className="text-amber-600 shrink-0" />
            <div>
              <p className="text-sm font-semibold text-amber-900">Waiting for Approval</p>
              <p className="text-xs text-amber-700 mt-0.5">This execution is paused pending a human decision.</p>
            </div>
          </div>
          <span className="text-xs font-medium text-amber-700 group-hover:underline shrink-0">Review Approval →</span>
        </Link>
      )}

      <div className="bg-white border border-slate-200 rounded-xl">
        <div className="p-5 border-b border-slate-100">
          <h3 className="text-sm font-semibold text-slate-900">Node Timeline</h3>
        </div>
        <div className="divide-y divide-slate-100">
          {timeline?.nodes.map((n, i) => (
            <div key={n.id} className="flex items-start gap-4 p-4">
              <div className="shrink-0 w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-sm font-bold text-slate-500">{i+1}</div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between gap-2">
                  <span className="font-medium text-sm text-slate-900">{n.nodeId}</span>
                  <div className="flex items-center gap-2">
                    <Badge variant={statusVariant(n.status)}>{statusLabel(n.status)}</Badge>
                    {n.status === 'WaitingForApproval' && pendingApproval && (
                      <Link
                        href={`/approvals/${pendingApproval.id}`}
                        className="flex items-center gap-1 text-xs text-amber-700 hover:underline font-medium"
                      >
                        <ClipboardCheck size={12} /> Review
                      </Link>
                    )}
                  </div>
                </div>
                <p className="text-xs text-slate-400 font-mono mt-0.5">{n.nodeType}</p>
                <div className="flex gap-4 mt-1 text-xs text-slate-400">
                  <span>Start: {formatDate(n.startedAt)}</span>
                  {n.completedAt && <span>End: {formatDate(n.completedAt)}</span>}
                  {n.retryCount > 0 && <span className="text-orange-500">Retries: {n.retryCount}</span>}
                </div>
                {n.errorMessage && <p className="mt-1 text-xs text-red-500">{n.errorMessage}</p>}
              </div>
            </div>
          ))}
          {!timeline?.nodes.length && (
            <p className="text-sm text-slate-400 text-center py-6">No node executions yet - refreshing every 3s...</p>
          )}
        </div>
      </div>

      <div className="bg-white border border-slate-200 rounded-xl">
        <div className="p-5 border-b border-slate-100 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-900">Live Event Log</h3>
          <span className="text-xs text-slate-400">{events.length} event{events.length !== 1 ? 's' : ''}</span>
        </div>
        <div className="p-4 space-y-2 font-mono text-xs max-h-64 overflow-y-auto">
          {events.length === 0 && (
            <p className="text-slate-400 text-center py-4">Waiting for live events...</p>
          )}
          {events.map((evt, i) => (
            <div key={i} className="flex items-start gap-3">
              <span className="text-slate-400 shrink-0">{new Date(evt.timestamp).toLocaleTimeString()}</span>
              <span className="shrink-0 font-semibold text-slate-700">{evt.type}</span>
              <span className="text-slate-600 truncate">
                {evt.nodeType && <span>{evt.nodeType}</span>}
                {evt.error && <span className="text-red-500"> - {evt.error}</span>}
                {evt.status && <span> - {statusLabel(evt.status)}</span>}
              </span>
            </div>
          ))}
        </div>
      </div>
      </div>

      {showCancelModal && exec && (
        <CancelExecutionModal
          executionId={exec.id}
          workflowName={exec.workflowName}
          versionNumber={exec.versionNumber}
          onConfirm={handleCancel}
          onClose={() => setShowCancelModal(false)}
        />
      )}
    </>
  );
}
