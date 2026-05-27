'use client';
import { useState, useEffect } from 'react';
import { PageHeader } from '@/components/ui';
import { api, SecretSummary } from '@/lib/api';
import { CheckCircle, XCircle, Loader2, Eye, EyeOff, Trash2, Plus, Lock, Mail } from 'lucide-react';

type TestStatus = 'idle' | 'testing' | 'ok' | 'fail';

const MODELS = [
  'gpt-4o',
  'gpt-4o-mini',
  'gpt-4-turbo',
  'gpt-3.5-turbo',
];

export default function SettingsPage() {
  const [apiKey, setApiKey] = useState('');
  const [defaultModel, setDefaultModel] = useState('gpt-4o-mini');
  const [showKey, setShowKey] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [testStatus, setTestStatus] = useState<TestStatus>('idle');
  const [testMessage, setTestMessage] = useState('');

  // Gmail OAuth2 state
  const [gmailClientId, setGmailClientId] = useState('');
  const [gmailClientSecret, setGmailClientSecret] = useState('');
  const [showGmailSecret, setShowGmailSecret] = useState(false);
  const [gmailSaving, setGmailSaving] = useState(false);
  const [gmailSaved, setGmailSaved] = useState(false);

  // Secrets vault state
  const [secrets, setSecrets] = useState<SecretSummary[]>([]);
  const [secretsLoading, setSecretsLoading] = useState(false);
  const [newSecretName, setNewSecretName] = useState('');
  const [newSecretValue, setNewSecretValue] = useState('');
  const [showSecretValue, setShowSecretValue] = useState(false);
  const [addingSecret, setAddingSecret] = useState(false);
  const [secretError, setSecretError] = useState('');

  const loadSecrets = async () => {
    setSecretsLoading(true);
    try { setSecrets(await api.secrets.list()); } catch { /* ignore */ } finally { setSecretsLoading(false); }
  };

  useEffect(() => {
    api.settings.get().then(s => {
      if (s['llm.defaultModel']) setDefaultModel(s['llm.defaultModel'] ?? 'gpt-4o-mini');
      if (s['gmail.clientId']) setGmailClientId(s['gmail.clientId'] ?? '');
      // Never populate clientSecret — let user re-enter if needed
    }).catch(() => {});
    loadSecrets();
  }, []);

  const handleSave = async () => {
    setSaving(true);
    setSaved(false);
    try {
      const updates: Record<string, string> = { 'llm.defaultModel': defaultModel };
      if (apiKey) updates['llm.openai.apiKey'] = apiKey;
      await api.settings.update(updates);
      setSaved(true);
      setApiKey('');
      setTimeout(() => setSaved(false), 3000);
    } finally {
      setSaving(false);
    }
  };

  const handleTest = async () => {
    setTestStatus('testing');
    setTestMessage('');
    try {
      const r = await api.settings.testOpenAI();
      setTestStatus(r.success ? 'ok' : 'fail');
      setTestMessage(r.message);
    } catch {
      setTestStatus('fail');
      setTestMessage('Request failed');
    }
  };

  const handleSaveGmail = async () => {
    setGmailSaving(true);
    setGmailSaved(false);
    try {
      const updates: Record<string, string> = {};
      if (gmailClientId) updates['gmail.clientId'] = gmailClientId;
      if (gmailClientSecret) updates['gmail.clientSecret'] = gmailClientSecret;
      if (Object.keys(updates).length > 0) await api.settings.update(updates);
      setGmailSaved(true);
      setGmailClientSecret('');
      setTimeout(() => setGmailSaved(false), 3000);
    } finally {
      setGmailSaving(false);
    }
  };

  const handleAddSecret = async () => {
    setSecretError('');
    if (!newSecretName.trim() || !newSecretValue.trim()) { setSecretError('Name and value are required'); return; }
    setAddingSecret(true);
    try {
      await api.secrets.create(newSecretName.trim(), newSecretValue);
      setNewSecretName(''); setNewSecretValue('');
      await loadSecrets();
    } catch (e: unknown) {
      setSecretError((e as Error)?.message ?? 'Failed to create secret');
    } finally { setAddingSecret(false); }
  };

  const handleDeleteSecret = async (id: string) => {
    if (!confirm('Delete this secret? Workflows using it will break.')) return;
    try { await api.secrets.delete(id); await loadSecrets(); } catch { /* ignore */ }
  };

  return (
    <div>
      <PageHeader title="Settings" subtitle="Configure AI providers and platform settings" />

      <div className="space-y-6 mt-6">
        {/* OpenAI Provider */}
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center gap-3">
            <div className="w-8 h-8 bg-slate-900 rounded-lg flex items-center justify-center text-white text-xs font-bold">AI</div>
            <div>
              <p className="text-sm font-semibold text-slate-900">OpenAI</p>
              <p className="text-xs text-slate-500">GPT-4o, GPT-4o-mini, GPT-3.5-turbo</p>
            </div>
          </div>

          <div className="p-6 space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                API Key
              </label>
              <div className="flex gap-2">
                <div className="relative flex-1">
                  <input
                    type={showKey ? 'text' : 'password'}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 pr-10"
                    placeholder="sk-... (leave blank to keep existing)"
                    value={apiKey}
                    onChange={e => setApiKey(e.target.value)}
                  />
                  <button
                    type="button"
                    onClick={() => setShowKey(v => !v)}
                    className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600"
                  >
                    {showKey ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
                <button
                  onClick={handleTest}
                  disabled={testStatus === 'testing'}
                  className="px-3 py-2 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-50 whitespace-nowrap flex items-center gap-2"
                >
                  {testStatus === 'testing' && <Loader2 size={14} className="animate-spin" />}
                  Test connection
                </button>
              </div>

              {testStatus === 'ok' && (
                <p className="mt-1.5 text-xs text-green-600 flex items-center gap-1">
                  <CheckCircle size={13} /> {testMessage}
                </p>
              )}
              {testStatus === 'fail' && (
                <p className="mt-1.5 text-xs text-red-600 flex items-center gap-1">
                  <XCircle size={13} /> {testMessage}
                </p>
              )}
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Default Model
              </label>
              <select
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                value={defaultModel}
                onChange={e => setDefaultModel(e.target.value)}
              >
                {MODELS.map(m => <option key={m} value={m}>{m}</option>)}
              </select>
              <p className="text-xs text-slate-400 mt-1">Used when a node&apos;s model is set to &quot;default&quot;.</p>
            </div>
          </div>

          <div className="px-6 py-4 border-t border-slate-200 flex items-center justify-between">
            {saved && <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p>}
            {!saved && <span />}
            <button
              onClick={handleSave}
              disabled={saving}
              className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50 flex items-center gap-2"
            >
              {saving && <Loader2 size={14} className="animate-spin" />}
              Save changes
            </button>
          </div>
        </div>

        {/* Gmail OAuth2 App */}
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center gap-3">
            <div className="w-8 h-8 bg-red-500 rounded-lg flex items-center justify-center text-white">
              <Mail size={16} />
            </div>
            <div>
              <p className="text-sm font-semibold text-slate-900">Gmail</p>
              <p className="text-xs text-slate-500">OAuth2 app credentials for Gmail integration nodes</p>
            </div>
          </div>
          <div className="p-6 space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Client ID</label>
              <input
                type="text"
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                placeholder="xxx.apps.googleusercontent.com"
                value={gmailClientId}
                onChange={e => setGmailClientId(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Client Secret</label>
              <div className="relative">
                <input
                  type={showGmailSecret ? 'text' : 'password'}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 pr-10"
                  placeholder="Leave blank to keep existing"
                  value={gmailClientSecret}
                  onChange={e => setGmailClientSecret(e.target.value)}
                />
                <button type="button" onClick={() => setShowGmailSecret(v => !v)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                  {showGmailSecret ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>
            <div className="pt-1">
              <p className="text-xs text-slate-400">
                Once saved, use the <strong>Connect Gmail account</strong> link in any GmailReadNode config — no need to re-enter credentials.
              </p>
            </div>
          </div>
          <div className="px-6 py-4 border-t border-slate-200 flex items-center justify-between">
            {gmailSaved && <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p>}
            {!gmailSaved && <span />}
            <button
              onClick={handleSaveGmail}
              disabled={gmailSaving}
              className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50 flex items-center gap-2"
            >
              {gmailSaving && <Loader2 size={14} className="animate-spin" />}
              Save changes
            </button>
          </div>
        </div>

        {/* Secret Vault */}
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center gap-3">
            <div className="w-8 h-8 bg-amber-600 rounded-lg flex items-center justify-center text-white"><Lock size={16} /></div>
            <div>
              <p className="text-sm font-semibold text-slate-900">Secret Vault</p>
              <p className="text-xs text-slate-500">Encrypted named values. Reference in any node config as <code className="bg-slate-100 px-1 rounded">{'{{secret:name}}'}</code></p>
            </div>
          </div>

          <div className="p-6 space-y-4">
            {secretsLoading ? (
              <div className="flex items-center gap-2 text-slate-400 text-sm"><Loader2 size={14} className="animate-spin" /> Loading secrets...</div>
            ) : secrets.length === 0 ? (
              <p className="text-sm text-slate-400">No secrets yet.</p>
            ) : (
              <ul className="divide-y divide-slate-100">
                {secrets.map(s => (
                  <li key={s.id} className="flex items-center justify-between py-2">
                    <div>
                      <span className="text-sm font-medium text-slate-800">{s.name}</span>
                      <span className="ml-2 text-xs text-slate-400">Added {new Date(s.createdAt).toLocaleDateString()}</span>
                    </div>
                    <button onClick={() => handleDeleteSecret(s.id)} className="text-slate-400 hover:text-red-500 transition-colors"><Trash2 size={15} /></button>
                  </li>
                ))}
              </ul>
            )}

            <div className="border-t border-slate-100 pt-4">
              <p className="text-xs font-medium text-slate-600 mb-2">Add new secret</p>
              <div className="flex gap-2">
                <input
                  type="text"
                  placeholder="Secret name (e.g. openai-key)"
                  value={newSecretName}
                  onChange={e => setNewSecretName(e.target.value)}
                  className="flex-1 border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-500"
                />
                <div className="relative flex-1">
                  <input
                    type={showSecretValue ? 'text' : 'password'}
                    placeholder="Secret value"
                    value={newSecretValue}
                    onChange={e => setNewSecretValue(e.target.value)}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 pr-8"
                  />
                  <button type="button" onClick={() => setShowSecretValue(v => !v)} className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                    {showSecretValue ? <EyeOff size={14} /> : <Eye size={14} />}
                  </button>
                </div>
                <button
                  onClick={handleAddSecret}
                  disabled={addingSecret}
                  className="px-3 py-2 bg-amber-600 text-white rounded-lg text-sm font-medium hover:bg-amber-700 disabled:opacity-50 flex items-center gap-1"
                >
                  {addingSecret ? <Loader2 size={14} className="animate-spin" /> : <Plus size={14} />}
                  Add
                </button>
              </div>
              {secretError && <p className="mt-1.5 text-xs text-red-600">{secretError}</p>}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
