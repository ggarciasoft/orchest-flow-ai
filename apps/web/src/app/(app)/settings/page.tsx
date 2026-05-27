'use client';
import { useState, useEffect } from 'react';
import { PageHeader } from '@/components/ui';
import { api, SecretSummary } from '@/lib/api';
import { CheckCircle, XCircle, Loader2, Eye, EyeOff, Trash2, Plus, Lock, Mail, Cpu, Cloud, Server } from 'lucide-react';

type TestStatus = 'idle' | 'testing' | 'ok' | 'fail';

const MODELS = ['gpt-4o', 'gpt-4o-mini', 'gpt-4-turbo', 'gpt-3.5-turbo'];

export default function SettingsPage() {
  // OpenAI
  const [apiKey, setApiKey] = useState('');
  const [defaultModel, setDefaultModel] = useState('gpt-4o-mini');
  const [showKey, setShowKey] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [testStatus, setTestStatus] = useState<TestStatus>('idle');
  const [testMessage, setTestMessage] = useState('');

  // Gmail
  const [gmailClientId, setGmailClientId] = useState('');
  const [gmailClientSecret, setGmailClientSecret] = useState('');
  const [showGmailSecret, setShowGmailSecret] = useState(false);
  const [gmailSaving, setGmailSaving] = useState(false);
  const [gmailSaved, setGmailSaved] = useState(false);

  // Anthropic
  const [anthropicKey, setAnthropicKey] = useState('');
  const [showAnthropicKey, setShowAnthropicKey] = useState(false);
  const [anthropicSaving, setAnthropicSaving] = useState(false);
  const [anthropicSaved, setAnthropicSaved] = useState(false);
  const [anthropicTestStatus, setAnthropicTestStatus] = useState<TestStatus>('idle');
  const [anthropicTestMessage, setAnthropicTestMessage] = useState('');

  // Azure OpenAI
  const [azureEndpoint, setAzureEndpoint] = useState('');
  const [azureApiKey, setAzureApiKey] = useState('');
  const [azureDeployment, setAzureDeployment] = useState('');
  const [showAzureKey, setShowAzureKey] = useState(false);
  const [azureSaving, setAzureSaving] = useState(false);
  const [azureSaved, setAzureSaved] = useState(false);
  const [azureTestStatus, setAzureTestStatus] = useState<TestStatus>('idle');
  const [azureTestMessage, setAzureTestMessage] = useState('');

  // Ollama
  const [ollamaBaseUrl, setOllamaBaseUrl] = useState('http://localhost:11434');
  const [ollamaSaving, setOllamaSaving] = useState(false);
  const [ollamaSaved, setOllamaSaved] = useState(false);
  const [ollamaTestStatus, setOllamaTestStatus] = useState<TestStatus>('idle');
  const [ollamaTestMessage, setOllamaTestMessage] = useState('');

  // Secret Vault
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
      if (s['llm.azure.endpoint']) setAzureEndpoint(s['llm.azure.endpoint'] ?? '');
      if (s['llm.azure.deploymentName']) setAzureDeployment(s['llm.azure.deploymentName'] ?? '');
      if (s['llm.ollama.baseUrl']) setOllamaBaseUrl(s['llm.ollama.baseUrl'] ?? 'http://localhost:11434');
      // Never pre-fill secrets — user must re-enter to change
    }).catch(() => {});
    loadSecrets();
  }, []);

  // ---- OpenAI handlers ----
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

  // ---- Gmail handlers ----
  const handleSaveGmail = async () => {
    setGmailSaving(true); setGmailSaved(false);
    try {
      const updates: Record<string, string> = {};
      if (gmailClientId) updates['gmail.clientId'] = gmailClientId;
      if (gmailClientSecret) updates['gmail.clientSecret'] = gmailClientSecret;
      if (Object.keys(updates).length > 0) await api.settings.update(updates);
      setGmailSaved(true); setGmailClientSecret('');
      setTimeout(() => setGmailSaved(false), 3000);
    } finally { setGmailSaving(false); }
  };

  // ---- Anthropic handlers ----
  const handleSaveAnthropic = async () => {
    setAnthropicSaving(true); setAnthropicSaved(false);
    try {
      const updates: Record<string, string> = {};
      if (anthropicKey) updates['llm.anthropic.apiKey'] = anthropicKey;
      if (Object.keys(updates).length > 0) await api.settings.update(updates);
      setAnthropicSaved(true); setAnthropicKey('');
      setTimeout(() => setAnthropicSaved(false), 3000);
    } finally { setAnthropicSaving(false); }
  };

  const handleTestAnthropic = async () => {
    setAnthropicTestStatus('testing'); setAnthropicTestMessage('');
    try {
      const r = await api.settings.testAnthropic();
      setAnthropicTestStatus(r.success ? 'ok' : 'fail');
      setAnthropicTestMessage(r.message);
    } catch { setAnthropicTestStatus('fail'); setAnthropicTestMessage('Request failed'); }
  };

  // ---- Azure OpenAI handlers ----
  const handleSaveAzure = async () => {
    setAzureSaving(true); setAzureSaved(false);
    try {
      const updates: Record<string, string> = {};
      if (azureEndpoint) updates['llm.azure.endpoint'] = azureEndpoint;
      if (azureApiKey) updates['llm.azure.apiKey'] = azureApiKey;
      if (azureDeployment) updates['llm.azure.deploymentName'] = azureDeployment;
      if (Object.keys(updates).length > 0) await api.settings.update(updates);
      setAzureSaved(true); setAzureApiKey('');
      setTimeout(() => setAzureSaved(false), 3000);
    } finally { setAzureSaving(false); }
  };

  const handleTestAzure = async () => {
    setAzureTestStatus('testing'); setAzureTestMessage('');
    try {
      const r = await api.settings.testAzure();
      setAzureTestStatus(r.success ? 'ok' : 'fail');
      setAzureTestMessage(r.message);
    } catch { setAzureTestStatus('fail'); setAzureTestMessage('Request failed'); }
  };

  // ---- Ollama handlers ----
  const handleSaveOllama = async () => {
    setOllamaSaving(true); setOllamaSaved(false);
    try {
      const updates: Record<string, string> = {};
      if (ollamaBaseUrl) updates['llm.ollama.baseUrl'] = ollamaBaseUrl;
      if (Object.keys(updates).length > 0) await api.settings.update(updates);
      setOllamaSaved(true);
      setTimeout(() => setOllamaSaved(false), 3000);
    } finally { setOllamaSaving(false); }
  };

  const handleTestOllama = async () => {
    setOllamaTestStatus('testing'); setOllamaTestMessage('');
    try {
      const r = await api.settings.testOllama();
      setOllamaTestStatus(r.success ? 'ok' : 'fail');
      setOllamaTestMessage(r.message);
    } catch { setOllamaTestStatus('fail'); setOllamaTestMessage('Request failed'); }
  };

  // ---- Secret handlers ----
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
    if (!confirm('Delete this secret? Workflows referencing it will break.')) return;
    try { await api.secrets.delete(id); await loadSecrets(); } catch { /* ignore */ }
  };

  return (
    <div>
      <PageHeader title="Settings" subtitle="Configure AI providers and platform settings" />

      <div className="space-y-6 mt-6">

        {/* ── OpenAI ── */}
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
              <label className="block text-sm font-medium text-slate-700 mb-1">API Key</label>
              <div className="flex gap-2">
                <div className="relative flex-1">
                  <input
                    type={showKey ? 'text' : 'password'}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 pr-10"
                    placeholder="sk-... (leave blank to keep existing)"
                    value={apiKey}
                    onChange={e => setApiKey(e.target.value)}
                  />
                  <button type="button" onClick={() => setShowKey(v => !v)}
                    className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                    {showKey ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
                <button onClick={handleTest} disabled={testStatus === 'testing'}
                  className="px-3 py-2 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-50 whitespace-nowrap flex items-center gap-2">
                  {testStatus === 'testing' && <Loader2 size={14} className="animate-spin" />}
                  Test connection
                </button>
              </div>
              {testStatus === 'ok' && <p className="mt-1.5 text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} /> {testMessage}</p>}
              {testStatus === 'fail' && <p className="mt-1.5 text-xs text-red-600 flex items-center gap-1"><XCircle size={13} /> {testMessage}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Default Model</label>
              <select
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                value={defaultModel} onChange={e => setDefaultModel(e.target.value)}>
                {MODELS.map(m => <option key={m} value={m}>{m}</option>)}
              </select>
              <p className="text-xs text-slate-400 mt-1">Used when a node&apos;s model is set to &quot;default&quot;.</p>
            </div>
          </div>
          <div className="px-6 py-4 border-t border-slate-200 flex items-center justify-between">
            {saved ? <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p> : <span />}
            <button onClick={handleSave} disabled={saving}
              className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50 flex items-center gap-2">
              {saving && <Loader2 size={14} className="animate-spin" />}
              Save changes
            </button>
          </div>
        </div>

        {/* ── Anthropic ── */}
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center gap-3">
            <div className="w-8 h-8 bg-amber-700 rounded-lg flex items-center justify-center text-white">
              <Cpu size={16} />
            </div>
            <div>
              <p className="text-sm font-semibold text-slate-900">Anthropic</p>
              <p className="text-xs text-slate-500">Claude 3.5 Sonnet, Claude 3 Haiku, Claude 3 Opus</p>
            </div>
          </div>
          <div className="p-6 space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">API Key</label>
              <div className="flex gap-2">
                <div className="relative flex-1">
                  <input
                    type={showAnthropicKey ? 'text' : 'password'}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 pr-10"
                    placeholder="sk-ant-... (leave blank to keep existing)"
                    value={anthropicKey}
                    onChange={e => setAnthropicKey(e.target.value)}
                  />
                  <button type="button" onClick={() => setShowAnthropicKey(v => !v)}
                    className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                    {showAnthropicKey ? <EyeOff size={16} /> : <Eye size={16} />}
                  </button>
                </div>
                <button onClick={handleTestAnthropic} disabled={anthropicTestStatus === 'testing'}
                  className="px-3 py-2 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-50 whitespace-nowrap flex items-center gap-2">
                  {anthropicTestStatus === 'testing' && <Loader2 size={14} className="animate-spin" />}
                  Test connection
                </button>
              </div>
              {anthropicTestStatus === 'ok' && <p className="mt-1.5 text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} /> {anthropicTestMessage}</p>}
              {anthropicTestStatus === 'fail' && <p className="mt-1.5 text-xs text-red-600 flex items-center gap-1"><XCircle size={13} /> {anthropicTestMessage}</p>}
            </div>
          </div>
          <div className="px-6 py-4 border-t border-slate-200 flex items-center justify-between">
            {anthropicSaved ? <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p> : <span />}
            <button onClick={handleSaveAnthropic} disabled={anthropicSaving}
              className="bg-amber-700 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-amber-800 disabled:opacity-50 flex items-center gap-2">
              {anthropicSaving && <Loader2 size={14} className="animate-spin" />}
              Save changes
            </button>
          </div>
        </div>

        {/* ── Azure OpenAI ── */}
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center gap-3">
            <div className="w-8 h-8 bg-blue-600 rounded-lg flex items-center justify-center text-white">
              <Cloud size={16} />
            </div>
            <div>
              <p className="text-sm font-semibold text-slate-900">Azure OpenAI</p>
              <p className="text-xs text-slate-500">Hosted GPT models via Azure deployment</p>
            </div>
          </div>
          <div className="p-6 space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Endpoint</label>
              <input type="text"
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="https://your-resource.openai.azure.com"
                value={azureEndpoint}
                onChange={e => setAzureEndpoint(e.target.value)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">API Key</label>
              <div className="relative">
                <input type={showAzureKey ? 'text' : 'password'}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 pr-10"
                  placeholder="Leave blank to keep existing"
                  value={azureApiKey}
                  onChange={e => setAzureApiKey(e.target.value)}
                />
                <button type="button" onClick={() => setShowAzureKey(v => !v)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                  {showAzureKey ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Deployment Name</label>
              <input type="text"
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="e.g. gpt-4o-deployment"
                value={azureDeployment}
                onChange={e => setAzureDeployment(e.target.value)}
              />
            </div>
            {azureTestStatus === 'ok' && <p className="text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} /> {azureTestMessage}</p>}
            {azureTestStatus === 'fail' && <p className="text-xs text-red-600 flex items-center gap-1"><XCircle size={13} /> {azureTestMessage}</p>}
          </div>
          <div className="px-6 py-4 border-t border-slate-200 flex items-center justify-between">
            <div className="flex items-center gap-3">
              {azureSaved && <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p>}
              <button onClick={handleTestAzure} disabled={azureTestStatus === 'testing'}
                className="px-3 py-2 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-50 flex items-center gap-2">
                {azureTestStatus === 'testing' && <Loader2 size={14} className="animate-spin" />}
                Test connection
              </button>
            </div>
            <button onClick={handleSaveAzure} disabled={azureSaving}
              className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2">
              {azureSaving && <Loader2 size={14} className="animate-spin" />}
              Save changes
            </button>
          </div>
        </div>

        {/* ── Ollama ── */}
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center gap-3">
            <div className="w-8 h-8 bg-green-700 rounded-lg flex items-center justify-center text-white">
              <Server size={16} />
            </div>
            <div>
              <p className="text-sm font-semibold text-slate-900">Ollama</p>
              <p className="text-xs text-slate-500">Local models — Llama 3, Mistral, Phi-3, Gemma 2</p>
            </div>
          </div>
          <div className="p-6 space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Base URL</label>
              <input type="text"
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                placeholder="http://localhost:11434"
                value={ollamaBaseUrl}
                onChange={e => setOllamaBaseUrl(e.target.value)}
              />
              <p className="text-xs text-slate-400 mt-1">Default: <code className="bg-slate-100 px-1 rounded">http://localhost:11434</code></p>
            </div>
            {ollamaTestStatus === 'ok' && <p className="text-xs text-green-600 flex items-center gap-1"><CheckCircle size={13} /> {ollamaTestMessage}</p>}
            {ollamaTestStatus === 'fail' && <p className="text-xs text-red-600 flex items-center gap-1"><XCircle size={13} /> {ollamaTestMessage}</p>}
          </div>
          <div className="px-6 py-4 border-t border-slate-200 flex items-center justify-between">
            <div className="flex items-center gap-3">
              {ollamaSaved && <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p>}
              <button onClick={handleTestOllama} disabled={ollamaTestStatus === 'testing'}
                className="px-3 py-2 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 text-slate-700 disabled:opacity-50 flex items-center gap-2">
                {ollamaTestStatus === 'testing' && <Loader2 size={14} className="animate-spin" />}
                Test connection
              </button>
            </div>
            <button onClick={handleSaveOllama} disabled={ollamaSaving}
              className="bg-green-700 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-green-800 disabled:opacity-50 flex items-center gap-2">
              {ollamaSaving && <Loader2 size={14} className="animate-spin" />}
              Save changes
            </button>
          </div>
        </div>

        {/* ── Gmail ── */}
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
              <input type="text"
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                placeholder="xxxx.apps.googleusercontent.com"
                value={gmailClientId} onChange={e => setGmailClientId(e.target.value)} />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Client Secret</label>
              <div className="relative">
                <input type={showGmailSecret ? 'text' : 'password'}
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 pr-10"
                  placeholder="Leave blank to keep existing"
                  value={gmailClientSecret} onChange={e => setGmailClientSecret(e.target.value)} />
                <button type="button" onClick={() => setShowGmailSecret(v => !v)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                  {showGmailSecret ? <EyeOff size={16} /> : <Eye size={16} />}
                </button>
              </div>
            </div>
            <p className="text-xs text-slate-400">
              Once saved, click <strong>Connect Gmail account</strong> in any GmailReadNode config drawer — no need to re-enter credentials each time.
            </p>
          </div>
          <div className="px-6 py-4 border-t border-slate-200 flex items-center justify-between">
            {gmailSaved ? <p className="text-sm text-green-600 flex items-center gap-1"><CheckCircle size={14} /> Saved</p> : <span />}
            <button onClick={handleSaveGmail} disabled={gmailSaving}
              className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50 flex items-center gap-2">
              {gmailSaving && <Loader2 size={14} className="animate-spin" />}
              Save changes
            </button>
          </div>
        </div>

        {/* ── Secret Vault ── */}
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center gap-3">
            <div className="w-8 h-8 bg-amber-600 rounded-lg flex items-center justify-center text-white"><Lock size={16} /></div>
            <div>
              <p className="text-sm font-semibold text-slate-900">Secret Vault</p>
              <p className="text-xs text-slate-500">
                Encrypted named values. Reference in any node config as{' '}
                <code className="bg-slate-100 px-1 rounded">{'{{secret:name}}'}</code>
              </p>
            </div>
          </div>
          <div className="p-6 space-y-4">
            {secretsLoading ? (
              <div className="flex items-center gap-2 text-slate-400 text-sm"><Loader2 size={14} className="animate-spin" /> Loading secrets…</div>
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
                    <button onClick={() => handleDeleteSecret(s.id)} className="text-slate-400 hover:text-red-500 transition-colors">
                      <Trash2 size={15} />
                    </button>
                  </li>
                ))}
              </ul>
            )}
            <div className="border-t border-slate-100 pt-4">
              <p className="text-xs font-medium text-slate-600 mb-2">Add new secret</p>
              <div className="flex gap-2">
                <input type="text" placeholder="Name (e.g. openai-key)"
                  value={newSecretName} onChange={e => setNewSecretName(e.target.value)}
                  className="flex-1 border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-500" />
                <div className="relative flex-1">
                  <input type={showSecretValue ? 'text' : 'password'} placeholder="Secret value"
                    value={newSecretValue} onChange={e => setNewSecretValue(e.target.value)}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-amber-500 pr-8" />
                  <button type="button" onClick={() => setShowSecretValue(v => !v)}
                    className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600">
                    {showSecretValue ? <EyeOff size={14} /> : <Eye size={14} />}
                  </button>
                </div>
                <button onClick={handleAddSecret} disabled={addingSecret}
                  className="px-3 py-2 bg-amber-600 text-white rounded-lg text-sm font-medium hover:bg-amber-700 disabled:opacity-50 flex items-center gap-1">
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
