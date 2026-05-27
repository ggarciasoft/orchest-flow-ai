'use client';
import type { Node } from '@xyflow/react';
import type { NodeDescriptor, GmailCredentialSummary } from '@/lib/api';
import { X, Trash2, Plus, ExternalLink } from 'lucide-react';
import { useState, useEffect } from 'react';
import { api, PresetResponse } from '@/lib/api';

interface Props { node: Node; catalog: NodeDescriptor[]; onClose: () => void; onDelete: () => void; onConfigChange: (config: Record<string, unknown>) => void; }

/**
 * NodeConfigDrawer — side panel showing configuration, inputs, and outputs for a selected node.
 * Includes a delete button to remove the node from the canvas.
 *
 * @param node - The currently selected ReactFlow node.
 * @param catalog - Full node descriptor catalog for looking up config schema.
 * @param onClose - Called when the user closes the drawer.
 * @param onDelete - Called when the user clicks the delete button.
 * @param onConfigChange - Called with updated config when the user changes a field.
 */
export function NodeConfigDrawer({ node, catalog, onClose, onDelete, onConfigChange }: Props) {
  const data = node.data as { descriptor?: NodeDescriptor; config?: Record<string, unknown> };
  const descriptor = catalog.find(d => d.type === data.descriptor?.type);
  const config = data.config ?? {};

  const [presets, setPresets] = useState<PresetResponse[]>([]);
const [selectedPreset, setSelectedPreset] = useState<string>('');
const [presetName, setPresetName] = useState<string>('');
const [showSavePresetForm, setShowSavePresetForm] = useState<boolean>(false);
// Dynamic options: keyed by optionsSource string → array of { value, label }
const [dynamicOptions, setDynamicOptions] = useState<Record<string, { value: string; label: string }[]>>({});

useEffect(() => {
  if (descriptor) {
    api.presets.list(descriptor.type)
      .then(setPresets)
      .catch(err => console.error('Failed to fetch presets:', err));

    // Load dynamic option sources referenced by any config field
    const sources = [...new Set(
      descriptor.configuration
        .map(c => (c as { optionsSource?: string }).optionsSource)
        .filter((s): s is string => !!s)
    )];
    for (const src of sources) {
      if (src === 'gmail-credentials') {
        api.gmail.list()
          .then(creds => setDynamicOptions(prev => ({
            ...prev,
            'gmail-credentials': creds.map((c: GmailCredentialSummary) => ({
              value: c.name,
              label: c.email ? `${c.name} (${c.email})` : c.name,
            }))
          })))
          .catch(() => {});
      }
      if (src === 'llm-models') {
        api.nodes.models()
          .then(res => setDynamicOptions(prev => ({
            ...prev,
            'llm-models': res.models,
          })))
          .catch(() => {});
      }
    }
  }
}, [descriptor]);

if (!descriptor) return null; // Return early if no descriptor is found

  return (
    <div className="w-80 border-l bg-white flex flex-col shrink-0 overflow-hidden">
      {/* Drawer header with title and close button */}
      <div className="flex items-center justify-between p-4 border-b">
        <div>
          <h3 className="font-semibold text-sm">{descriptor.displayName}</h3>
          <p className="text-xs text-gray-400 font-mono mt-0.5">{descriptor.type}</p>
        </div>
        <button
          onClick={onClose}
          className="p-1 hover:bg-gray-100 rounded"
          title="Close"
        >
          <X size={16} />
        </button>
      </div>

      {/* Main content describing the configuration and its parameters */}
      <div className="flex-1 overflow-y-auto p-4 space-y-5">
        <div>
          <p className="text-xs font-semibold text-gray-500 uppercase mb-1">Description</p>
          <p className="text-sm text-gray-600">{descriptor.description}</p>
        </div>

        {/* Render configuration section only if configuration exists */}
        {descriptor.configuration.length > 0 && (
          <div>
            <p className="text-xs font-semibold text-gray-500 uppercase mb-3">Configuration</p>
            <div className="space-y-3">
              {/* Preset selector at the top */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Use Preset</label>
          <div className="flex gap-2 items-center">
            <select
              className="flex-1 border rounded-lg px-3 py-1.5 text-sm"
              value={selectedPreset}
              onChange={e => {
                const preset = presets.find(p => p.id === e.target.value);
                if (preset) {
                  const presetConfig = JSON.parse(preset.configJson);
                  onConfigChange({ ...presetConfig, ...config });
                }
                setSelectedPreset(e.target.value);
              }}
            >
              <option value="">Select a preset</option>
              {presets.map(p => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
            <button
              onClick={() => setShowSavePresetForm(!showSavePresetForm)}
              className="text-blue-600 hover:text-blue-800 flex items-center gap-1"
            >
              <Plus size={14} /> Save as Preset
            </button>
          </div>
          {showSavePresetForm && (
            <div className="mt-2 flex gap-2">
              <input
                type="text"
                value={presetName}
                onChange={(e) => setPresetName(e.target.value)}
                placeholder="Preset name"
                className="flex-1 border rounded-lg px-3 py-1.5 text-sm"
              />
              <button
                onClick={() => {
                  api.presets.create({ name: presetName, nodeType: descriptor.type, configJson: JSON.stringify(config) })
                    .then(newPreset => setPresets([...presets, newPreset]))
                    .catch(err => console.error('Failed to save preset:', err));
                  setPresetName('');
                  setShowSavePresetForm(false);
                }}
                className="bg-blue-600 text-white px-3 py-1.5 rounded-lg text-sm hover:bg-blue-700"
              >
                Save
              </button>
            </div>
          )}
        </div>

        {descriptor.configuration.map(cfg => {
                // Conditional visibility for auth sub-fields — only show when the relevant authType is selected
                const authType = config.authType as string | undefined;
                const hiddenWhen: Record<string, string[]> = {
                  authToken:          ['bearer'],
                  authUsername:       ['basic'],
                  authPassword:       ['basic'],
                  authApiKeyName:     ['api-key'],
                  authApiKeyValue:    ['api-key'],
                  authApiKeyLocation: ['api-key'],
                  authTokenUrl:       ['oauth2-client-credentials'],
                  authClientId:       ['oauth2-client-credentials'],
                  authClientSecret:   ['oauth2-client-credentials'],
                  authScope:          ['oauth2-client-credentials'],
                };
                const visibleFor = hiddenWhen[cfg.key];
                if (visibleFor && !visibleFor.includes(authType ?? '')) return null;

                // Dynamic options source (e.g. gmail-credentials fetched from API)
                const cfgWithSource = cfg as { optionsSource?: string } & typeof cfg;
                const optsSrc = cfgWithSource.optionsSource;
                const dynOpts = optsSrc ? (dynamicOptions[optsSrc] ?? []) : null;

                return (
                <div key={cfg.key}>
                  <label className="block text-xs font-medium text-gray-700 mb-1">
                    {cfg.displayName}
                    {cfg.required && <span className="text-red-500 ml-1">*</span>}
                  </label>

                  {/* Dynamic options source — renders a select + optional connect button */}
                  {dynOpts !== null ? (
                    <div className="space-y-1">
                      <select
                        className="w-full border rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                        value={String(config[cfg.key] ?? '')}
                        onChange={e => onConfigChange({ ...config, [cfg.key]: e.target.value })}
                      >
                        <option value="">— select —</option>
                        {dynOpts.map(opt => (
                          <option key={opt.value} value={opt.value}>{opt.label}</option>
                        ))}
                      </select>
                      {optsSrc === 'gmail-credentials' && (
                        <a
                          href={api.gmail.authStartUrl({ name: 'my-gmail' })}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="inline-flex items-center gap-1 text-xs text-blue-600 hover:underline mt-0.5"
                          onClick={e => {
                            // Prompt for name/creds before opening
                            e.preventDefault();
                            const name = window.prompt('Credential name (e.g. my-gmail):');
                            if (!name) return;
                            // clientId and clientSecret come from Settings — backend will use saved settings
                            window.open(api.gmail.authStartUrl({ name }), '_blank');
                          }}
                        >
                          <ExternalLink size={11} /> Connect Gmail account
                        </a>
                      )}
                    </div>
                  ) : cfg.allowedValues ? (
                    /* Static allowedValues dropdown */
                    <select
                      className="w-full border rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                      value={String(config[cfg.key] ?? cfg.defaultValue ?? '')}
                      onChange={e => onConfigChange({ ...config, [cfg.key]: e.target.value })}
                    >
                      {cfg.allowedValues.map(v => (
                        <option key={v} value={v}>{v}</option>
                      ))}
                    </select>
                  ) : (
                    <input
                      className="w-full border rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder={String(cfg.defaultValue ?? '')}
                      value={String(config[cfg.key] ?? '')}
                      onChange={e => onConfigChange({ ...config, [cfg.key]: e.target.value })}
                    />
                  )}
                  <p className="text-xs text-gray-400 mt-0.5">{cfg.description}</p>
                </div>
              )})
            }

            </div>
          </div>
        )}

        {/* Render inputs section */}
        {descriptor.inputs.length > 0 && (
          <div>
            <p className="text-xs font-semibold text-gray-500 uppercase mb-2">Inputs</p>
            {descriptor.inputs.map(i => (
              <div key={i.key} className="text-xs py-1.5 border-b flex gap-2">
                <span className="font-mono font-medium text-gray-700">{i.key}</span>
                <span className="text-gray-400">({i.type})</span>
                {i.required && <span className="text-red-400">required</span>}
              </div>
            ))}
          </div>
        )}

        {/* Render outputs section */}
        {descriptor.outputs.length > 0 && (
          <div>
            <p className="text-xs font-semibold text-gray-500 uppercase mb-2">Outputs</p>
            {descriptor.outputs.map(o => (
              <div key={o.key} className="text-xs py-1.5 border-b flex gap-2">
                <span className="font-mono font-medium text-gray-700">{o.key}</span>
                <span className="text-gray-400">({o.type})</span>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Delete button at bottom of drawer */}
      <div className="p-4 border-t shrink-0">
        <button
          onClick={onDelete}
          className="w-full flex items-center justify-center gap-2 text-sm text-red-600 hover:bg-red-50 border border-red-200 rounded-lg py-2 transition-colors"
        >
          <Trash2 size={14} /> Delete Node
        </button>
      </div>
    </div>
  );
}
