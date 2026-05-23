'use client';
import { useParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { statusColor, formatDate } from '@/lib/utils';

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

  if (!exec) return (
    <div className="p-8 space-y-4">
      {[1,2,3].map(i => <div key={i} className="h-16 bg-gray-200 rounded animate-pulse" />)}
    </div>
  );

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold">Execution Timeline</h2>
          <p className="font-mono text-sm text-gray-400 mt-1">{exec.id}</p>
        </div>
        <span className={`text-sm px-3 py-1.5 rounded-full font-semibold ${statusColor(exec.status)}`}>{exec.status}</span>
      </div>

      <div className="grid grid-cols-3 gap-4">
        {[
          { label: 'Started', value: formatDate(exec.startedAt) },
          { label: 'Completed', value: formatDate(exec.completedAt) },
          { label: 'Correlation ID', value: exec.correlationId?.slice(0, 16) + '…' },
        ].map(({ label, value }) => (
          <div key={label} className="bg-white border rounded-xl p-4">
            <p className="text-xs text-gray-400">{label}</p>
            <p className="font-medium mt-1 text-sm">{value}</p>
          </div>
        ))}
      </div>

      {exec.errorMessage && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-4 text-red-700 text-sm">{exec.errorMessage}</div>
      )}

      <div className="bg-white border rounded-xl">
        <div className="p-5 border-b"><h3 className="font-semibold">Node Timeline</h3></div>
        <div className="p-4 space-y-3">
          {timeline?.nodes.map((n, i) => (
            <div key={n.id} className="flex items-start gap-4 pb-3 border-b last:border-0">
              <div className="shrink-0 w-8 h-8 rounded-full bg-gray-100 flex items-center justify-center text-sm font-bold text-gray-500">{i+1}</div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between gap-2">
                  <span className="font-medium text-sm">{n.nodeId}</span>
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium shrink-0 ${statusColor(n.status)}`}>{n.status}</span>
                </div>
                <p className="text-xs text-gray-400 font-mono mt-0.5">{n.nodeType}</p>
                <div className="flex gap-4 mt-1 text-xs text-gray-400">
                  <span>Start: {formatDate(n.startedAt)}</span>
                  {n.completedAt && <span>End: {formatDate(n.completedAt)}</span>}
                  {n.retryCount > 0 && <span className="text-orange-500">Retries: {n.retryCount}</span>}
                </div>
                {n.errorMessage && <p className="mt-1 text-xs text-red-500">{n.errorMessage}</p>}
              </div>
            </div>
          ))}
          {!timeline?.nodes.length && (
            <p className="text-sm text-gray-400 text-center py-6">No node executions yet — refreshing every 3s…</p>
          )}
        </div>
      </div>
    </div>
  );
}
