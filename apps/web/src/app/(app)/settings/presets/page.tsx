"use client";

import { useState, useEffect, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ExternalLink, Plus, ChevronDown, Trash2, Edit2, X, Sliders } from 'lucide-react';
import { api, PresetResponse, NodeDescriptor, NodePort, GmailCredentialSummary } from '@/lib/api';
import { PageHeader, Badge, Button, EmptyState } from '@/components/ui';

// ─── Config field renderer (same logic as NodeConfigDrawer) ──────────────────

function NodeConfigFields({
  descriptor,
  config,
  onChange,
}: {
  descriptor: NodeDescriptor;
  config: Record<string, unknown>;
  onChange: (cfg: Record<string, unknown>) => void;
}) {
  const [dynamicOptions, setDynamicOptions] = useState<Record<string, { value: string; label: string }[]>>({});

  useEffect(() => {
    const sources = [...new Set(
      descriptor.configuration
        .map(c => (c as NodePort & { optionsSource?: string }).optionsSource)
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
            })),
          })))
          .catch(() => {});
      }
      if (src === 'llm-models') {
        api.nodes.models()
          .then(res => setDynamicOptions(prev => ({ ...prev, 'llm-models': res.models })))
          .catch(() => {});
      }
    }
  }, [descriptor]);

  if (descriptor.configuration.length === 0) {
    return <p className="text-xs text-slate-400 italic">This node has no configurable fields.</p>;
  }

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

  return (
    <div className="space-y-4">
      {descriptor.configuration.map(cfg => {
        const visibleFor = hiddenWhen[cfg.key];
        if (visibleFor && !visibleFor.includes(authType ?? '')) return null;

        const optsSrc = (cfg as NodePort & { optionsSource?: string }).optionsSource;
        const dynOpts = optsSrc ? (dynamicOptions[optsSrc] ?? []) : null;

        return (
          <div key={cfg.key}>
            <label className="mb-1 block text-xs font-medium text-slate-700">
              {cfg.displayName}
              {cfg.required && <span className="ml-1 text-red-500">*</span>}
            </label>

            {dynOpts !== null ? (
              <div className="space-y-1">
                <select
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                  value={String(config[cfg.key] ?? '')}
                  onChange={e => onChange({ ...config, [cfg.key]: e.target.value })}
                >
                  <option value="">-- select --</option>
                  {dynOpts.map(opt => (
                    <option key={opt.value} value={opt.value}>{opt.label}</option>
                  ))}
                </select>
                {optsSrc === 'gmail-credentials' && (
                  <div className="space-y-1">
                    <p className="text-xs text-amber-600">
                      Configure Gmail credentials in{' '}
                      <a href="/settings/integrations" className="underline">Settings → Integrations</a> first.
                    </p>
                    <a
                      href="#"
                      className="inline-flex items-center gap-1 text-xs text-indigo-600 hover:underline"
                      onClick={e => {
                        e.preventDefault();
                        const name = window.prompt('Credential name (e.g. my-gmail):');
                        if (!name) return;
                        window.open(api.gmail.authStartUrl({ name }), '_blank');
                      }}
                    >
                      <ExternalLink size={11} /> Connect Gmail account
                    </a>
                  </div>
                )}
              </div>
            ) : cfg.allowedValues ? (
              <div>
                <select
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                  value={String(config[cfg.key] ?? cfg.defaultValue ?? '')}
                  onChange={e => onChange({ ...config, [cfg.key]: e.target.value })}
                >
                  {cfg.allowedValues.map(v => <option key={v} value={v}>{v}</option>)}
                </select>
                {cfg.optionDescriptions && config[cfg.key] != null &&
                  cfg.optionDescriptions[String(config[cfg.key])] && (
                  <pre className="mt-1.5 whitespace-pre-wrap rounded border bg-slate-50 p-2 font-mono text-xs leading-relaxed text-slate-500">
                    {cfg.optionDescriptions[String(config[cfg.key])]}
                  </pre>
                )}
              </div>
            ) : cfg.type === 'Boolean' ? (
              <select
                className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                value={String(config[cfg.key] ?? cfg.defaultValue ?? 'false')}
                onChange={e => onChange({ ...config, [cfg.key]: e.target.value === 'true' })}
              >
                <option value="false">false</option>
                <option value="true">true</option>
              </select>
            ) : (cfg as NodePort & { isMultiline?: boolean }).isMultiline ? (
              <textarea
                rows={4}
                className="w-full resize-y rounded-lg border border-slate-200 px-3 py-2 font-mono text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                placeholder={String(cfg.defaultValue ?? '')}
                value={String(config[cfg.key] ?? '')}
                onChange={e => onChange({ ...config, [cfg.key]: e.target.value })}
              />
            ) : (
              <div className="space-y-1">
                <input
                  type={cfg.isSensitive ? 'password' : 'text'}
                  className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
                  placeholder={cfg.isSensitive ? '••••••••  or  {{secret:name}}' : String(cfg.defaultValue ?? '')}
                  value={String(config[cfg.key] ?? '')}
                  onChange={e => onChange({ ...config, [cfg.key]: e.target.value })}
                />
                {cfg.isSensitive && (
                  <p className="flex flex-wrap items-center gap-1 text-xs text-amber-600">
                    <span>🔒 Sensitive — store as</span>
                    <code className="rounded border border-amber-200 bg-amber-50 px-1 font-mono">{'{{secret:name}}'}</code>
                    <a href="/settings/secrets" target="_blank" className="font-medium underline">Manage secrets</a>
                  </p>
                )}
              </div>
            )}

            {cfg.description && (
              <p className="mt-0.5 text-xs text-slate-400">{cfg.description}</p>
            )}
          </div>
        );
      })}
    </div>
  );
}

// ─── Grouped node type select ─────────────────────────────────────────────────

function NodeTypeSelect({
  catalog,
  value,
  onChange,
  disabled = false,
}: {
  catalog: NodeDescriptor[];
  value: string;
  onChange: (type: string) => void;
  disabled?: boolean;
}) {
  const categories = useMemo(() => {
    const map = new Map<string, NodeDescriptor[]>();
    for (const d of catalog) {
      if (!map.has(d.category)) map.set(d.category, []);
      map.get(d.category)!.push(d);
    }
    return map;
  }, [catalog]);

  return (
    <div className="relative">
      <select
        value={value}
        onChange={e => onChange(e.target.value)}
        disabled={disabled}
        className="w-full appearance-none rounded-lg border border-slate-200 bg-white px-3 py-2 pr-8 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300 disabled:cursor-not-allowed disabled:opacity-60"
      >
        <option value="">-- select node type --</option>
        {[...categories.entries()].map(([cat, nodes]) => (
          <optgroup key={cat} label={cat}>
            {nodes.map(n => (
              <option key={n.type} value={n.type}>{n.displayName}</option>
            ))}
          </optgroup>
        ))}
      </select>
      <ChevronDown className="pointer-events-none absolute right-2.5 top-2.5 h-4 w-4 text-slate-400" />
    </div>
  );
}

// ─── Preset form (create / edit) ──────────────────────────────────────────────

function PresetForm({
  title,
  initialName = '',
  initialNodeType = '',
  initialConfig = {},
  fixedNodeType = false,
  catalog,
  isLoading,
  onSubmit,
  onCancel,
  submitLabel = 'Create Preset',
}: {
  title: string;
  initialName?: string;
  initialNodeType?: string;
  initialConfig?: Record<string, unknown>;
  fixedNodeType?: boolean;
  catalog: NodeDescriptor[];
  isLoading: boolean;
  onSubmit: (name: string, nodeType: string, config: Record<string, unknown>) => void;
  onCancel: () => void;
  submitLabel?: string;
}) {
  const [name, setName] = useState(initialName);
  const [nodeType, setNodeType] = useState(initialNodeType);
  const [config, setConfig] = useState<Record<string, unknown>>(initialConfig);
  const [error, setError] = useState('');

  const descriptor = catalog.find(d => d.type === nodeType);

  function handleNodeTypeChange(type: string) {
    setNodeType(type);
    setConfig({});
  }

  function handleSubmit() {
    if (!name.trim()) { setError('Preset name is required.'); return; }
    if (!nodeType)     { setError('Node type is required.'); return; }
    setError('');
    onSubmit(name.trim(), nodeType, config);
  }

  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold text-slate-800">{title}</h3>

      <div>
        <label className="mb-1 block text-xs font-medium text-slate-700">Preset name</label>
        <input
          type="text"
          placeholder="e.g. GPT-4o Mini (English)"
          value={name}
          onChange={e => { setName(e.target.value); setError(''); }}
          className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-300"
        />
      </div>

      <div>
        <label className="mb-1 block text-xs font-medium text-slate-700">Node type</label>
        {fixedNodeType ? (
          <div className="flex items-center gap-2">
            <Badge variant="info">{descriptor?.displayName ?? nodeType}</Badge>
            <span className="text-xs text-slate-400 font-mono">{nodeType}</span>
          </div>
        ) : (
          <NodeTypeSelect catalog={catalog} value={nodeType} onChange={handleNodeTypeChange} />
        )}
      </div>

      {descriptor && (
        <div>
          <label className="mb-2 block text-xs font-semibold uppercase tracking-wide text-slate-500">
            Configuration
          </label>
          <div className="rounded-lg border border-slate-100 bg-slate-50 p-4">
            <NodeConfigFields descriptor={descriptor} config={config} onChange={setConfig} />
          </div>
        </div>
      )}

      {error && <p className="text-xs text-red-600">{error}</p>}

      <div className="flex gap-2 pt-1">
        <Button variant="primary" size="sm" onClick={handleSubmit} disabled={isLoading}>
          {isLoading ? 'Saving…' : submitLabel}
        </Button>
        <Button variant="ghost" size="sm" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </div>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export default function PresetsPage() {
  const queryClient = useQueryClient();

  const { data: presets, isLoading: presetsLoading } = useQuery({
    queryKey: ['presets'],
    queryFn: () => api.presets.list(),
  });

  const { data: catalogData } = useQuery({
    queryKey: ['nodes-catalog'],
    queryFn: () => api.nodes.catalog(),
  });

  const catalog: NodeDescriptor[] = catalogData?.nodes ?? [];

  const [showNewForm, setShowNewForm] = useState(false);
  const [editingPreset, setEditingPreset] = useState<(PresetResponse & { parsedConfig: Record<string, unknown> }) | null>(null);
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: (data: { name: string; nodeType: string; configJson: string }) =>
      api.presets.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['presets'] });
      setShowNewForm(false);
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: { id: string; name: string; configJson: string }) =>
      api.presets.update(data.id, { name: data.name, configJson: data.configJson }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['presets'] });
      setEditingPreset(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.presets.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['presets'] });
      setConfirmDelete(null);
    },
  });

  function openEdit(preset: PresetResponse) {
    let parsedConfig: Record<string, unknown> = {};
    try { parsedConfig = JSON.parse(preset.configJson); } catch { /* keep empty */ }
    setEditingPreset({ ...preset, parsedConfig });
  }

  // Group existing presets by category for display
  const presetsByCategory = useMemo(() => {
    const map = new Map<string, { preset: PresetResponse; descriptor?: NodeDescriptor }[]>();
    for (const p of presets ?? []) {
      const descriptor = catalog.find(d => d.type === p.nodeType);
      const cat = descriptor?.category ?? 'Other';
      if (!map.has(cat)) map.set(cat, []);
      map.get(cat)!.push({ preset: p, descriptor });
    }
    return map;
  }, [presets, catalog]);

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-6">
      <PageHeader
        title="Node Configuration Presets"
        subtitle="Save reusable node configurations. Apply them directly in the workflow designer."
        action={
          <Button variant="primary" size="sm" onClick={() => { setShowNewForm(v => !v); }}>
            <Plus className="mr-1 h-3.5 w-3.5" /> New Preset
          </Button>
        }
      />

      {/* ── Create form ─────────────────────────────────────────────────── */}
      {showNewForm && (
        <div className="rounded-xl border border-indigo-200 bg-white p-5 shadow-sm">
          <PresetForm
            title="New Preset"
            catalog={catalog}
            isLoading={createMutation.isPending}
            onSubmit={(name, nodeType, config) =>
              createMutation.mutate({ name, nodeType, configJson: JSON.stringify(config) })
            }
            onCancel={() => setShowNewForm(false)}
            submitLabel="Create Preset"
          />
        </div>
      )}

      {/* ── Preset list ──────────────────────────────────────────────────── */}
      {presetsLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Loading…</div>
      ) : (presets ?? []).length === 0 && !showNewForm ? (
        <EmptyState
          icon={Sliders}
          title="No presets yet"
          subtitle="Create a preset to save a node configuration you use often."
          action={<Button variant="primary" size="sm" onClick={() => setShowNewForm(true)}>Create your first preset</Button>}
        />
      ) : (
        <div className="space-y-6">
          {[...presetsByCategory.entries()].map(([cat, items]) => (
            <section key={cat}>
              <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-slate-400">{cat}</p>
              <div className="space-y-2">
                {items.map(({ preset, descriptor }) => (
                  <div
                    key={preset.id}
                    className="flex items-center justify-between gap-4 rounded-xl border border-slate-200 bg-white px-5 py-3.5 shadow-sm"
                  >
                    <div className="min-w-0 space-y-0.5">
                      <p className="text-sm font-medium text-slate-900">{preset.name}</p>
                      <div className="flex items-center gap-2">
                        <Badge variant="info">{descriptor?.displayName ?? preset.nodeType}</Badge>
                        <span className="font-mono text-xs text-slate-400">{preset.nodeType}</span>
                      </div>
                    </div>
                    <div className="flex shrink-0 items-center gap-2">
                      <button
                        onClick={() => openEdit(preset)}
                        className="rounded p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                        title="Edit preset"
                      >
                        <Edit2 className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => setConfirmDelete(preset.id)}
                        className="rounded p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-500"
                        title="Delete preset"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </section>
          ))}
        </div>
      )}

      {/* ── Edit modal ───────────────────────────────────────────────────── */}
      {editingPreset && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="relative w-full max-w-lg max-h-[90vh] overflow-y-auto rounded-xl border border-slate-200 bg-white p-6 shadow-2xl">
            <button
              onClick={() => setEditingPreset(null)}
              className="absolute right-4 top-4 rounded p-1 text-slate-400 hover:bg-slate-100"
            >
              <X className="h-4 w-4" />
            </button>
            <PresetForm
              title="Edit Preset"
              initialName={editingPreset.name}
              initialNodeType={editingPreset.nodeType}
              initialConfig={editingPreset.parsedConfig}
              fixedNodeType
              catalog={catalog}
              isLoading={updateMutation.isPending}
              onSubmit={(name, _nodeType, config) =>
                updateMutation.mutate({
                  id: editingPreset.id,
                  name,
                  configJson: JSON.stringify(config),
                })
              }
              onCancel={() => setEditingPreset(null)}
              submitLabel="Save Changes"
            />
          </div>
        </div>
      )}

      {/* ── Delete confirmation ──────────────────────────────────────────── */}
      {confirmDelete && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-sm rounded-xl border border-slate-200 bg-white p-6 shadow-2xl">
            <h2 className="mb-1 text-sm font-semibold text-slate-900">Delete preset?</h2>
            <p className="mb-5 text-sm text-slate-500">
              This will permanently remove the preset. Existing workflows that use it are not affected.
            </p>
            <div className="flex gap-2">
              <Button variant="danger" size="sm" onClick={() => deleteMutation.mutate(confirmDelete)} disabled={deleteMutation.isPending}>
                {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
              </Button>
              <Button variant="ghost" size="sm" onClick={() => setConfirmDelete(null)}>Cancel</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
