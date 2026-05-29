'use client';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, WorkflowExecution } from '@/lib/api';
import Link from 'next/link';
import { formatDate } from '@/lib/utils';
import { Play, XCircle } from 'lucide-react';
import { PageHeader, Badge, statusVariant, statusLabel, EmptyState, Pagination, SearchInput } from '@/components/ui';
import { useState } from 'react';
import { CancelExecutionModal } from '@/components/CancelExecutionModal';
import { useDebounce } from '@/hooks/useDebounce';

const PAGE_SIZE = 20;
const STATUS_OPTIONS = ['', 'Running', 'Queued', 'Paused', 'Completed', 'Failed', 'Cancelled'] as const;

export default function ExecutionsPage() {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState('');
  const [searchRaw, setSearchRaw] = useState('');
  const search = useDebounce(searchRaw, 350);
  const [cancelTarget, setCancelTarget] = useState<WorkflowExecution | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['executions', { page, status, search }],
    queryFn: () => api.executions.list({ status: status || undefined, search: search || undefined, page, pageSize: PAGE_SIZE }),
    refetchInterval: 10_000,
  });

  function handleFilterChange(newStatus: string) {
    setStatus(newStatus);
    setPage(1);
  }

  function handleSearch(v: string) {
    setSearchRaw(v);
    setPage(1);
  }

  async function handleCancel(id: string) {
    await api.executions.cancel(id);
    await queryClient.invalidateQueries({ queryKey: ['executions'] });
  }

  return (
    <div>
      <PageHeader title="Executions" subtitle="All workflow runs" />

      {/* Filters row */}
      <div className="flex flex-wrap items-center gap-3 mb-5">
        <SearchInput
          value={searchRaw}
          onChange={handleSearch}
          placeholder="Search by correlation ID…"
          className="w-64"
        />
        <div className="flex gap-1.5 flex-wrap">
          {STATUS_OPTIONS.map(s => (
            <button
              key={s}
              onClick={() => handleFilterChange(s)}
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
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {[1,2,3].map(i => <div key={i} className="h-16 bg-slate-100 rounded-xl animate-pulse" />)}
        </div>
      ) : !data?.items.length ? (
        <EmptyState
          icon={Play}
          title="No executions found"
          subtitle={status || search ? 'Try adjusting your filters' : 'Execute a workflow to see runs here'}
        />
      ) : (
        <>
          <div className="bg-white border border-slate-200 rounded-xl divide-y divide-slate-100">
            {data.items.map(e => (
              <div key={e.id} className="px-5 py-4 flex items-center justify-between hover:bg-slate-50 transition-colors">
                <div>
                  <Link href={`/executions/${e.id}`} className="text-sm font-medium text-slate-900 hover:text-indigo-600 transition-colors">
                    {e.workflowName ?? e.workflowId.slice(0, 12) + '…'}
                  </Link>
                  <div className="flex items-center gap-2 mt-0.5">
                    {e.versionNumber != null && (
                      <span className="text-xs text-slate-400 border border-slate-200 rounded px-1.5 py-0.5">v{e.versionNumber}</span>
                    )}
                    <span className="font-mono text-xs text-slate-400">{e.id.slice(0, 16)}…</span>
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <Badge variant={statusVariant(e.status)}>{statusLabel(e.status)}</Badge>
                  <span className="text-xs text-slate-400">{formatDate(e.startedAt)}</span>
                  {['Queued', 'Running', 'Paused'].includes(e.status) && (
                    <button
                      onClick={() => setCancelTarget(e)}
                      title="Cancel execution"
                      className="text-red-400 hover:text-red-600 transition-colors"
                    >
                      <XCircle size={16} />
                    </button>
                  )}
                  <Link href={`/executions/${e.id}`}>
                    <button className="text-xs text-indigo-600 hover:underline">View Timeline →</button>
                  </Link>
                </div>
              </div>
            ))}
          </div>
          <Pagination page={page} pageSize={PAGE_SIZE} total={data.total} onPage={setPage} />
        </>
      )}

      {cancelTarget && (
        <CancelExecutionModal
          executionId={cancelTarget.id}
          workflowName={cancelTarget.workflowName}
          versionNumber={cancelTarget.versionNumber}
          onConfirm={() => handleCancel(cancelTarget.id)}
          onClose={() => setCancelTarget(null)}
        />
      )}
    </div>
  );
}
