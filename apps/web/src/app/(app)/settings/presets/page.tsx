"use client";

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, PresetResponse } from '@/lib/api';

/**
 * PresetsPage — Allows users to create, edit, delete, and view configuration presets.
 */
export default function PresetsPage() {
  const queryClient = useQueryClient();
  const { data: presets, isLoading } = useQuery({ queryKey: ['presets'], queryFn: () => api.presets.list() });
  const [newPreset, setNewPreset] = useState({ name: '', nodeType: '', configJson: '' });
  const [editingPreset, setEditingPreset] = useState<PresetResponse | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);

  const createPresetMutation = useMutation({
    mutationFn: (data: { name: string; nodeType: string; configJson: string }) => api.presets.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['presets'] });
      setNewPreset({ name: '', nodeType: '', configJson: '' });
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
      <h1 className="text-2xl font-semibold mb-4">Node Configuration Presets</h1>

      {/* Create new preset form */}
      <div className="bg-white p-4 rounded-lg shadow mb-6">
        <h2 className="text-lg font-medium mb-3">New Preset</h2>
        <div className="space-y-4">
          <input
            type="text"
            placeholder="Preset Name"
            value={newPreset.name}
            onChange={(e) => setNewPreset({ ...newPreset, name: e.target.value })}
            className="w-full border rounded-lg px-4 py-2"
          />
          <input
            type="text"
            placeholder="Node Type"
            value={newPreset.nodeType}
            onChange={(e) => setNewPreset({ ...newPreset, nodeType: e.target.value })}
            className="w-full border rounded-lg px-4 py-2"
          />
          <textarea
            placeholder="Configuration JSON"
            value={newPreset.configJson}
            onChange={(e) => setNewPreset({ ...newPreset, configJson: e.target.value })}
            className="w-full border rounded-lg px-4 py-2"
          ></textarea>
          <button
            onClick={() => createPresetMutation.mutate(newPreset)}
            className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
          >
            Create Preset
          </button>
        </div>
      </div>

      {/* List of presets */}
      {isLoading ? (
        <p>Loading presets...</p>
      ) : (
        <div className="space-y-4">
          {presets?.map((preset) => (
            <div key={preset.id} className="bg-white p-4 rounded-lg shadow flex justify-between items-center">
              <div>
                <h3 className="text-lg font-medium">{preset.name}</h3>
                <p className="text-sm text-gray-500">Node Type: {preset.nodeType}</p>
              </div>
              <div className="flex items-center gap-4">
                <button
                  onClick={() => setEditingPreset(preset)}
                  className="text-blue-600 hover:underline"
                >
                  Edit
                </button>
                <button
                  onClick={() => setConfirmDelete(preset.id)}
                  className="text-red-600 hover:underline"
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Edit preset form */}
      {editingPreset && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
          <div className="bg-white p-6 rounded-lg shadow w-96">
            <h2 className="text-lg font-medium mb-3">Edit Preset</h2>
            <div className="space-y-4">
              <input
                type="text"
                value={editingPreset.name}
                onChange={(e) => setEditingPreset({ ...editingPreset, name: e.target.value })}
                className="w-full border rounded-lg px-4 py-2"
              />
              <textarea
                value={editingPreset.configJson}
                onChange={(e) => setEditingPreset({ ...editingPreset, configJson: e.target.value })}
                className="w-full border rounded-lg px-4 py-2"
              ></textarea>
              <button
                onClick={() => updatePresetMutation.mutate({ id: editingPreset.id, name: editingPreset.name, configJson: editingPreset.configJson })}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
              >
                Save Changes
              </button>
              <button
                onClick={() => setEditingPreset(null)}
                className="text-gray-600 hover:underline"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete confirmation */}
      {confirmDelete && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
          <div className="bg-white p-6 rounded-lg shadow w-96">
            <h2 className="text-lg font-medium mb-3">Confirm Delete</h2>
            <p className="mb-4">Are you sure you want to delete this preset?</p>
            <div className="flex items-center gap-4">
              <button
                onClick={() => deletePresetMutation.mutate(confirmDelete)}
                className="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700"
              >
                Delete
              </button>
              <button
                onClick={() => setConfirmDelete(null)}
                className="text-gray-600 hover:underline"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}