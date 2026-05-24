"use client";

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, PresetResponse } from '@/lib/api';
import { PageHeader, Badge, Button } from '@/components/ui';

/**
 * PresetsPage — Allows users to create, edit, delete, and view configuration presets.
 */
export default function PresetsPage() {
  const queryClient = useQueryClient();
  const { data: presets, isLoading } = useQuery({ queryKey: ['presets'], queryFn: () => api.presets.list() });
  const [newPreset, setNewPreset] = useState({ name: '', nodeType: '', configJson: '' });
  const [editingPreset, setEditingPreset] = useState<PresetResponse | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);
  const [showNewForm, setShowNewForm] = useState(false);

  const createPresetMutation = useMutation({
    mutationFn: (data: { name: string; nodeType: string; configJson: string }) => api.presets.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['presets'] });
      setNewPreset({ name: '', nodeType: '', configJson: '' });
      setShowNewForm(false);
    },
  });

  const updatePresetMutation = useMutation({
    mutationFn: (data: { id: string; name: string; configJson: string }) =>
      api.presets.update(data.id, { name: data.name, configJson: data.configJson }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['presets'] });
      setEditingPreset(null);
    },
  });

  const deletePresetMutation = useMutation({
    mutationFn: (id: string) => api.presets.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['presets'] });
      setConfirmDelete(null);
    },
  });

  return (
    <div className="max-w-4xl mx-auto p-6">
      <PageHeader
        title="Node Configuration Presets"
        action={<Button variant="primary" size="sm" onClick={() => setShowNewForm(v => !v)}>New Preset</Button>}
      />

      {/* Create new preset form */}
      {showNewForm && (
        <div className="bg-white border border-slate-200 rounded-xl p-5 mb-6">
          <h2 className="text-sm font-medium text-slate-900 mb-3">New Preset</h2>
          <div className="space-y-4">
            <input
              type="text"
              placeholder="Preset Name"
              value={newPreset.name}
              onChange={(e) => setNewPreset({ ...newPreset, name: e.target.value })}
              className="w-full border border-slate-200 rounded-lg px-4 py-2 text-sm"
            />
            <input
              type="text"
              placeholder="Node Type"
              value={newPreset.nodeType}
              onChange={(e) => setNewPreset({ ...newPreset, nodeType: e.target.value })}
              className="w-full border border-slate-200 rounded-lg px-4 py-2 text-sm"
            />
            <textarea
              placeholder="Configuration JSON"
              value={newPreset.configJson}
              onChange={(e) => setNewPreset({ ...newPreset, configJson: e.target.value })}
              className="w-full border border-slate-200 rounded-lg px-4 py-2 text-sm"
            ></textarea>
            <Button variant="primary" size="sm" onClick={() => createPresetMutation.mutate(newPreset)}>
              Create Preset
            </Button>
          </div>
        </div>
      )}

      {/* List of presets */}
      {isLoading ? (
        <p className="text-sm text-slate-500">Loading presets...</p>
      ) : (
        <div className="space-y-3">
          {presets?.map((preset) => (
            <div key={preset.id} className="bg-white border border-slate-200 rounded-xl p-5 flex items-start justify-between">
              <div className="space-y-1">
                <p className="text-sm font-medium text-slate-900">{preset.name}</p>
                <Badge variant="info">{preset.nodeType}</Badge>
              </div>
              <div className="flex items-center gap-2">
                <Button variant="ghost" size="sm" onClick={() => setEditingPreset(preset)}>Edit</Button>
                <Button variant="danger" size="sm" onClick={() => setConfirmDelete(preset.id)}>Delete</Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Edit preset modal */}
      {editingPreset && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
          <div className="bg-white rounded-xl border border-slate-200 shadow-xl p-6 w-96">
            <h2 className="text-sm font-medium text-slate-900 mb-3">Edit Preset</h2>
            <div className="space-y-4">
              <input
                type="text"
                value={editingPreset.name}
                onChange={(e) => setEditingPreset({ ...editingPreset, name: e.target.value })}
                className="w-full border border-slate-200 rounded-lg px-4 py-2 text-sm"
              />
              <textarea
                value={editingPreset.configJson}
                onChange={(e) => setEditingPreset({ ...editingPreset, configJson: e.target.value })}
                className="w-full border border-slate-200 rounded-lg px-4 py-2 text-sm"
              ></textarea>
              <div className="flex gap-2">
                <Button variant="primary" size="sm" onClick={() => updatePresetMutation.mutate({ id: editingPreset.id, name: editingPreset.name, configJson: editingPreset.configJson })}>
                  Save Changes
                </Button>
                <Button variant="ghost" size="sm" onClick={() => setEditingPreset(null)}>Cancel</Button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Delete confirmation modal */}
      {confirmDelete && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
          <div className="bg-white rounded-xl border border-slate-200 shadow-xl p-6 w-96">
            <h2 className="text-sm font-medium text-slate-900 mb-3">Confirm Delete</h2>
            <p className="text-sm text-slate-500 mb-4">Are you sure you want to delete this preset?</p>
            <div className="flex items-center gap-2">
              <Button variant="danger" size="sm" onClick={() => deletePresetMutation.mutate(confirmDelete)}>Delete</Button>
              <Button variant="ghost" size="sm" onClick={() => setConfirmDelete(null)}>Cancel</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
