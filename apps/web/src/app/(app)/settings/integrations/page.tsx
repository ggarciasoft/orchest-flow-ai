'use client';
import { useState, useEffect } from 'react';
import { PageHeader } from '@/components/ui';
import { AdminPageGuard } from '@/components/AdminPageGuard';
import { api } from '@/lib/api';
import { CheckCircle, Loader2, Eye, EyeOff, Mail, ChevronDown } from 'lucide-react';

// ── Integration registry ─────────────────────────────────────────────────────
// To add a new integration: add an entry here and implement its panel below.
const INTEGRATIONS = [
  {
    id: 'gmail',
    label: 'Gmail',
    description: 'OAuth2 credentials for Gmail read/send workflow nodes',
    iconBg: 'bg-red-500',
  },
] as const;

type IntegrationId = typeof INTEGRATIONS[number]['id'];

// ── Icon helper ──────────────────────────────────────────────────────────────
function IntegrationIcon({ id }: { id: IntegrationId }) {
  const i = INTEGRATIONS.find(x => x.id === id)!;
  const base = `w-9 h-9 ${i.iconBg} rounded-lg flex items-center justify-center shrink-0`;
  return <div className={base}><Mail size={16} className="text-white" /></div>;
}

// ── Gmail panel ──────────────────────────────────────────────────────────────
function GmailPanel({ initialClientId }: { initialClientId: string }) {
  const [clientId, setClientId] = useState(initialClientId);
  const [clientSecret, setClientSecret] = useState('');
  const [showSecret, setShowSecret] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => { setClientId(initialClientId); }, [initialClientId]);

  const handleSave = async () => {
    setSaving(true); setSaved(false);
    try {
      const updates: Record<string, string> = {};
      if (clientId) updates['gmail.clientId'] = clientId;
      if (clientSecret) updates['gmail.clientSecret'] = clientSecret;
      if (Object.keys(updates).length) await api.settings.update(updates);
      setSaved(true); setClientSecret('');
      setTimeout(() => setSaved(false), 3000);
    } finally { setSaving(false); }
  };

  return (
    <div className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">Client ID</label>
        <input type="text"
          className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-red-400"
          placeholder="xxxx.apps.googleusercontent.com"
          value={clientId} onChange={e => setClientId(e.target.value)} />
      </div>
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">Client Secret</label>
        <div className="relative">
          <input type={showSecret ? 'text' : 'password'}
            className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-red-400 pr-10"
            placeholder="Leave blank to keep existing"
            value={clientSecret} onChange={e => setClientSecret(e.target.value)} />
          <button type="button" onClick={() => setShowSecret(v => !v)}
            className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
            {showSecret ? <EyeOff size={15} /> : <Eye size={15} />}
          </button>
        </div>
      </div>
      <p className="text-xs text-slate-400">
        Once saved, click <strong>Connect Gmail account</strong> in any GmailReadNode config drawer — no need to re-enter credentials each time.<br/>
        You can store values as <code className="bg-slate-100 px-1 rounded font-mono">{'{{'+'secret:name'+'}}'}</code> and they will be resolved automatically.
      </p>
      <div className="flex items-center justify-between pt-2 border-t border-slate-100">
        {saved ? <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p> : <span />}
        <button onClick={handleSave} disabled={saving}
          className="bg-red-500 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-red-600 disabled:opacity-50 flex items-center gap-2">
          {saving && <Loader2 size={14} className="animate-spin" />}
          Save changes
        </button>
      </div>
    </div>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────
export default function IntegrationsPage() {
  const [selected, setSelected] = useState<IntegrationId>('gmail');
  const [open, setOpen] = useState(false);
  const [gmailClientId, setGmailClientId] = useState('');

  useEffect(() => {
    api.settings.get().then(s => {
      if (s['gmail.clientId']) setGmailClientId(s['gmail.clientId'] ?? '');
    }).catch(() => {});
  }, []);

  const current = INTEGRATIONS.find(p => p.id === selected)!;

  return (
    <AdminPageGuard>
    <div>
      <PageHeader title="Integrations" subtitle="Connect external services used in workflow nodes" />

      <div className="mt-6 space-y-6">
        {/* Integration selector */}
        <div className="bg-white border border-slate-200 rounded-xl p-4">
          <label className="block text-xs font-medium text-slate-500 uppercase tracking-wide mb-2">
            Select integration
          </label>
          <div className="relative">
            <button
              type="button"
              onClick={() => setOpen(v => !v)}
              className="w-full flex items-center gap-3 px-4 py-3 border border-slate-200 rounded-xl bg-white hover:bg-slate-50 transition-colors text-left"
            >
              <IntegrationIcon id={selected} />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-slate-900">{current.label}</p>
                <p className="text-xs text-slate-500 truncate">{current.description}</p>
              </div>
              <ChevronDown size={16} className={`text-slate-400 transition-transform shrink-0 ${open ? 'rotate-180' : ''}`} />
            </button>

            {open && (
              <div className="absolute top-full left-0 right-0 mt-1 bg-white border border-slate-200 rounded-xl shadow-lg z-10 overflow-hidden">
                {INTEGRATIONS.map(i => (
                  <button
                    key={i.id}
                    type="button"
                    onClick={() => { setSelected(i.id); setOpen(false); }}
                    className={`w-full flex items-center gap-3 px-4 py-3 hover:bg-slate-50 transition-colors text-left ${i.id === selected ? 'bg-indigo-50' : ''}`}
                  >
                    <IntegrationIcon id={i.id} />
                    <div className="flex-1 min-w-0">
                      <p className={`text-sm font-medium ${i.id === selected ? 'text-indigo-700' : 'text-slate-900'}`}>{i.label}</p>
                      <p className="text-xs text-slate-500 truncate">{i.description}</p>
                    </div>
                    {i.id === selected && <CheckCircle size={15} className="text-indigo-600 shrink-0" />}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Config panel */}
        <div className="bg-white border border-slate-200 rounded-xl">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center gap-3">
            <IntegrationIcon id={selected} />
            <div>
              <p className="text-sm font-semibold text-slate-900">{current.label} Configuration</p>
              <p className="text-xs text-slate-500">{current.description}</p>
            </div>
          </div>
          <div className="p-6">
            {selected === 'gmail' && <GmailPanel initialClientId={gmailClientId} />}
          </div>
        </div>
      </div>
    </div>
    </AdminPageGuard>
  );
}
