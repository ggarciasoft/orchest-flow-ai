'use client';
import { use, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import Link from 'next/link';
import { ArrowLeft, Play, XCircle, History } from 'lucide-react';
import { api, WorkflowExecution } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { PageHeader, Badge, statusVariant, statusLabel, EmptyState, Pagination } from '@/components/ui';
import { CancelExecutionModal } from '@/components/CancelExecutionModal';

const PAGE_SIZE = 20;
const STATUS_OPTIONS = ['', 'Running', 'Queued', 'Paused', 'Completed', 'Failed', 'Cancelled'] as const;

export default function WorkflowExecutionsPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [cancelTarget, setCancelTarget] = useState<WorkflowExecution | null>(null);

  const { data: workflow } = useQuery({
    queryKey: ['workflow', id],
    queryFn: () => api.workflows.get(id),
  });

  const { data, isLoading } = useQuery({
    queryKey: ['executions', 'by-workflow', id, page, status],
    queryFn: () => api.executions.list({
      workflowId: id,
      status: status || undefined,
      page,
      pageSize: PAGE_SIZE,
    }),
    refetchInterval: 10_000,
  });

  async function handleCancel(execId: string) {
    await api.executions.cancel(execId);
    await qc.invalidateQueries({ queryKey: ['executions', 'by-workflow', id] });
  }

  return (
    <div>
      <div className="mb-4">
        <Link
          href="/workflows"
          className="flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700 w-fit"
        >
          <ArrowLeft size={14} /> Back to Workflows
        </Link>
      </div>

      <PageHeader
        title={workflow ? `${workflow.name} — Execution History` : 'Execution History'}
        subtitle="All runs for this workflow"
        action={
          workflow?.activeVersion ? (
            <Link
              href={`/workflows/${id}/designer`}
              className="text-sm text-slate-600 border border-slate-200 rounded-lg px-3 py-1.5 hover:bg-slate-50 transition-colors"
            >
              Open Designer
            </Link>
          ) : undefined
        }
      />

      {/* Status filter */}
      <div className="flex gap-1.5 flex-wrap mb-5">
        {STATUS_OPTIONS.map(s => (
          <button
            key={s}
            onClick={() => { setStatus(s); setPage(1); }}
            className={`px-3 py-1.5 rounded-lg text-xs font-medium border transition-colors ${
              status === s
                ? 'bg-indigo-600 text-white border-indigo-600'
                : 'bg-white text-slate-600 border-slate-200 hover:bg-slate-50'
            }`}
          >
            {s || 'All'}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {[1, 2, 3].map(i => (
            <div key={i} className="h-16 bg-slate-100 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : data?.items.length === 0 ? (
        <EmptyState
          icon={History}
          title="No executions yet"
          subtitle={status ? 'No executions match this status filter' : 'Run this workflow to see its execution history here'}
        />
      ) : (
        <>
          <div className="bg-white border border-slate-200 rounded-xl divide-y divide-slate-100">
            {data?.items.map(exec => {
              const canCancel = ['Running', 'Queued', 'Paused'].includes(exec.status);
              return (
                <div key={exec.id} className="flex items-center justify-between px-5 py-3 hover:bg-slate-50 transition-colors">
                  <div className="flex items-center gap-4 min-w-0">
                    <Badge variant={statusVariant(exec.status)}>{statusLabel(exec.status)}</Badge>
                    <div className="min-w-0">
                      <Link
                        href={`/executions/${exec.id}`}
                        className="text-sm font-mono text-slate-700 hover:text-indigo-600 transition-colors truncate block"
                      >
                        {exec.id.slice(0, 20)}…
                      </Link>
                      <p className="text-xs text-slate-400 mt-0.5">
                        Started {formatDate(exec.startedAt)}
                        {exec.completedAt && ` · Ended ${formatDate(exec.completedAt)}`}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2 shrink-0 ml-4">
                    <Link
                      href={`/executions/${exec.id}`}
                      className="text-xs text-indigo-600 hover:text-indigo-800 font-medium px-3 py-1.5 rounded-lg border border-indigo-200 hover:bg-indigo-50 transition-colors flex items-center gap-1.5"
                    >
                      <Play size={11} /> View
                    </Link>
                    {canCancel && (
                      <button
                        onClick={() => setCancelTarget(exec)}
                        className="text-xs text-red-600 hover:text-red-800 font-medium px-3 py-1.5 rounded-lg border border-red-200 hover:bg-red-50 transition-colors flex items-center gap-1.5"
                      >
                        <XCircle size={11} /> Cancel
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
          <Pagination page={page} pageSize={PAGE_SIZE} total={data?.total ?? 0} onPage={setPage} />
        </>
      )}

      {cancelTarget && (
        <CancelExecutionModal
          executionId={cancelTarget.id}
          onConfirm={() => handleCancel(cancelTarget.id)}
          onClose={() => setCancelTarget(null)}
        />
      )}
    </div>
  );
}
