'use client';
import { useParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { useExecutionStream } from '@/hooks/useExecutionStream';
import Link from 'next/link';
import { ArrowLeft } from 'lucide-react';
import { PageHeader, Badge, statusVariant } from '@/components/ui';

/**
 * ExecutionDetailPage - shows execution metadata, node timeline, and a live SignalR event log.
 */
export default function ExecutionDetailPage() {
  const { id } = useParams<{ id: string }>();
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

  if (!exec) return (
    <div className="space-y-4">
      {[1,2,3].map(i => <div key={i} className="h-16 bg-slate-100 rounded-xl animate-pulse" />)}
    </div>
  );

  return (
    <div className="space-y-6">
      <Link href="/executions" className="flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700 mb-4">
        <ArrowLeft size={16} /> Back to Executions
      </Link>
      <PageHeader
        title="Execution Timeline"
        subtitle={exec.id}
        action={<Badge variant={statusVariant(exec.status)}>{exec.status}</Badge>}
      />

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Started', value: formatDate(exec.startedAt) },
          { label: 'Completed', value: formatDate(exec.completedAt) },
          { label: 'Correlation ID', value: exec.correlationId?.slice(0, 16) + '...' },
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
                  <Badge variant={statusVariant(n.status)}>{n.status}</Badge>
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
                {evt.status && <span> - {evt.status}</span>}
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
