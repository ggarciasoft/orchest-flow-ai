'use client';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { api, WorkflowForm } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { ClipboardList, Plus, Pencil, Trash2, Copy } from 'lucide-react';
import { PageHeader, EmptyState, Badge } from '@/components/ui';

/**
 * FormsPage — lists all custom forms for the tenant.
 * Allows creating, editing, deleting, and copying the form's node type.
 */
export default function FormsPage() {
  const router = useRouter();
  const qc = useQueryClient();
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);
  const [copied, setCopied] = useState<string | null>(null);

  const { data: forms, isLoading } = useQuery<WorkflowForm[]>({
    queryKey: ['forms'],
    queryFn: () => api.forms.list(),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.forms.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['forms'] });
      setConfirmDelete(null);
    },
  });

  const handleCopy = (slug: string) => {
    navigator.clipboard.writeText(`form.${slug}`);
    setCopied(slug);
    setTimeout(() => setCopied(null), 2000);
  };

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <PageHeader title="Forms" subtitle="Custom data-collection forms for workflow pauses" />
        <button
          onClick={() => router.push('/forms/new')}
          className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors"
        >
          <Plus size={16} />
          New Form
        </button>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3].map(i => (
            <div key={i} className="h-40 bg-gray-200 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : forms?.length === 0 ? (
        <EmptyState
          icon={ClipboardList}
          title="No forms yet"
          subtitle="Create a form to collect user input during workflow execution"
        />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {forms?.map(form => (
            <div
              key={form.id}
              className="bg-white border border-slate-200 rounded-xl p-5 space-y-3 hover:shadow-sm transition-shadow"
            >
              <div className="flex items-start justify-between">
                <h3 className="font-semibold text-slate-900">{form.name}</h3>
                <Badge variant="default">{form.fields.length} field{form.fields.length !== 1 ? 's' : ''}</Badge>
              </div>

              {form.description && (
                <p className="text-sm text-slate-500 line-clamp-2">{form.description}</p>
              )}

              <button
                onClick={() => handleCopy(form.slug)}
                className="inline-flex items-center gap-1.5 text-xs text-indigo-600 hover:text-indigo-800 font-mono bg-indigo-50 px-2 py-1 rounded transition-colors"
                title="Copy node type to clipboard"
              >
                <Copy size={11} />
                {copied === form.slug ? 'Copied!' : `form.${form.slug}`}
              </button>

              <div className="text-xs text-slate-400">
                Created {formatDate(form.createdAt)}
              </div>

              <div className="flex gap-2 pt-1">
                <button
                  onClick={() => router.push(`/forms/${form.id}`)}
                  className="flex items-center gap-1.5 text-sm text-slate-600 hover:text-indigo-700 px-3 py-1.5 rounded-lg hover:bg-indigo-50 transition-colors"
                >
                  <Pencil size={13} /> Edit
                </button>
                <button
                  onClick={() => setConfirmDelete(form.id)}
                  className="flex items-center gap-1.5 text-sm text-slate-600 hover:text-red-700 px-3 py-1.5 rounded-lg hover:bg-red-50 transition-colors"
                >
                  <Trash2 size={13} /> Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Delete confirmation modal */}
      {confirmDelete && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-6 w-full max-w-sm shadow-xl space-y-4">
            <h2 className="font-semibold text-lg text-slate-900">Delete Form?</h2>
            <p className="text-sm text-slate-500">This action cannot be undone. Any workflows using this form will break.</p>
            <div className="flex gap-3 justify-end">
              <button
                onClick={() => setConfirmDelete(null)}
                className="px-4 py-2 text-sm rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                onClick={() => deleteMutation.mutate(confirmDelete)}
                disabled={deleteMutation.isPending}
                className="px-4 py-2 text-sm rounded-lg bg-red-600 text-white hover:bg-red-700 disabled:opacity-50"
              >
                {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
