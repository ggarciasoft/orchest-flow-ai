'use client';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { statusColor, formatDate } from '@/lib/utils';
import { GitBranch, Play, CheckSquare, AlertCircle } from 'lucide-react';

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
    { label: 'Total Workflows', value: workflows?.total ?? 0, icon: GitBranch, color: 'text-blue-600 bg-blue-50' },
    { label: 'Executions', value: executions?.total ?? 0, icon: Play, color: 'text-green-600 bg-green-50' },
    { label: 'Pending Approvals', value: approvals?.total ?? 0, icon: CheckSquare, color: 'text-yellow-600 bg-yellow-50' },
    { label: 'Failed', value: executions?.items.filter(e => e.status === 'Failed').length ?? 0, icon: AlertCircle, color: 'text-red-600 bg-red-50' },
  ];

  return (
    <div className="p-8 space-y-8">
      <div>
        <h2 className="text-3xl font-bold text-gray-900">Dashboard</h2>
        <p className="text-gray-500 mt-1">Overview of your AI workflows</p>
      </div>

      {/* Render the stats summary cards */}
      <div className="grid grid-cols-4 gap-6">
        {stats.map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="bg-white rounded-xl border p-6 flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-500">{label}</p>
              <p className="text-3xl font-bold mt-1">{value}</p>
            </div>
            <div className={`p-3 rounded-xl ${color}`}><Icon size={24} /></div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-2 gap-6">
        {/* Section for recent executions */}
        <div className="bg-white rounded-xl border">
          <div className="p-5 border-b"><h3 className="font-semibold">Recent Executions</h3></div>
          <div className="p-4 space-y-2">
            {executions?.items.slice(0, 5).map(e => (
              <div key={e.id} className="flex items-center justify-between py-2">
                <span className="text-sm font-mono text-gray-600">{e.id.slice(0, 12)}…</span>
                <span className={`text-xs px-2 py-1 rounded-full font-medium ${statusColor(e.status)}`}>{e.status}</span>
                <span className="text-xs text-gray-400">{formatDate(e.startedAt)}</span>
              </div>
            )) ?? <p className="text-sm text-gray-400 py-4 text-center">No executions yet</p>}
          </div>
        </div>

        {/* Section for pending approvals */}
        <div className="bg-white rounded-xl border">
          <div className="p-5 border-b"><h3 className="font-semibold">Pending Approvals</h3></div>
          <div className="p-4 space-y-2">
            {approvals?.items.slice(0, 5).map(a => (
              <div key={a.id} className="flex items-center justify-between py-2">
                <span className="text-sm font-mono text-gray-600">{a.id.slice(0, 12)}…</span>
                <span className="text-xs px-2 py-1 rounded-full font-medium bg-yellow-100 text-yellow-800">Pending</span>
                <span className="text-xs text-gray-400">{formatDate(a.requestedAt)}</span>
              </div>
            )) ?? <p className="text-sm text-gray-400 py-4 text-center">No pending approvals</p>}
          </div>
        </div>
      </div>
    </div>
  );
}