'use client';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { GitBranch, Play, CheckSquare, AlertCircle, ArrowRight } from 'lucide-react';
import { PageHeader, Badge, statusVariant } from '@/components/ui';
import Link from 'next/link';

/**
 * DashboardPage — landing overview showing key workflow stats,
 * recent executions, and pending tasks.
 */
export default function DashboardPage() {
  const { data: workflows } = useQuery({ queryKey: ['workflows'], queryFn: () => api.workflows.list() });
  const { data: executions } = useQuery({ queryKey: ['executions'], queryFn: () => api.executions.list({ page: 1 }) });
  const { data: approvals }  = useQuery({ queryKey: ['approvals', 'Pending'], queryFn: () => api.approvals.list('Pending') });

  const stats = [
    { label: 'Total Workflows', value: workflows?.total ?? 0,                                                icon: GitBranch,  color: 'text-indigo-600 bg-indigo-50', href: '/workflows'  },
    { label: 'Executions',      value: executions?.total ?? 0,                                               icon: Play,       color: 'text-emerald-600 bg-emerald-50', href: '/executions' },
    { label: 'Pending Tasks',   value: approvals?.total ?? 0,                                                icon: CheckSquare, color: 'text-amber-600 bg-amber-50',   href: '/approvals'  },
    { label: 'Failed',          value: executions?.items.filter(e => e.status === 'Failed').length ?? 0,     icon: AlertCircle, color: 'text-red-600 bg-red-50',       href: '/executions' },
  ];

  return (
    <div>
      <PageHeader title="Dashboard" subtitle="Overview of your AI workflows" />

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        {stats.map(({ label, value, icon: Icon, color, href }) => (
          <Link key={label} href={href} className="bg-white border border-slate-200 rounded-xl p-5 hover:border-indigo-300 hover:shadow-sm transition-all">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-medium text-slate-500 uppercase tracking-wide">{label}</span>
              <div className={`p-2 rounded-lg ${color}`}><Icon size={14} /></div>
            </div>
            <p className="text-2xl font-semibold text-slate-900">{value}</p>
          </Link>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent Executions */}
        <div className="bg-white border border-slate-200 rounded-xl">
          <div className="px-5 py-4 border-b border-slate-200 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-slate-900">Recent Executions</h2>
            <Link href="/executions" className="text-xs text-indigo-600 hover:text-indigo-800 flex items-center gap-1">
              View all <ArrowRight size={11} />
            </Link>
          </div>
          <div className="divide-y divide-slate-100">
            {executions?.items.length === 0 ? (
              <p className="text-sm text-slate-400 text-center py-8">No executions yet</p>
            ) : executions?.items.slice(0, 5).map(e => (
              <Link
                key={e.id}
                href={`/executions/${e.id}`}
                className="flex items-center justify-between px-5 py-3 hover:bg-slate-50 transition-colors"
              >
                <span className="text-xs text-slate-700 font-medium truncate max-w-[140px]">
                  {e.workflowName ?? e.id.slice(0, 12) + '…'}
                </span>
                <Badge variant={statusVariant(e.status)}>{e.status}</Badge>
                <span className="text-xs text-slate-400">{formatDate(e.startedAt)}</span>
              </Link>
            ))}
          </div>
        </div>

        {/* Pending Tasks */}
        <div className="bg-white border border-slate-200 rounded-xl">
          <div className="px-5 py-4 border-b border-slate-200 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-slate-900">Pending Tasks</h2>
            <Link href="/approvals" className="text-xs text-indigo-600 hover:text-indigo-800 flex items-center gap-1">
              View all <ArrowRight size={11} />
            </Link>
          </div>
          <div className="divide-y divide-slate-100">
            {approvals?.items.length === 0 ? (
              <p className="text-sm text-slate-400 text-center py-8">No pending tasks</p>
            ) : approvals?.items.slice(0, 5).map(a => {
              let payload: Record<string, unknown> = {};
              try { payload = JSON.parse(a.payloadJson); } catch { /* ignore */ }
              const title = String(payload._approvalTitle ?? payload._formName ?? payload.title ?? a.id.slice(0, 12) + '…');
              return (
                <Link
                  key={a.id}
                  href={`/approvals/${a.id}`}
                  className="flex items-center justify-between px-5 py-3 hover:bg-slate-50 transition-colors"
                >
                  <span className="text-xs text-slate-700 font-medium truncate max-w-[160px]">{title}</span>
                  <span className="text-xs text-slate-500 truncate max-w-[100px]">{a.workflowName ?? ''}</span>
                  <span className="text-xs text-slate-400">{formatDate(a.requestedAt)}</span>
                </Link>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}
