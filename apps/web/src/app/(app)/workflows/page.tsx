'use client';
import { useState } from 'react';
import Link from 'next/link';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { api } from '@/lib/api';
import type { Workflow } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { Plus, Search, GitBranch, Play, Copy, Loader2 } from 'lucide-react';
import { PageHeader, Button, EmptyState } from '@/components/ui';
import { RunWorkflowModal } from '@/components/RunWorkflowModal';

export default function WorkflowsPage() {
  const [search, setSearch] = useState('');
  const [selectedWorkflow, setSelectedWorkflow] = useState<Workflow | null>(null);
  const [cloningId, setCloningId] = useState<string | null>(null);
  const router = useRouter();
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({ queryKey: ['workflows', search], queryFn: () => api.workflows.list({ search }) });

  const cloneMutation = useMutation({
    mutationFn: (id: string) => api.workflows.clone(id),
    onMutate: (id) => setCloningId(id),
    onSettled: () => setCloningId(null),
    onSuccess: (cloned) => {
      qc.invalidateQueries({ queryKey: ['workflows'] });
      router.push(`/workflows/${cloned.id}/designer`);
    },
  });

  return (
    <div>
      <PageHeader
        title="Workflows"
        subtitle="Manage your AI workflow definitions"
        action={
          <Link href="/workflows/new">
            <Button><Plus size={15} />New Workflow</Button>
          </Link>
        }
      />

      {/* Search */}
      <div className="relative max-w-xs mb-6">
        <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
        <input
          className="w-full border border-slate-200 rounded-lg pl-9 pr-4 py-2 text-sm bg-white placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
          placeholder="Search workflows…"
          value={search}
          onChange={e => setSearch(e.target.value)}
        />
      </div>

      {/* List */}
      {isLoading ? (
        <div className="space-y-2">
          {[1,2,3].map(i => <div key={i} className="h-[72px] bg-slate-100 rounded-xl animate-pulse" />)}
        </div>
      ) : data?.items.length === 0 ? (
        <EmptyState
          icon={GitBranch}
          title="No workflows yet"
          subtitle="Create your first workflow to get started"
          action={<Link href="/workflows/new"><Button>New Workflow</Button></Link>}
        />
      ) : (
        <div className="space-y-2">
          {data?.items.map(w => (
            <div key={w.id} className="bg-white border border-slate-200 rounded-xl px-5 py-4 flex items-center justify-between hover:border-slate-300 transition-colors">
              <div className="min-w-0">
                <Link href={`/workflows/${w.id}`} className="text-sm font-medium text-slate-900 hover:text-indigo-600 transition-colors">{w.name}</Link>
                <p className="text-xs text-slate-500 mt-0.5 truncate">{w.description}</p>
              </div>
              <div className="flex items-center gap-3 shrink-0 ml-4">
                {w.activeVersion && (
                  <span className="text-xs text-slate-400 border border-slate-200 rounded px-2 py-0.5">v{w.activeVersion}</span>
                )}
                <span className="text-xs text-slate-400">{formatDate(w.updatedAt)}</span>
                <button
                  onClick={() => setSelectedWorkflow(w)}
                  disabled={!w.activeVersion}
                  title={!w.activeVersion ? 'No active version — save from the designer first' : 'Run workflow'}
                  className="flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-lg font-medium bg-emerald-600 hover:bg-emerald-700 text-white disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                >
                  <Play size={12} />Run
                </button>
                <button
                  onClick={() => cloneMutation.mutate(w.id)}
                  disabled={cloningId === w.id || !w.activeVersion}
                  title={!w.activeVersion ? 'No active version to clone' : 'Duplicate workflow'}
                  className="flex items-center gap-1.5 text-xs px-3 py-1.5 rounded-lg font-medium border border-slate-200 text-slate-600 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
                >
                  {cloningId === w.id ? <Loader2 size={12} className="animate-spin" /> : <Copy size={12} />}
                  Duplicate
                </button>
                <Link href={`/workflows/${w.id}/designer`}>
                  <Button variant="ghost" size="sm">Designer</Button>
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}

      {selectedWorkflow && (
        <RunWorkflowModal
          workflow={selectedWorkflow}
          onClose={() => setSelectedWorkflow(null)}
        />
      )}
    </div>
  );
}
