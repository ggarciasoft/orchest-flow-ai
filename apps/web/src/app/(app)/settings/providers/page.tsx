'use client';
import { useState, useEffect } from 'react';
import { PageHeader } from '@/components/ui';
import { api } from '@/lib/api';
import {
  CheckCircle, XCircle, Loader2, Eye, EyeOff,
  Mail, Cpu, Cloud, Server, ChevronDown,
} from 'lucide-react';

type TestStatus = 'idle' | 'testing' | 'ok' | 'fail';

// ── Provider registry ────────────────────────────────────────────────────────
// To add a new provider: add an entry here and implement its panel below.
const PROVIDERS = [
  { id: 'openai',    label: 'OpenAI',       description: 'GPT-4o, GPT-4o-mini, GPT-3.5-turbo',              iconBg: 'bg-slate-900',  iconText: 'text-white text-xs font-bold', iconContent: 'AI' },
  { id: 'anthropic', label: 'Anthropic',    description: 'Claude 3.5 Sonnet, Claude 3 Haiku, Claude 3 Opus', iconBg: 'bg-amber-700',  iconText: 'text-white', iconContent: 'cpu' },
  { id: 'azure',     label: 'Azure OpenAI', description: 'Hosted GPT models via Azure deployment',           iconBg: 'bg-blue-600',   iconText: 'text-white', iconContent: 'cloud' },
  { id: 'ollama',    label: 'Ollama',       description: 'Local models — Llama 3, Mistral, Phi-3, Gemma 2', iconBg: 'bg-green-700',  iconText: 'text-white', iconContent: 'server' },
  { id: 'gmail',     label: 'Gmail',        description: 'OAuth2 credentials for Gmail integration nodes',   iconBg: 'bg-red-500',    iconText: 'text-white', iconContent: 'mail' },
] as const;

type ProviderId = typeof PROVIDERS[number]['id'];

const MODELS = ['gpt-4o', 'gpt-4o-mini', 'gpt-4-turbo', 'gpt-3.5-turbo'];

// ── Icon helper ──────────────────────────────────────────────────────────────
function ProviderIcon({ id }: { id: ProviderId }) {
  const p = PROVIDERS.find(x => x.id === id)!;
  const base = `w-9 h-9 ${p.iconBg} rounded-lg flex items-center justify-center shrink-0`;
  if (id === 'openai') return <div className={base}><span className="text-white text-xs font-bold">AI</span></div>;
  if (id === 'anthropic') return <div className={base}><Cpu size={16} className="text-white" /></div>;
  if (id === 'azure') return <div className={base}><Cloud size={16} className="text-white" /></div>;
  if (id === 'ollama') return <div className={base}><Server size={16} className="text-white" /></div>;
  return <div className={base}><Mail size={16} className="text-white" /></div>;
}

// ── Sub-components for each provider panel ───────────────────────────────────

function OpenAIPanel({ initialModel }: { initialModel: string }) {
  const [apiKey, setApiKey] = useState('');
  const [defaultModel, setDefaultModel] = useState(initialModel);
  const [showKey, setShowKey] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [testStatus, setTestStatus] = useState<TestStatus>('idle');
  const [testMessage, setTestMessage] = useState('');

  useEffect(() => { setDefaultModel(initialModel); }, [initialModel]);

  const handleSave = async () => {
    setSaving(true); setSaved(false);
    try {
      const updates: Record<string, string> = { 'llm.defaultModel': defaultModel };
      if (apiKey) updates['llm.openai.apiKey'] = apiKey;
      await api.settings.update(updates);
      setSaved(true); setApiKey('');
      setTimeout(() => setSaved(false), 3000);
    } finally { setSaving(false); }
  };

  const handleTest = async () => {
    setTestStatus('testing'); setTestMessage('');
    try {
      const r = await api.settings.testOpenAI();
      setTestStatus(r.success ? 'ok' : 'fail');
      setTestMessage(r.message);
    } catch { setTestStatus('fail'); setTestMessage('Request failed'); }
  };

  return (
    <div className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">API Key</label>
        <div className="flex gap-2">
          <div className="relative flex-1">
            <input type={showKey ? 'text' : 'password'}
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 pr-10"
              placeholder="sk-... (leave blank to keep existing)"
              value={apiKey} onChange={e => setApiKey(e.target.value)} />
            <button type="button" onClick={() => setShowKey(v => !v)}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
              {showKey ? <EyeOff size={15} /> : <Eye size={15} />}
            </button>
          </div>
          <button onClick={handleTest} disabled={testStatus === 'testing'}
            className="px-3 py-2 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-50 whitespace-nowrap flex items-center gap-2">
            {testStatus === 'testing' && <Loader2 size={14} className="animate-spin" />}
            Test connection
          </button>
        </div>
        {testStatus === 'ok' && <p className="mt-1.5 text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} />{testMessage}</p>}
        {testStatus === 'fail' && <p className="mt-1.5 text-xs text-red-600 flex items-center gap-1"><XCircle size={13} />{testMessage}</p>}
      </div>
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">Default Model</label>
        <select className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          value={defaultModel} onChange={e => setDefaultModel(e.target.value)}>
          {MODELS.map(m => <option key={m} value={m}>{m}</option>)}
        </select>
        <p className="text-xs text-slate-400 mt-1">Used when a node&apos;s model is set to &quot;default&quot;.</p>
      </div>
      <div className="flex items-center justify-between pt-2 border-t border-slate-100">
        {saved ? <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p> : <span />}
        <button onClick={handleSave} disabled={saving}
          className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50 flex items-center gap-2">
          {saving && <Loader2 size={14} className="animate-spin" />}
          Save changes
        </button>
      </div>
    </div>
  );
}

function AnthropicPanel() {
  const [apiKey, setApiKey] = useState('');
  const [showKey, setShowKey] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [testStatus, setTestStatus] = useState<TestStatus>('idle');
  const [testMessage, setTestMessage] = useState('');

  const handleSave = async () => {
    setSaving(true); setSaved(false);
    try {
      if (apiKey) await api.settings.update({ 'llm.anthropic.apiKey': apiKey });
      setSaved(true); setApiKey('');
      setTimeout(() => setSaved(false), 3000);
    } finally { setSaving(false); }
  };

  const handleTest = async () => {
    setTestStatus('testing'); setTestMessage('');
    try {
      const r = await api.settings.testAnthropic();
      setTestStatus(r.success ? 'ok' : 'fail');
      setTestMessage(r.message);
    } catch { setTestStatus('fail'); setTestMessage('Request failed'); }
  };

  return (
    <div className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">API Key</label>
        <div className="flex gap-2">
          <div className="relative flex-1">
            <input type={showKey ? 'text' : 'password'}
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 pr-10"
              placeholder="sk-ant-... (leave blank to keep existing)"
              value={apiKey} onChange={e => setApiKey(e.target.value)} />
            <button type="button" onClick={() => setShowKey(v => !v)}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
              {showKey ? <EyeOff size={15} /> : <Eye size={15} />}
            </button>
          </div>
          <button onClick={handleTest} disabled={testStatus === 'testing'}
            className="px-3 py-2 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-50 whitespace-nowrap flex items-center gap-2">
            {testStatus === 'testing' && <Loader2 size={14} className="animate-spin" />}
            Test connection
          </button>
        </div>
        {testStatus === 'ok' && <p className="mt-1.5 text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} />{testMessage}</p>}
        {testStatus === 'fail' && <p className="mt-1.5 text-xs text-red-600 flex items-center gap-1"><XCircle size={13} />{testMessage}</p>}
      </div>
      <div className="flex items-center justify-between pt-2 border-t border-slate-100">
        {saved ? <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p> : <span />}
        <button onClick={handleSave} disabled={saving}
          className="bg-amber-700 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-amber-800 disabled:opacity-50 flex items-center gap-2">
          {saving && <Loader2 size={14} className="animate-spin" />}
          Save changes
        </button>
      </div>
    </div>
  );
}

function AzurePanel({ initialEndpoint, initialDeployment }: { initialEndpoint: string; initialDeployment: string }) {
  const [endpoint, setEndpoint] = useState(initialEndpoint);
  const [apiKey, setApiKey] = useState('');
  const [deployment, setDeployment] = useState(initialDeployment);
  const [showKey, setShowKey] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [testStatus, setTestStatus] = useState<TestStatus>('idle');
  const [testMessage, setTestMessage] = useState('');

  useEffect(() => { setEndpoint(initialEndpoint); setDeployment(initialDeployment); }, [initialEndpoint, initialDeployment]);

  const handleSave = async () => {
    setSaving(true); setSaved(false);
    try {
      const updates: Record<string, string> = {};
      if (endpoint) updates['llm.azure.endpoint'] = endpoint;
      if (apiKey) updates['llm.azure.apiKey'] = apiKey;
      if (deployment) updates['llm.azure.deploymentName'] = deployment;
      if (Object.keys(updates).length) await api.settings.update(updates);
      setSaved(true); setApiKey('');
      setTimeout(() => setSaved(false), 3000);
    } finally { setSaving(false); }
  };

  const handleTest = async () => {
    setTestStatus('testing'); setTestMessage('');
    try {
      const r = await api.settings.testAzure();
      setTestStatus(r.success ? 'ok' : 'fail');
      setTestMessage(r.message);
    } catch { setTestStatus('fail'); setTestMessage('Request failed'); }
  };

  return (
    <div className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">Endpoint</label>
        <input type="text"
          className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="https://your-resource.openai.azure.com"
          value={endpoint} onChange={e => setEndpoint(e.target.value)} />
      </div>
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">API Key</label>
        <div className="relative">
          <input type={showKey ? 'text' : 'password'}
            className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 pr-10"
            placeholder="Leave blank to keep existing"
            value={apiKey} onChange={e => setApiKey(e.target.value)} />
          <button type="button" onClick={() => setShowKey(v => !v)}
            className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
            {showKey ? <EyeOff size={15} /> : <Eye size={15} />}
          </button>
        </div>
      </div>
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">Deployment Name</label>
        <input type="text"
          className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          placeholder="e.g. gpt-4o-deployment"
          value={deployment} onChange={e => setDeployment(e.target.value)} />
      </div>
      {testStatus === 'ok' && <p className="text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} />{testMessage}</p>}
      {testStatus === 'fail' && <p className="text-xs text-red-600 flex items-center gap-1"><XCircle size={13} />{testMessage}</p>}
      <div className="flex items-center justify-between pt-2 border-t border-slate-100">
        <div className="flex items-center gap-3">
          {saved && <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p>}
          <button onClick={handleTest} disabled={testStatus === 'testing'}
            className="px-3 py-2 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-50 flex items-center gap-2">
            {testStatus === 'testing' && <Loader2 size={14} className="animate-spin" />}
            Test connection
          </button>
        </div>
        <button onClick={handleSave} disabled={saving}
          className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2">
          {saving && <Loader2 size={14} className="animate-spin" />}
          Save changes
        </button>
      </div>
    </div>
  );
}

function OllamaPanel({ initialUrl }: { initialUrl: string }) {
  const [baseUrl, setBaseUrl] = useState(initialUrl);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [testStatus, setTestStatus] = useState<TestStatus>('idle');
  const [testMessage, setTestMessage] = useState('');

  useEffect(() => { setBaseUrl(initialUrl); }, [initialUrl]);

  const handleSave = async () => {
    setSaving(true); setSaved(false);
    try {
      if (baseUrl) await api.settings.update({ 'llm.ollama.baseUrl': baseUrl });
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } finally { setSaving(false); }
  };

  const handleTest = async () => {
    setTestStatus('testing'); setTestMessage('');
    try {
      const r = await api.settings.testOllama();
      setTestStatus(r.success ? 'ok' : 'fail');
      setTestMessage(r.message);
    } catch { setTestStatus('fail'); setTestMessage('Request failed'); }
  };

  return (
    <div className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">Base URL</label>
        <input type="text"
          className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
          placeholder="http://localhost:11434"
          value={baseUrl} onChange={e => setBaseUrl(e.target.value)} />
        <p className="text-xs text-slate-400 mt-1">Default: <code className="bg-slate-100 px-1 rounded">http://localhost:11434</code></p>
      </div>
      {testStatus === 'ok' && <p className="text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} />{testMessage}</p>}
      {testStatus === 'fail' && <p className="text-xs text-red-600 flex items-center gap-1"><XCircle size={13} />{testMessage}</p>}
      <div className="flex items-center justify-between pt-2 border-t border-slate-100">
        <div className="flex items-center gap-3">
          {saved && <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p>}
          <button onClick={handleTest} disabled={testStatus === 'testing'}
            className="px-3 py-2 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-50 flex items-center gap-2">
            {testStatus === 'testing' && <Loader2 size={14} className="animate-spin" />}
            Test connection
          </button>
        </div>
        <button onClick={handleSave} disabled={saving}
          className="bg-green-700 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-green-800 disabled:opacity-50 flex items-center gap-2">
          {saving && <Loader2 size={14} className="animate-spin" />}
          Save changes
        </button>
      </div>
    </div>
  );
}

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
          className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          placeholder="xxxx.apps.googleusercontent.com"
          value={clientId} onChange={e => setClientId(e.target.value)} />
      </div>
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">Client Secret</label>
        <div className="relative">
          <input type={showSecret ? 'text' : 'password'}
            className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 pr-10"
            placeholder="Leave blank to keep existing"
            value={clientSecret} onChange={e => setClientSecret(e.target.value)} />
          <button type="button" onClick={() => setShowSecret(v => !v)}
            className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
            {showSecret ? <EyeOff size={15} /> : <Eye size={15} />}
          </button>
        </div>
      </div>
      <p className="text-xs text-slate-400">
        Once saved, click <strong>Connect Gmail account</strong> in any GmailReadNode config drawer.
      </p>
      <div className="flex items-center justify-between pt-2 border-t border-slate-100">
        {saved ? <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p> : <span />}
        <button onClick={handleSave} disabled={saving}
          className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50 flex items-center gap-2">
          {saving && <Loader2 size={14} className="animate-spin" />}
          Save changes
        </button>
      </div>
    </div>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────
export default function ProvidersPage() {
  const [selected, setSelected] = useState<ProviderId>('openai');
  const [open, setOpen] = useState(false);

  // Pre-loaded non-secret settings
  const [defaultModel, setDefaultModel] = useState('gpt-4o-mini');
  const [gmailClientId, setGmailClientId] = useState('');
  const [azureEndpoint, setAzureEndpoint] = useState('');
  const [azureDeployment, setAzureDeployment] = useState('');
  const [ollamaBaseUrl, setOllamaBaseUrl] = useState('http://localhost:11434');

  useEffect(() => {
    api.settings.get().then(s => {
      if (s['llm.defaultModel']) setDefaultModel(s['llm.defaultModel'] ?? 'gpt-4o-mini');
      if (s['gmail.clientId']) setGmailClientId(s['gmail.clientId'] ?? '');
      if (s['llm.azure.endpoint']) setAzureEndpoint(s['llm.azure.endpoint'] ?? '');
      if (s['llm.azure.deploymentName']) setAzureDeployment(s['llm.azure.deploymentName'] ?? '');
      if (s['llm.ollama.baseUrl']) setOllamaBaseUrl(s['llm.ollama.baseUrl'] ?? 'http://localhost:11434');
    }).catch(() => {});
  }, []);

  const current = PROVIDERS.find(p => p.id === selected)!;

  return (
    <div>
      <PageHeader title="AI Providers" subtitle="Configure credentials and options for each integration" />

      <div className="mt-6 space-y-6">
        {/* Provider selector */}
        <div className="bg-white border border-slate-200 rounded-xl p-4">
          <label className="block text-xs font-medium text-slate-500 uppercase tracking-wide mb-2">
            Select provider
          </label>
          <div className="relative">
            <button
              type="button"
              onClick={() => setOpen(v => !v)}
              className="w-full flex items-center gap-3 px-4 py-3 border border-slate-200 rounded-xl bg-white hover:bg-slate-50 transition-colors text-left"
            >
              <ProviderIcon id={selected} />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-slate-900">{current.label}</p>
                <p className="text-xs text-slate-500 truncate">{current.description}</p>
              </div>
              <ChevronDown size={16} className={`text-slate-400 transition-transform shrink-0 ${open ? 'rotate-180' : ''}`} />
            </button>

            {open && (
              <div className="absolute top-full left-0 right-0 mt-1 bg-white border border-slate-200 rounded-xl shadow-lg z-10 overflow-hidden">
                {PROVIDERS.map(p => (
                  <button
                    key={p.id}
                    type="button"
                    onClick={() => { setSelected(p.id); setOpen(false); }}
                    className={`w-full flex items-center gap-3 px-4 py-3 hover:bg-slate-50 transition-colors text-left ${p.id === selected ? 'bg-indigo-50' : ''}`}
                  >
                    <ProviderIcon id={p.id} />
                    <div className="flex-1 min-w-0">
                      <p className={`text-sm font-medium ${p.id === selected ? 'text-indigo-700' : 'text-slate-900'}`}>{p.label}</p>
                      <p className="text-xs text-slate-500 truncate">{p.description}</p>
                    </div>
                    {p.id === selected && <CheckCircle size={15} className="text-indigo-600 shrink-0" />}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Config panel */}
        <div className="bg-white border border-slate-200 rounded-xl">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center gap-3">
            <ProviderIcon id={selected} />
            <div>
              <p className="text-sm font-semibold text-slate-900">{current.label} Configuration</p>
              <p className="text-xs text-slate-500">{current.description}</p>
            </div>
          </div>
          <div className="p-6">
            {selected === 'openai'    && <OpenAIPanel    initialModel={defaultModel} />}
            {selected === 'anthropic' && <AnthropicPanel />}
            {selected === 'azure'     && <AzurePanel     initialEndpoint={azureEndpoint} initialDeployment={azureDeployment} />}
            {selected === 'ollama'    && <OllamaPanel    initialUrl={ollamaBaseUrl} />}
            {selected === 'gmail'     && <GmailPanel     initialClientId={gmailClientId} />}
          </div>
        </div>
      </div>
    </div>
  );
}
