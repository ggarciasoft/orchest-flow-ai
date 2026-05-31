'use client';
import { useState, useRef, useEffect } from 'react';
import { X, Send, Loader2 } from 'lucide-react';
import { api, AiAssistResult, NodeDescriptor } from '@/lib/api';

interface Message {
  role: 'user' | 'assistant';
  content: string;
  result?: AiAssistResult;
  provider?: string;
  model?: string;
  totalTokens?: number;
}

function ActiveProviderBadge() {
  const [info, setInfo] = useState<{ provider: string; model: string } | null>(null);
  useEffect(() => {
    api.settings.get().then(s => {
      const provider = s['llm.defaultProvider'] ?? 'openai';
      const model    = s['llm.defaultModel']    ?? 'gpt-4o-mini';
      setInfo({ provider, model });
    }).catch(() => {});
  }, []);
  if (!info) return null;
  return (
    <div className="flex items-center gap-1 mt-1 flex-wrap">
      <span className="text-[10px] bg-indigo-50 text-indigo-600 border border-indigo-200 px-1.5 py-0.5 rounded font-mono">{info.provider}</span>
      <span className="text-[10px] bg-slate-50 text-slate-500 border border-slate-200 px-1.5 py-0.5 rounded font-mono">{info.model}</span>
    </div>
  );
}

interface Props {
  workflowId: string;
  workflowName: string;
  nodeCatalog: NodeDescriptor[];
  onClose: () => void;
  onPreview: (definition: object) => void;
  onAccept: (definition: object) => void;
  getCurrentDefinitionJson: () => string;
}

/**
 * AiAssistPanel — side panel for AI-powered workflow generation.
 * Users describe what they want in natural language; the AI builds or modifies the workflow.
 */
export function AiAssistPanel({ workflowName, onClose, onPreview, onAccept, getCurrentDefinitionJson }: Props) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [aiReady, setAiReady] = useState<boolean | null>(null);
  const [aiNotConfiguredMsg, setAiNotConfiguredMsg] = useState<string>('');
  const chatEndRef = useRef<HTMLDivElement>(null);

  // Check AI provider status on mount
  useEffect(() => {
    api.settings.aiStatus().then(status => {
      setAiReady(status.isDefaultConfigured);
      if (!status.isDefaultConfigured) {
        setAiNotConfiguredMsg(
          `${status.defaultProvider} is not configured. Go to Settings → AI Providers to add your API key.`
        );
      }
    }).catch(() => setAiReady(true));
  }, []);

  // Scroll to bottom when messages change
  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = async () => {
    const trimmed = input.trim();
    if (!trimmed || loading) return;

    const userMessage: Message = { role: 'user', content: trimmed };
    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setLoading(true);

    try {
      const currentDefinitionJson = getCurrentDefinitionJson();
      const result = await api.aiAssist.generate(trimmed, currentDefinitionJson, workflowName);
      const assistantMessage: Message = {
        role: 'assistant',
        content: result.explanation,
        result,
        provider: result.provider,
        model: result.model,
        totalTokens: result.totalTokens,
      };
      setMessages(prev => [...prev, assistantMessage]);
    } catch (e) {
      const assistantMessage: Message = {
        role: 'assistant',
        content: `Error: ${(e as Error).message}`,
      };
      setMessages(prev => [...prev, assistantMessage]);
    } finally {
      setLoading(false);
    }
  };

  const handleKeyDown = (evt: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (evt.key === 'Enter' && !evt.shiftKey) {
      evt.preventDefault();
      handleSend();
    }
  };

  return (
    <div className="w-80 border-l bg-white flex flex-col shrink-0 overflow-hidden shadow-lg">
      {/* Header */}
      <div className="flex items-start justify-between p-4 border-b">
        <div>
          <h3 className="font-semibold text-sm text-slate-900">✨ AI Assistant</h3>
          <p className="text-xs text-slate-400 mt-0.5">Describe what you want, I&apos;ll build it</p>
          <ActiveProviderBadge />
          <p className="text-xs text-amber-600 mt-1">⚠️ AI can make mistakes — always review before saving.</p>
        </div>
        <button onClick={onClose} className="p-1 hover:bg-gray-100 rounded mt-0.5" title="Close">
          <X size={16} />
        </button>
      </div>

      {/* Chat area */}
      <div className="flex-1 overflow-y-auto p-3 flex flex-col gap-3">
        {aiReady === false && (
          <div className="mx-0 mb-2 p-3 bg-amber-50 border border-amber-200 rounded-lg">
            <p className="text-xs text-amber-800 font-medium">AI not configured</p>
            <p className="text-xs text-amber-700 mt-0.5">{aiNotConfiguredMsg}</p>
            <a href="/settings/providers" className="text-xs text-indigo-600 hover:underline mt-1 block">
              Open AI Providers settings →
            </a>
          </div>
        )}

        {messages.length === 0 && (
          <div className="text-center text-xs text-slate-400 mt-6 px-2">
            <p className="mb-1">Start by describing your workflow</p>
            <p className="italic">&quot;Create a workflow that reads Gmail and saves amounts to the database&quot;</p>
          </div>
        )}

        {messages.map((msg, i) => (
          <div key={i} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            {msg.role === 'user' ? (
              <div className="max-w-[90%] bg-blue-600 text-white text-xs rounded-2xl rounded-tr-sm px-3 py-2">
                {msg.content}
              </div>
            ) : (
              <div className="max-w-[95%] bg-white border border-slate-200 rounded-2xl rounded-tl-sm px-3 py-2 shadow-sm">
                <p className="text-xs text-slate-700 mb-1">{msg.content}</p>
                {msg.result && msg.result.changes.length > 0 && (
                  <div className="mt-1.5 mb-2">
                    <p className="text-xs font-semibold text-slate-500 mb-1">Changes:</p>
                    <ul className="list-disc list-inside space-y-0.5">
                      {msg.result.changes.map((change, ci) => (
                        <li key={ci} className="text-xs text-slate-600">{change}</li>
                      ))}
                    </ul>
                  </div>
                )}
                {msg.provider && (
                  <div className="mt-1.5 flex items-center gap-1.5 text-[10px] text-slate-400 flex-wrap">
                    <span className="bg-slate-100 px-1.5 py-0.5 rounded font-mono">{msg.provider}</span>
                    <span className="bg-slate-100 px-1.5 py-0.5 rounded font-mono">{msg.model}</span>
                    {msg.totalTokens ? <span>{msg.totalTokens.toLocaleString()} tokens</span> : null}
                  </div>
                )}
                {msg.result && (
                  <div className="flex gap-1.5 mt-2">
                    <button
                      className="flex-1 text-xs px-2 py-1 rounded border border-slate-300 text-slate-700 hover:bg-slate-50 transition-colors"
                      onClick={() => onPreview(msg.result!.definition)}
                    >
                      Preview on canvas
                    </button>
                    <button
                      className="flex-1 text-xs px-2 py-1 rounded bg-emerald-600 hover:bg-emerald-700 text-white transition-colors"
                      onClick={() => onAccept(msg.result!.definition)}
                    >
                      Accept &amp; Save
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        ))}

        {loading && (
          <div className="flex justify-start">
            <div className="bg-white border border-slate-200 rounded-2xl rounded-tl-sm px-3 py-2 shadow-sm flex items-center gap-2">
              <Loader2 size={12} className="animate-spin text-purple-500" />
              <span className="text-xs text-slate-500">Generating workflow…</span>
            </div>
          </div>
        )}
        <div ref={chatEndRef} />
      </div>

      {/* Input area */}
      <div className="border-t p-3">
        <div className="relative">
          <textarea
            className="w-full text-xs rounded-lg border border-slate-200 p-2 pr-9 resize-none focus:outline-none focus:ring-2 focus:ring-purple-300 placeholder:text-slate-400"
            rows={3}
            placeholder="e.g. 'Create a workflow that reads Gmail and saves amounts to the database'"
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={loading || aiReady === false}
          />
          <button
            className="absolute bottom-2 right-2 p-1 rounded-md bg-purple-600 hover:bg-purple-700 text-white disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            onClick={handleSend}
            disabled={loading || !input.trim() || aiReady === false}
            title="Send"
          >
            <Send size={12} />
          </button>
        </div>
      </div>
    </div>
  );
}
