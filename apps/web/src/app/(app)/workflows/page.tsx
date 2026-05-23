'use client';
import { useState } from 'react';
import Link from 'next/link';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { Plus, Search, GitBranch, Play } from 'lucide-react';

/**
 * WorkflowsPage — lists all workflows for the current tenant.
 * Supports live search filtering and navigation to create or open workflows.
 */
export default function WorkflowsPage() {
  const [search, setSearch] = useState('');
  // Re-fetch when search term changes; debounce handled server-side via query params
  const { data, isLoading } = useQuery({ queryKey: ['workflows', search], queryFn: () => api.workflows.list({ search }) });

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold">Workflows</h2>
          <p className="text-gray-500 mt-1">Manage your AI workflow definitions</p>
        </div>
        <Link href="/workflows/new">
          <button className="bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium px-4 py-2 rounded-lg flex items-center gap-2">
            <Plus size={16} />New Workflow
          </button>
        </Link>
      </div>

      <div className="relative max-w-sm">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
        <input className="w-full border rounded-lg pl-9 pr-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="Search workflows…" value={search} onChange={e => setSearch(e.target.value)} />
      </div>

      {isLoading ? (
        <div className="space-y-3">{[1,2,3].map(i => <div key={i} className="h-20 bg-gray-200 rounded-xl animate-pulse" />)}</div>
      ) : data?.items.length === 0 ? (
        <div className="text-center py-16 text-gray-400">
          <GitBranch size={48} className="mx-auto mb-4 opacity-30" />
          <p className="text-lg font-medium">No workflows yet</p>
          <p className="text-sm mt-1">Create your first workflow to get started</p>
        </div>
      ) : (
        <div className="space-y-3">
          {data?.items.map(w => (
            <div key={w.id} className="bg-white border rounded-xl p-5 hover:shadow-md transition-shadow flex items-center justify-between">
              <div>
                <Link href={`/workflows/${w.id}`} className="font-semibold text-lg hover:text-blue-600">{w.name}</Link>
                <p className="text-sm text-gray-500 mt-0.5">{w.description}</p>
              </div>
              <div className="flex items-center gap-3">
                {w.activeVersion && <span className="text-xs border rounded px-2 py-0.5 text-gray-500">v{w.activeVersion}</span>}
                <span className="text-xs text-gray-400">{formatDate(w.updatedAt)}</span>
                <Link href={`/workflows/${w.id}/designer`}>
                  <button className="border text-sm px-3 py-1.5 rounded-lg hover:bg-gray-50 text-gray-700">Open Designer</button>
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
