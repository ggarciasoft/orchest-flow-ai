'use client';
import { useState, useEffect } from 'react';
import { PageHeader } from '@/components/ui';
import { AdminPageGuard } from '@/components/AdminPageGuard';
import { api } from '@/lib/api';
import {
  CheckCircle, XCircle, Loader2, Eye, EyeOff,
  Cpu, Cloud, Server, ChevronDown,
} from 'lucide-react';

type TestStatus = 'idle' | 'testing' | 'ok' | 'fail';

// ── Provider registry ────────────────────────────────────────────────────────
// To add a new provider: add an entry here and implement its panel below.
const PROVIDERS = [
  { id: 'openai',    label: 'OpenAI',       description: 'GPT-4o, GPT-4o-mini, GPT-3.5-turbo',               iconBg: 'bg-slate-900' },
  { id: 'anthropic', label: 'Anthropic',    description: 'Claude 3.5 Sonnet, Claude 3 Haiku, Claude 3 Opus',  iconBg: 'bg-amber-700' },
  { id: 'azure',     label: 'Azure OpenAI', description: 'Hosted GPT models via Azure deployment',            iconBg: 'bg-blue-600'  },
  { id: 'ollama',    label: 'Ollama',       description: 'Local models — Llama 3, Mistral, Phi-3, Gemma 2',  iconBg: 'bg-green-700' },
  { id: 'deepseek',  label: 'DeepSeek',     description: 'deepseek-chat, deepseek-reasoner',                  iconBg: 'bg-cyan-700'  },
] as const;

type ProviderId = typeof PROVIDERS[number]['id'];

const MODELS = ['gpt-4o', 'gpt-4o-mini', 'gpt-4-turbo', 'gpt-3.5-turbo'];

// ── Icon helper ──────────────────────────────────────────────────────────────
function ProviderIcon({ id }: { id: ProviderId }) {
  const p = PROVIDERS.find(x => x.id === id)!;
  const base = `w-9 h-9 ${p.iconBg} rounded-lg flex items-center justify-center shrink-0`;
  if (id === 'openai')    return <div className={base}><span className="text-white text-xs font-bold">AI</span></div>;
  if (id === 'anthropic') return <div className={base}><Cpu size={16} className="text-white" /></div>;
  if (id === 'azure')     return <div className={base}><Cloud size={16} className="text-white" /></div>;
  if (id === 'deepseek')  return <div className={base}><span className="text-white text-xs font-bold">DS</span></div>;
  return                         <div className={base}><Server size={16} className="text-white" /></div>;
}

// ── Provider panels ──────────────────────────────────────────────────────────

function OpenAIPanel({ 
  initialModel, 
  isDefault, 
  onSetDefault 
}: { 
  initialModel: string; 
  isDefault: boolean; 
  onSetDefault: () => void;
}) {
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

  const handleSetDefault = async () => {
    setSaving(true);
    try {
      await api.settings.update({ 
        'llm.defaultProvider': 'openai',
        'llm.defaultModel': defaultModel 
      });
      onSetDefault();
      setSaved(true);
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
      <div className="flex items-center justify-between mb-4 pb-3 border-b border-slate-100">
        <span className="text-xs font-medium text-slate-500 uppercase tracking-wide">Provider status</span>
        {isDefault ? (
          <span className="px-2 py-1 bg-green-50 text-green-700 text-xs font-medium rounded-md flex items-center gap-1">
            <CheckCircle size={12} /> Active default
          </span>
        ) : (
          <button 
            onClick={handleSetDefault}
            disabled={saving}
            className="px-3 py-1 border border-indigo-200 text-indigo-700 text-xs font-medium rounded-md hover:bg-indigo-50 disabled:opacity-50"
          >
            Set as default provider
          </button>
        )}
      </div>
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
        {testStatus === 'ok'   && <p className="mt-1.5 text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} />{testMessage}</p>}
        {testStatus === 'fail' && <p className="mt-1.5 text-xs text-red-600   flex items-center gap-1"><XCircle    size={13} />{testMessage}</p>}
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

function AnthropicPanel({ 
  isDefault, 
  onSetDefault 
}: { 
  isDefault: boolean; 
  onSetDefault: () => void;
}) {
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

  const handleSetDefault = async () => {
    setSaving(true);
    try {
      await api.settings.update({ 'llm.defaultProvider': 'anthropic' });
      onSetDefault();
      setSaved(true);
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
      <div className="flex items-center justify-between mb-4 pb-3 border-b border-slate-100">
        <span className="text-xs font-medium text-slate-500 uppercase tracking-wide">Provider status</span>
        {isDefault ? (
          <span className="px-2 py-1 bg-green-50 text-green-700 text-xs font-medium rounded-md flex items-center gap-1">
            <CheckCircle size={12} /> Active default
          </span>
        ) : (
          <button 
            onClick={handleSetDefault}
            disabled={saving}
            className="px-3 py-1 border border-amber-200 text-amber-700 text-xs font-medium rounded-md hover:bg-amber-50 disabled:opacity-50"
          >
            Set as default provider
          </button>
        )}
      </div>
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
        {testStatus === 'ok'   && <p className="mt-1.5 text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} />{testMessage}</p>}
        {testStatus === 'fail' && <p className="mt-1.5 text-xs text-red-600   flex items-center gap-1"><XCircle    size={13} />{testMessage}</p>}
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

function AzurePanel({ 
  initialEndpoint, 
  initialDeployment, 
  isDefault, 
  onSetDefault 
}: { 
  initialEndpoint: string; 
  initialDeployment: string; 
  isDefault: boolean; 
  onSetDefault: () => void;
}) {
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

  const handleSetDefault = async () => {
    setSaving(true);
    try {
      await api.settings.update({ 'llm.defaultProvider': 'azure' });
      onSetDefault();
      setSaved(true);
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
      <div className="flex items-center justify-between mb-4 pb-3 border-b border-slate-100">
        <span className="text-xs font-medium text-slate-500 uppercase tracking-wide">Provider status</span>
        {isDefault ? (
          <span className="px-2 py-1 bg-green-50 text-green-700 text-xs font-medium rounded-md flex items-center gap-1">
            <CheckCircle size={12} /> Active default
          </span>
        ) : (
          <button 
            onClick={handleSetDefault}
            disabled={saving}
            className="px-3 py-1 border border-blue-200 text-blue-700 text-xs font-medium rounded-md hover:bg-blue-50 disabled:opacity-50"
          >
            Set as default provider
          </button>
        )}
      </div>
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
      {testStatus === 'ok'   && <p className="text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} />{testMessage}</p>}
      {testStatus === 'fail' && <p className="text-xs text-red-600   flex items-center gap-1"><XCircle    size={13} />{testMessage}</p>}
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

function OllamaPanel({ 
  initialUrl, 
  isDefault, 
  onSetDefault 
}: { 
  initialUrl: string; 
  isDefault: boolean; 
  onSetDefault: () => void;
}) {
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

  const handleSetDefault = async () => {
    setSaving(true);
    try {
      await api.settings.update({ 'llm.defaultProvider': 'ollama' });
      onSetDefault();
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
      <div className="flex items-center justify-between mb-4 pb-3 border-b border-slate-100">
        <span className="text-xs font-medium text-slate-500 uppercase tracking-wide">Provider status</span>
        {isDefault ? (
          <span className="px-2 py-1 bg-green-50 text-green-700 text-xs font-medium rounded-md flex items-center gap-1">
            <CheckCircle size={12} /> Active default
          </span>
        ) : (
          <button 
            onClick={handleSetDefault}
            disabled={saving}
            className="px-3 py-1 border border-green-200 text-green-700 text-xs font-medium rounded-md hover:bg-green-50 disabled:opacity-50"
          >
            Set as default provider
          </button>
        )}
      </div>
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">Base URL</label>
        <input type="text"
          className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
          placeholder="http://localhost:11434"
          value={baseUrl} onChange={e => setBaseUrl(e.target.value)} />
        <p className="text-xs text-slate-400 mt-1">Default: <code className="bg-slate-100 px-1 rounded">http://localhost:11434</code></p>
      </div>
      {testStatus === 'ok'   && <p className="text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} />{testMessage}</p>}
      {testStatus === 'fail' && <p className="text-xs text-red-600   flex items-center gap-1"><XCircle    size={13} />{testMessage}</p>}
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

function DeepSeekPanel({ isDefault, onSetDefault }: { isDefault: boolean; onSetDefault: () => void }) {
  const [apiKey, setApiKey] = useState('');
  const [showKey, setShowKey] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  const handleSave = async () => {
    setSaving(true); setSaved(false);
    try {
      if (apiKey) await api.settings.update({ 'llm.deepseek.apiKey': apiKey });
      setSaved(true); setApiKey('');
      setTimeout(() => setSaved(false), 3000);
    } finally { setSaving(false); }
  };

  const handleSetDefault = async () => {
    setSaving(true);
    try {
      await api.settings.update({ 'llm.defaultProvider': 'deepseek', 'llm.defaultModel': 'deepseek-chat' });
      onSetDefault();
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } finally { setSaving(false); }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between mb-4 pb-3 border-b border-slate-100">
        <span className="text-xs font-medium text-slate-500 uppercase tracking-wide">Provider status</span>
        {isDefault ? (
          <span className="px-2 py-1 bg-green-50 text-green-700 text-xs font-medium rounded-md flex items-center gap-1">
            <CheckCircle size={12} /> Active default
          </span>
        ) : (
          <button onClick={handleSetDefault} disabled={saving}
            className="px-3 py-1 border border-cyan-200 text-cyan-700 text-xs font-medium rounded-md hover:bg-cyan-50 disabled:opacity-50">
            Set as default provider
          </button>
        )}
      </div>
      <div>
        <label className="block text-sm font-medium text-slate-700 mb-1">API Key</label>
        <div className="relative">
          <input type={showKey ? 'text' : 'password'}
            className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-cyan-500 pr-10"
            placeholder="sk-... (leave blank to keep existing). Use {{secret:name}} to reference a vault secret."
            value={apiKey} onChange={e => setApiKey(e.target.value)} />
          <button type="button" onClick={() => setShowKey(v => !v)}
            className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
            {showKey ? <EyeOff size={15} /> : <Eye size={15} />}
          </button>
        </div>
        <p className="text-xs text-slate-400 mt-1">Get your key at <a href="https://platform.deepseek.com" target="_blank" rel="noreferrer" className="underline">platform.deepseek.com</a>. Models: deepseek-chat, deepseek-reasoner.</p>
      </div>
      <div className="flex items-center justify-between pt-2 border-t border-slate-100">
        {saved ? <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p> : <span />}
        <button onClick={handleSave} disabled={saving}
          className="bg-cyan-700 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-cyan-800 disabled:opacity-50 flex items-center gap-2">
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
  const [activeProvider, setActiveProvider] = useState<ProviderId>('openai');

  const [defaultModel, setDefaultModel] = useState('gpt-4o-mini');
  const [azureEndpoint, setAzureEndpoint] = useState('');
  const [azureDeployment, setAzureDeployment] = useState('');
  const [ollamaBaseUrl, setOllamaBaseUrl] = useState('http://localhost:11434');

  useEffect(() => {
    api.settings.get().then(s => {
      if (s['llm.defaultModel'])       setDefaultModel(s['llm.defaultModel'] ?? 'gpt-4o-mini');
      if (s['llm.defaultProvider'])    setActiveProvider(s['llm.defaultProvider'] as ProviderId ?? 'openai');
      if (s['llm.azure.endpoint'])     setAzureEndpoint(s['llm.azure.endpoint'] ?? '');
      if (s['llm.azure.deploymentName']) setAzureDeployment(s['llm.azure.deploymentName'] ?? '');
      if (s['llm.ollama.baseUrl'])     setOllamaBaseUrl(s['llm.ollama.baseUrl'] ?? 'http://localhost:11434');
    }).catch(() => {});
  }, []);

  const current = PROVIDERS.find(p => p.id === selected)!;
  const activeProviderLabel = PROVIDERS.find(p => p.id === activeProvider)?.label ?? 'OpenAI';

  return (
    <AdminPageGuard>
    <div>
      <PageHeader title="AI Providers" subtitle="Configure credentials and options for each LLM provider" />

      <div className="mt-6 space-y-6">
        {/* Active provider info */}
        <div className="bg-indigo-50 border border-indigo-100 rounded-xl p-4">
          <div className="flex items-center gap-2 text-sm">
            <span className="text-slate-600">Active AI provider:</span>
            <span className="font-semibold text-slate-900">{activeProviderLabel}</span>
            {activeProvider === 'openai' && (
              <span className="text-slate-500">({defaultModel})</span>
            )}
          </div>
        </div>

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
                      <div className="flex items-center gap-2">
                        <p className={`text-sm font-medium ${p.id === selected ? 'text-indigo-700' : 'text-slate-900'}`}>{p.label}</p>
                        {p.id === activeProvider && (
                          <span className="px-1.5 py-0.5 bg-green-100 text-green-700 text-[10px] font-medium rounded uppercase">
                            Default
                          </span>
                        )}
                      </div>
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
            {selected === 'openai'    && <OpenAIPanel    initialModel={defaultModel} isDefault={activeProvider === 'openai'} onSetDefault={() => setActiveProvider('openai')} />}
            {selected === 'anthropic' && <AnthropicPanel isDefault={activeProvider === 'anthropic'} onSetDefault={() => setActiveProvider('anthropic')} />}
            {selected === 'azure'     && <AzurePanel     initialEndpoint={azureEndpoint} initialDeployment={azureDeployment} isDefault={activeProvider === 'azure'} onSetDefault={() => setActiveProvider('azure')} />}
            {selected === 'ollama'    && <OllamaPanel    initialUrl={ollamaBaseUrl} isDefault={activeProvider === 'ollama'} onSetDefault={() => setActiveProvider('ollama')} />}
            {selected === 'deepseek'  && <DeepSeekPanel  isDefault={activeProvider === 'deepseek'} onSetDefault={() => setActiveProvider('deepseek')} />}
          </div>
        </div>
      </div>
    </div>
    </AdminPageGuard>
  );
}
