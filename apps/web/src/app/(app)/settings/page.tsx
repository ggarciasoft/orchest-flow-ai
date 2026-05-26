'use client';
import { useState, useEffect } from 'react';
import { PageHeader } from '@/components/ui';
import { api } from '@/lib/api';
import { CheckCircle, XCircle, Loader2, Eye, EyeOff } from 'lucide-react';

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

  useEffect(() => {
    api.settings.get().then(s => {
      if (s['llm.defaultModel']) setDefaultModel(s['llm.defaultModel'] ?? 'gpt-4o-mini');
    }).catch(() => {});
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
      </div>
    </div>
  );
}
