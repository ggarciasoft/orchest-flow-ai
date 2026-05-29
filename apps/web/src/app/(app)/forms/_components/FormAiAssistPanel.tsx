'use client';
import { useState, useRef, useEffect } from 'react';
import { api, FormAiAssistResult, FormFieldDefinition } from '@/lib/api';
import { X, Send, Loader2, Sparkles, CheckCircle } from 'lucide-react';

interface Message {
  role: 'user' | 'assistant';
  content: string;
  result?: FormAiAssistResult;
}

interface FormAiAssistPanelProps {
  formName: string;
  formDescription?: string;
  /** Current fields JSON string so the AI can modify existing fields */
  getCurrentFieldsJson: () => string;
  onPreview: (fields: FormFieldDefinition[]) => void;
  onAccept: (fields: FormFieldDefinition[]) => void;
  onClose: () => void;
}

function parseFields(json: string): FormFieldDefinition[] {
  try { return JSON.parse(json) as FormFieldDefinition[]; } catch { return []; }
}

/**
 * FormAiAssistPanel — side panel for AI-powered form field generation.
 * Mirrors the AiAssistPanel in the workflow designer.
 * Users describe the form in natural language; the AI generates or modifies the fields.
 */
export default function FormAiAssistPanel({
  formName, formDescription, getCurrentFieldsJson, onPreview, onAccept, onClose
}: FormAiAssistPanelProps) {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const chatEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = async () => {
    const trimmed = input.trim();
    if (!trimmed || loading) return;

    setMessages(prev => [...prev, { role: 'user', content: trimmed }]);
    setInput('');
    setLoading(true);

    try {
      const result = await api.forms.aiAssist({
        prompt: trimmed,
        currentFieldsJson: getCurrentFieldsJson(),
        formName,
        formDescription,
      });
      setMessages(prev => [...prev, { role: 'assistant', content: result.explanation, result }]);
    } catch (e) {
      setMessages(prev => [...prev, { role: 'assistant', content: `Error: ${(e as Error).message}` }]);
    } finally {
      setLoading(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend(); }
  };

  return (
    <div className="w-80 border-l bg-white flex flex-col shrink-0 overflow-hidden shadow-lg">
      {/* Header */}
      <div className="flex items-start justify-between p-4 border-b">
        <div>
          <h3 className="font-semibold text-sm text-slate-900 flex items-center gap-1.5">
            <Sparkles size={14} className="text-purple-500" /> AI Form Builder
          </h3>
          <p className="text-xs text-slate-400 mt-0.5">Describe your form, I&apos;ll generate the fields</p>
        </div>
        <button onClick={onClose} className="p-1 hover:bg-gray-100 rounded mt-0.5" title="Close">
          <X size={16} />
        </button>
      </div>

      {/* Chat */}
      <div className="flex-1 overflow-y-auto p-3 flex flex-col gap-3">
        {messages.length === 0 && (
          <div className="text-center text-xs text-slate-400 mt-6 px-2 space-y-1">
            <p>Describe the form you need</p>
            <p className="italic">&quot;A user registration form with name, email, phone and department select&quot;</p>
            <p className="italic">&quot;Add a file description field and make the deadline required&quot;</p>
          </div>
        )}

        {messages.map((msg, i) => (
          <div key={i} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            {msg.role === 'user' ? (
              <div className="max-w-[90%] bg-indigo-600 text-white text-xs rounded-2xl rounded-tr-sm px-3 py-2">
                {msg.content}
              </div>
            ) : (
              <div className="max-w-[95%] bg-white border border-slate-200 rounded-2xl rounded-tl-sm px-3 py-2 shadow-sm">
                <p className="text-xs text-slate-700 mb-1">{msg.content}</p>
                {msg.result && msg.result.changes.length > 0 && (
                  <div className="mt-1.5 mb-2">
                    <p className="text-xs font-semibold text-slate-500 mb-1">Changes:</p>
                    <ul className="list-disc list-inside space-y-0.5">
                      {msg.result.changes.map((ch, ci) => (
                        <li key={ci} className="text-xs text-slate-600">{ch}</li>
                      ))}
                    </ul>
                  </div>
                )}
                {msg.result && (
                  <div className="flex gap-1.5 mt-2">
                    <button
                      className="flex-1 text-xs px-2 py-1 rounded border border-slate-300 text-slate-700 hover:bg-slate-50 transition-colors"
                      onClick={() => onPreview(parseFields(msg.result!.fieldsJson))}
                    >
                      Preview
                    </button>
                    <button
                      className="flex-1 text-xs px-2 py-1 rounded bg-emerald-600 hover:bg-emerald-700 text-white transition-colors flex items-center justify-center gap-1"
                      onClick={() => onAccept(parseFields(msg.result!.fieldsJson))}
                    >
                      <CheckCircle size={11} /> Accept
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
              <span className="text-xs text-slate-500">Generating fields…</span>
            </div>
          </div>
        )}
        <div ref={chatEndRef} />
      </div>

      {/* Input */}
      <div className="border-t p-3">
        <div className="relative">
          <textarea
            className="w-full text-xs rounded-lg border border-slate-200 p-2 pr-9 resize-none focus:outline-none focus:ring-2 focus:ring-purple-300 placeholder:text-slate-400"
            rows={3}
            placeholder="e.g. 'A purchase order form with vendor, amount, and approval date'"
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={loading}
          />
          <button
            className="absolute bottom-2 right-2 p-1 rounded-md bg-purple-600 hover:bg-purple-700 text-white disabled:opacity-40 transition-colors"
            onClick={handleSend}
            disabled={loading || !input.trim()}
            title="Send"
          >
            <Send size={12} />
          </button>
        </div>
      </div>
    </div>
  );
}
