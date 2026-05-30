'use client';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, WorkflowConfigEntry } from '@/lib/api';
import { Plus, Trash2, Edit, X, Check, ChevronDown, ChevronUp, AlertCircle, Database } from 'lucide-react';
import { PageHeader } from '@/components/ui';
import { cn } from '@/lib/utils';

type ValueType = 'string' | 'number' | 'boolean' | 'json' | 'datetime';

const TYPE_COLORS: Record<ValueType, string> = {
  string: 'bg-slate-100 text-slate-700 border-slate-200',
  number: 'bg-blue-100 text-blue-700 border-blue-200',
  boolean: 'bg-green-100 text-green-700 border-green-200',
  json: 'bg-purple-100 text-purple-700 border-purple-200',
  datetime: 'bg-amber-100 text-amber-700 border-amber-200',
};

/**
 * ConfigurationPage — manage workflow configuration entries.
 * Persistent key-value store that workflows can read and write using config nodes.
 */
export default function ConfigurationPage() {
  const queryClient = useQueryClient();
  const [showAddForm, setShowAddForm] = useState(false);
  const [newKey, setNewKey] = useState('');
  const [newValue, setNewValue] = useState('');
  const [newValueType, setNewValueType] = useState<ValueType>('string');
  const [newDescription, setNewDescription] = useState('');
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [editValue, setEditValue] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [deleteConfirm, setDeleteConfirm] = useState<string | null>(null);
  const [error, setError] = useState('');

  const { data: entries, isLoading } = useQuery({
    queryKey: ['config'],
    queryFn: () => api.config.list(),
  });

  const createMutation = useMutation({
    mutationFn: () => api.config.create({
      key: newKey.trim(),
      value: newValue,
      valueType: newValueType,
      description: newDescription.trim() || undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['config'] });
      setNewKey('');
      setNewValue('');
      setNewValueType('string');
      setNewDescription('');
      setShowAddForm(false);
      setError('');
    },
    onError: (e: Error) => setError(e.message ?? 'Failed to create entry'),
  });

  const updateMutation = useMutation({
    mutationFn: (key: string) => api.config.update(key, {
      value: editValue,
      description: editDescription.trim() || undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['config'] });
      setEditingKey(null);
      setEditValue('');
      setEditDescription('');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (key: string) => api.config.delete(key),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['config'] });
      setDeleteConfirm(null);
    },
  });

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newKey.trim() || !newValue) {
      setError('Key and value are required');
      return;
    }
    if (!/^[a-zA-Z0-9_.-]+$/.test(newKey.trim())) {
      setError('Key may only contain letters, numbers, dots, hyphens and underscores');
      return;
    }
    createMutation.mutate();
  };

  const handleEdit = (entry: WorkflowConfigEntry) => {
    setEditingKey(entry.key);
    setEditValue(entry.value);
    setEditDescription(entry.description ?? '');
  };

  const handleCancelEdit = () => {
    setEditingKey(null);
    setEditValue('');
    setEditDescription('');
  };

  const truncateValue = (value: string, maxLength = 60) => {
    return value.length > maxLength ? value.substring(0, maxLength) + '...' : value;
  };

  const formatValueForDisplay = (entry: WorkflowConfigEntry) => {
    if (entry.valueType === 'json') {
      try {
        const parsed = JSON.parse(entry.value);
        return JSON.stringify(parsed);
      } catch {
        return entry.value;
      }
    }
    return entry.value;
  };

  const formatValueForTooltip = (entry: WorkflowConfigEntry) => {
    if (entry.valueType === 'json') {
      try {
        const parsed = JSON.parse(entry.value);
        return JSON.stringify(parsed, null, 2);
      } catch {
        return entry.value;
      }
    }
    return entry.value;
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Workflow Configuration"
        subtitle="Persistent key-value store for workflow state. Workflows can read and write these values using Read Config and Write Config nodes."
      />

      {/* Usage hint */}
      <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 flex gap-3">
        <Database size={18} className="text-blue-500 shrink-0 mt-0.5" />
        <div>
          <p className="text-sm font-medium text-blue-800">Shared workflow state</p>
          <p className="text-sm text-blue-600 mt-0.5">
            Store configuration values that persist across workflow executions. Use <strong>Read Config</strong> and <strong>Write Config</strong> nodes to interact with these values.
          </p>
        </div>
      </div>

      {/* Add entry form */}
      <div className="bg-white border border-slate-200 rounded-xl">
        <button
          onClick={() => setShowAddForm(v => !v)}
          className="w-full px-5 py-4 flex items-center justify-between hover:bg-slate-50 transition-colors"
        >
          <h3 className="text-sm font-semibold text-slate-900">Add Configuration Entry</h3>
          {showAddForm ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
        </button>

        {showAddForm && (
          <div className="border-t border-slate-200 p-5">
            <form onSubmit={handleCreate} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">
                    Key <span className="text-red-500">*</span>
                  </label>
                  <input
                    className="w-full border rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="e.g. gmail.last_sync_date"
                    value={newKey}
                    onChange={e => setNewKey(e.target.value)}
                  />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">
                    Value Type <span className="text-red-500">*</span>
                  </label>
                  <select
                    className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    value={newValueType}
                    onChange={e => setNewValueType(e.target.value as ValueType)}
                  >
                    <option value="string">String</option>
                    <option value="number">Number</option>
                    <option value="boolean">Boolean</option>
                    <option value="json">JSON</option>
                    <option value="datetime">DateTime</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  Value <span className="text-red-500">*</span>
                </label>
                <textarea
                  className="w-full border rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Value (supports multiline for JSON)"
                  rows={3}
                  value={newValue}
                  onChange={e => setNewValue(e.target.value)}
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  Description <span className="text-slate-400">(optional)</span>
                </label>
                <input
                  className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="Optional description"
                  value={newDescription}
                  onChange={e => setNewDescription(e.target.value)}
                />
              </div>

              {error && (
                <div className="flex items-center gap-2 text-red-600 text-sm">
                  <AlertCircle size={14} /> {error}
                </div>
              )}

              <div className="flex gap-2">
                <button
                  type="submit"
                  disabled={createMutation.isPending}
                  className="bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm px-4 py-2 rounded-lg flex items-center gap-1.5"
                >
                  <Plus size={14} /> {createMutation.isPending ? 'Saving…' : 'Save Entry'}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setShowAddForm(false);
                    setNewKey('');
                    setNewValue('');
                    setNewValueType('string');
                    setNewDescription('');
                    setError('');
                  }}
                  className="border border-slate-200 hover:bg-slate-50 text-slate-700 text-sm px-4 py-2 rounded-lg"
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>
        )}
      </div>

      {/* Entries table */}
      <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
        <div className="p-5 border-b border-slate-100">
          <h3 className="text-sm font-semibold text-slate-900">
            Configuration Entries {entries && <span className="text-slate-400 font-normal ml-1">({entries.length})</span>}
          </h3>
        </div>

        {isLoading && (
          <div className="p-6 text-center text-sm text-slate-400">Loading…</div>
        )}

        {!isLoading && (!entries || entries.length === 0) && (
          <div className="p-8 text-center">
            <Database size={32} className="text-slate-200 mx-auto mb-2" />
            <p className="text-sm text-slate-400">No configuration entries yet. Add your first entry to get started.</p>
          </div>
        )}

        {!isLoading && entries && entries.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  <th className="px-5 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Key</th>
                  <th className="px-5 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Type</th>
                  <th className="px-5 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Value</th>
                  <th className="px-5 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Description</th>
                  <th className="px-5 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Last Updated</th>
                  <th className="px-5 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {entries.map((entry: WorkflowConfigEntry) => {
                  const isEditing = editingKey === entry.key;
                  const isDeleting = deleteConfirm === entry.key;

                  return (
                    <tr key={entry.key} className={isEditing ? 'bg-blue-50' : ''}>
                      <td className="px-5 py-3 text-sm font-mono font-medium text-slate-800 whitespace-nowrap">
                        {entry.key}
                      </td>
                      <td className="px-5 py-3 whitespace-nowrap">
                        <span className={cn(
                          'inline-block px-2 py-0.5 rounded text-xs font-medium border',
                          TYPE_COLORS[entry.valueType]
                        )}>
                          {entry.valueType}
                        </span>
                      </td>

                      {isEditing ? (
                        <>
                          <td className="px-5 py-3" colSpan={2}>
                            <div className="space-y-2">
                              <textarea
                                className="w-full border rounded px-2 py-1 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-blue-500"
                                rows={2}
                                value={editValue}
                                onChange={e => setEditValue(e.target.value)}
                                autoFocus
                              />
                              <input
                                className="w-full border rounded px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                                placeholder="Description (optional)"
                                value={editDescription}
                                onChange={e => setEditDescription(e.target.value)}
                              />
                            </div>
                          </td>
                          <td className="px-5 py-3 text-xs text-slate-400 whitespace-nowrap">
                            {new Date(entry.updatedAt).toLocaleString()}
                          </td>
                          <td className="px-5 py-3 whitespace-nowrap">
                            <div className="flex gap-2">
                              <button
                                className="text-green-600 hover:text-green-800"
                                onClick={() => updateMutation.mutate(entry.key)}
                                disabled={updateMutation.isPending}
                                title="Save"
                              >
                                <Check size={16} />
                              </button>
                              <button
                                className="text-slate-400 hover:text-slate-600"
                                onClick={handleCancelEdit}
                                title="Cancel"
                              >
                                <X size={16} />
                              </button>
                            </div>
                          </td>
                        </>
                      ) : (
                        <>
                          <td
                            className="px-5 py-3 text-sm text-slate-600 font-mono max-w-xs truncate"
                            title={formatValueForTooltip(entry)}
                          >
                            {truncateValue(formatValueForDisplay(entry))}
                          </td>
                          <td className="px-5 py-3 text-sm text-slate-500 max-w-xs truncate">
                            {entry.description || <span className="text-slate-300">—</span>}
                          </td>
                          <td className="px-5 py-3 text-xs text-slate-400 whitespace-nowrap">
                            {new Date(entry.updatedAt).toLocaleString()}
                          </td>
                          <td className="px-5 py-3 whitespace-nowrap">
                            {isDeleting ? (
                              <div className="flex items-center gap-2">
                                <span className="text-xs text-red-600">Delete?</span>
                                <button
                                  className="text-xs bg-red-600 text-white px-2 py-1 rounded hover:bg-red-700"
                                  onClick={() => deleteMutation.mutate(entry.key)}
                                  disabled={deleteMutation.isPending}
                                >
                                  Yes
                                </button>
                                <button
                                  className="text-xs text-slate-400 hover:text-slate-600"
                                  onClick={() => setDeleteConfirm(null)}
                                >
                                  No
                                </button>
                              </div>
                            ) : (
                              <div className="flex gap-2">
                                <button
                                  className="text-blue-600 hover:text-blue-800"
                                  onClick={() => handleEdit(entry)}
                                  title="Edit"
                                >
                                  <Edit size={16} />
                                </button>
                                <button
                                  className="text-slate-300 hover:text-red-500 transition-colors"
                                  onClick={() => setDeleteConfirm(entry.key)}
                                  title="Delete"
                                >
                                  <Trash2 size={16} />
                                </button>
                              </div>
                            )}
                          </td>
                        </>
                      )}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
