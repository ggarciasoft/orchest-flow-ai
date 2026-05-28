'use client';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import Link from 'next/link';
import { formatDate } from '@/lib/utils';
import { Play } from 'lucide-react';
import { PageHeader, Badge, statusVariant, statusLabel, EmptyState } from '@/components/ui';

export default function ExecutionsPage() {
  const { data, isLoading } = useQuery({ queryKey: ['executions'], queryFn: () => api.executions.list() });

  return (
    <div>
      <PageHeader
        title="Executions"
        subtitle="All workflow runs"
      />

      {isLoading ? (
        <div className="space-y-2">
          {[1,2,3].map(i => <div key={i} className="h-16 bg-slate-100 rounded-xl animate-pulse" />)}
        </div>
      ) : !data?.items.length ? (
        <EmptyState
          icon={Play}
          title="No executions yet"
          subtitle="Execute a workflow to see runs here"
        />
      ) : (
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
                <Link href={`/executions/${e.id}`}>
                  <button className="text-xs text-indigo-600 hover:underline">View Timeline →</button>
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
