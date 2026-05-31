'use client';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, SecretSummary } from '@/lib/api';
import { Plus, Trash2, Eye, EyeOff, KeyRound, AlertCircle } from 'lucide-react';
import { PageHeader } from '@/components/ui';
import { AdminPageGuard } from '@/components/AdminPageGuard';

/**
 * SecretsPage — manage encrypted named secrets for use in workflow node config.
 * Secrets are referenced as {{secret:name}} in any config field.
 * Values are encrypted at rest and never returned by the API.
 */
export default function SecretsPage() {
  const queryClient = useQueryClient();
  const [newName, setNewName] = useState('');
  const [newValue, setNewValue] = useState('');
  const [showValue, setShowValue] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editValue, setEditValue] = useState('');
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);
  const [error, setError] = useState('');

  const { data: secrets, isLoading } = useQuery({
    queryKey: ['secrets'],
    queryFn: () => api.secrets.list(),
  });

  const createMutation = useMutation({
    mutationFn: () => api.secrets.create(newName.trim(), newValue),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['secrets'] });
      setNewName('');
      setNewValue('');
      setError('');
    },
    onError: (e: Error) => setError(e.message ?? 'Failed to create secret'),
  });

  const updateMutation = useMutation({
    mutationFn: (id: string) => api.secrets.update(id, { value: editValue }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['secrets'] });
      setEditingId(null);
      setEditValue('');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.secrets.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['secrets'] });
      setDeleteConfirm(null);
    },
  });

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName.trim() || !newValue) { setError('Name and value are required'); return; }
    if (!/^[a-zA-Z0-9_-]+$/.test(newName.trim())) {
      setError('Name may only contain letters, numbers, hyphens and underscores');
      return;
    }
    createMutation.mutate();
  };

  return (
    <AdminPageGuard>
    <div className="space-y-6">
      <PageHeader
        title="Secrets Vault"
        subtitle="Encrypted named values for use in workflow node config"
      />

      {/* Usage hint */}
      <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 flex gap-3">
        <KeyRound size={18} className="text-blue-500 shrink-0 mt-0.5" />
        <div>
          <p className="text-sm font-medium text-blue-800">How to use secrets in workflows</p>
          <p className="text-sm text-blue-600 mt-0.5">
            Reference a secret in any node config field using{' '}
            <code className="bg-blue-100 px-1 rounded font-mono text-xs">{'{{secret:my-secret-name}}'}</code>.
            The engine will substitute the encrypted value at execution time — it never appears in saved definitions.
          </p>
        </div>
      </div>

      {/* Create form */}
      <div className="bg-white border border-slate-200 rounded-xl p-5">
        <h3 className="text-sm font-semibold text-slate-900 mb-4">Add Secret</h3>
        <form onSubmit={handleCreate} className="flex gap-3 items-end flex-wrap">
          <div className="flex-1 min-w-48">
            <label className="block text-xs font-medium text-gray-700 mb-1">Name <span className="text-red-500">*</span></label>
            <input
              className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="e.g. my-api-key"
              value={newName}
              onChange={e => setNewName(e.target.value)}
            />
          </div>
          <div className="flex-1 min-w-48">
            <label className="block text-xs font-medium text-gray-700 mb-1">Value <span className="text-red-500">*</span></label>
            <div className="relative">
              <input
                className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 pr-9"
                type={showValue ? 'text' : 'password'}
                placeholder="Secret value (encrypted at rest)"
                value={newValue}
                onChange={e => setNewValue(e.target.value)}
              />
              <button type="button" className="absolute right-2 top-2.5 text-slate-400 hover:text-slate-600"
                onClick={() => setShowValue(v => !v)}>
                {showValue ? <EyeOff size={14} /> : <Eye size={14} />}
              </button>
            </div>
          </div>
          <button
            type="submit"
            disabled={createMutation.isPending}
            className="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm px-4 py-2 rounded-lg flex items-center gap-1.5"
          >
            <Plus size={14} /> {createMutation.isPending ? 'Adding…' : 'Add Secret'}
          </button>
        </form>
        {error && (
          <div className="mt-3 flex items-center gap-2 text-red-600 text-sm">
            <AlertCircle size={14} /> {error}
          </div>
        )}
      </div>

      {/* Secret list */}
      <div className="bg-white border border-slate-200 rounded-xl">
        <div className="p-5 border-b border-slate-100">
          <h3 className="text-sm font-semibold text-slate-900">
            Secrets {secrets && <span className="text-slate-400 font-normal ml-1">({secrets.length})</span>}
          </h3>
        </div>

        {isLoading && (
          <div className="p-6 text-center text-sm text-slate-400">Loading…</div>
        )}
        {!isLoading && (!secrets || secrets.length === 0) && (
          <div className="p-8 text-center">
            <KeyRound size={32} className="text-slate-200 mx-auto mb-2" />
            <p className="text-sm text-slate-400">No secrets yet. Add one above.</p>
          </div>
        )}

        <div className="divide-y divide-slate-100">
          {secrets?.map((s: SecretSummary) => (
            <div key={s.id} className="flex items-center gap-4 px-5 py-3">
              <div className="flex-1 min-w-0">
                <p className="text-sm font-mono font-medium text-slate-800">{s.name}</p>
                <p className="text-xs text-slate-400 mt-0.5">
                  Created {new Date(s.createdAt).toLocaleDateString()}
                  {s.updatedAt !== s.createdAt && ` · Updated ${new Date(s.updatedAt).toLocaleDateString()}`}
                </p>
              </div>

              <code className="text-xs text-slate-400 bg-slate-50 border rounded px-2 py-0.5 hidden sm:block">
                {`{{secret:${s.name}}}`}
              </code>

              {/* Inline update */}
              {editingId === s.id ? (
                <div className="flex gap-2 items-center">
                  <input
                    className="border rounded px-2 py-1 text-sm w-48 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    type="password"
                    placeholder="New value"
                    value={editValue}
                    onChange={e => setEditValue(e.target.value)}
                    autoFocus
                  />
                  <button
                    className="text-xs bg-blue-600 text-white px-3 py-1 rounded hover:bg-blue-700"
                    onClick={() => updateMutation.mutate(s.id)}
                    disabled={!editValue || updateMutation.isPending}
                  >
                    Save
                  </button>
                  <button className="text-xs text-slate-400 hover:text-slate-600" onClick={() => setEditingId(null)}>
                    Cancel
                  </button>
                </div>
              ) : (
                <button
                  className="text-xs text-blue-600 hover:text-blue-800 border border-blue-200 rounded px-2 py-1"
                  onClick={() => { setEditingId(s.id); setEditValue(''); }}
                >
                  Rotate
                </button>
              )}

              {/* Delete */}
              {deleteConfirm === s.id ? (
                <div className="flex gap-2 items-center">
                  <span className="text-xs text-red-600">Delete?</span>
                  <button
                    className="text-xs bg-red-600 text-white px-2 py-1 rounded hover:bg-red-700"
                    onClick={() => deleteMutation.mutate(s.id)}
                    disabled={deleteMutation.isPending}
                  >
                    Yes
                  </button>
                  <button className="text-xs text-slate-400 hover:text-slate-600" onClick={() => setDeleteConfirm(null)}>
                    No
                  </button>
                </div>
              ) : (
                <button
                  className="text-slate-300 hover:text-red-500 transition-colors"
                  onClick={() => setDeleteConfirm(s.id)}
                  title="Delete secret"
                >
                  <Trash2 size={15} />
                </button>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
    </AdminPageGuard>
  );
}
