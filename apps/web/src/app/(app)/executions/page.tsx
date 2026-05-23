'use client';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import Link from 'next/link';
import { statusColor, formatDate } from '@/lib/utils';

export default function ExecutionsPage() {
  const { data, isLoading } = useQuery({ queryKey: ['executions'], queryFn: () => api.executions.list() });

  return (
    <div className="p-8 space-y-6">
      <div>
        <h2 className="text-3xl font-bold">Executions</h2>
        <p className="text-gray-500 mt-1">All workflow runs</p>
      </div>
      {isLoading ? (
        <div className="space-y-3">{[1,2,3].map(i => <div key={i} className="h-16 bg-gray-200 rounded-xl animate-pulse" />)}</div>
      ) : (
        <div className="space-y-3">
          {data?.items.map(e => (
            <div key={e.id} className="bg-white border rounded-xl p-4 hover:shadow-md transition-shadow flex items-center justify-between">
              <div>
                <Link href={`/executions/${e.id}`} className="font-mono text-sm hover:text-blue-600">{e.id.slice(0,20)}…</Link>
                <p className="text-xs text-gray-400 mt-0.5">Workflow: {e.workflowId.slice(0,12)}…</p>
              </div>
              <div className="flex items-center gap-4">
                <span className={`text-xs px-2 py-1 rounded-full font-medium ${statusColor(e.status)}`}>{e.status}</span>
                <span className="text-xs text-gray-400">{formatDate(e.startedAt)}</span>
                <Link href={`/executions/${e.id}`}>
                  <button className="text-xs text-blue-600 hover:underline">View Timeline →</button>
                </Link>
              </div>
            </div>
          )) ?? null}
          {!data?.items.length && (
            <div className="text-center py-16 text-gray-400">
              <p className="text-lg font-medium">No executions yet</p>
              <p className="text-sm mt-1">Execute a workflow to see runs here</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
