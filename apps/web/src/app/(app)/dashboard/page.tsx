'use client';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { GitBranch, Play, CheckSquare, AlertCircle } from 'lucide-react';
import { PageHeader, Badge, statusVariant } from '@/components/ui';

/**
 * DashboardPage — landing overview showing key workflow stats,
 * recent executions, and pending approvals.
 */
export default function DashboardPage() {
  const { data: workflows } = useQuery({ queryKey: ['workflows'], queryFn: () => api.workflows.list() });
  const { data: executions } = useQuery({ queryKey: ['executions'], queryFn: () => api.executions.list({ page: 1 }) });
  const { data: approvals } = useQuery({ queryKey: ['approvals', 'Pending'], queryFn: () => api.approvals.list('Pending') });

  /**
   * Data array for displaying summary statistics at the top of the dashboard.
   */
  const stats = [
    { label: 'Total Workflows', value: workflows?.total ?? 0, icon: GitBranch, color: 'text-indigo-600 bg-indigo-50' },
    { label: 'Executions', value: executions?.total ?? 0, icon: Play, color: 'text-emerald-600 bg-emerald-50' },
    { label: 'Pending Approvals', value: approvals?.total ?? 0, icon: CheckSquare, color: 'text-amber-600 bg-amber-50' },
    { label: 'Failed', value: executions?.items.filter(e => e.status === 'Failed').length ?? 0, icon: AlertCircle, color: 'text-red-600 bg-red-50' },
  ];

  return (
    <div>
      <PageHeader title="Dashboard" subtitle="Overview of your AI workflows" />

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {stats.map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="bg-white border border-slate-200 rounded-xl p-5">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-medium text-slate-500 uppercase tracking-wide">{label}</span>
              <div className={`p-2 rounded-lg ${color}`}><Icon size={14} /></div>
            </div>
            <p className="text-2xl font-semibold text-slate-900">{value}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Executions */}
        <div className="bg-white border border-slate-200 rounded-xl">
          <div className="px-5 py-4 border-b border-slate-200">
            <h2 className="text-sm font-semibold text-slate-900">Recent Executions</h2>
          </div>
          <div className="divide-y divide-slate-100">
            {executions?.items.slice(0, 5).map(e => (
              <div key={e.id} className="flex items-center justify-between px-5 py-3">
                <span className="text-xs font-mono text-slate-500">{e.id.slice(0, 12)}…</span>
                <Badge variant={statusVariant(e.status)}>{e.status}</Badge>
                <span className="text-xs text-slate-400">{formatDate(e.startedAt)}</span>
              </div>
            )) ?? (
              <p className="text-sm text-slate-400 text-center py-8">No executions yet</p>
            )}
          </div>
        </div>

        {/* Pending Approvals */}
        <div className="bg-white border border-slate-200 rounded-xl">
          <div className="px-5 py-4 border-b border-slate-200">
            <h2 className="text-sm font-semibold text-slate-900">Pending Approvals</h2>
          </div>
          <div className="divide-y divide-slate-100">
            {approvals?.items.slice(0, 5).map(a => (
              <div key={a.id} className="flex items-center justify-between px-5 py-3">
                <span className="text-xs font-mono text-slate-500">{a.id.slice(0, 12)}…</span>
                <Badge variant="warning">Pending</Badge>
                <span className="text-xs text-slate-400">{formatDate(a.requestedAt)}</span>
              </div>
            )) ?? (
              <p className="text-sm text-slate-400 text-center py-8">No pending approvals</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}