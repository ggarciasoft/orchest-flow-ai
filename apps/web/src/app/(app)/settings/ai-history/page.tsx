'use client';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, AiChatSessionSummary, AiChatMessage } from '@/lib/api';
import { MessageSquare, ChevronDown, ChevronUp, Activity, Sparkles, Wrench, Filter } from 'lucide-react';
import { PageHeader } from '@/components/ui';

const surfaceLabelMap: Record<string, string> = {
  'workflow-assist': 'Workflow Designer',
  'form-generator': 'Form Generator',
};

function getSurfaceLabel(surface: string): string {
  return surfaceLabelMap[surface] ?? surface.charAt(0).toUpperCase() + surface.slice(1);
}

function SessionRow({ session }: { session: AiChatSessionSummary }) {
  const [expanded, setExpanded] = useState(false);
  const { data: messages, isLoading: loadingMessages } = useQuery({
    queryKey: ['ai-history', 'messages', session.id],
    queryFn: () => api.aiHistory.getMessages(session.id),
    enabled: expanded,
  });

  const messageCount = messages?.length ?? '?';

  return (
    <div className="border-b border-slate-100 last:border-b-0">
      <div className="flex items-center gap-4 px-5 py-3 hover:bg-slate-50 transition-colors">
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-slate-800">{getSurfaceLabel(session.surface)}</p>
          <p className="text-xs text-slate-400 mt-0.5">
            {new Date(session.createdAt).toLocaleString()}
          </p>
        </div>

        <div className="text-sm text-slate-500">{messageCount} messages</div>

        <button
          onClick={() => setExpanded(!expanded)}
          className="text-xs bg-indigo-600 text-white px-3 py-1.5 rounded hover:bg-indigo-700 flex items-center gap-1"
        >
          {expanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />}
          {expanded ? 'Hide' : 'View'}
        </button>
      </div>

      {expanded && (
        <div className="px-5 pb-4 bg-slate-50 border-t border-slate-100">
          {loadingMessages && (
            <div className="py-4 text-center text-sm text-slate-400">Loading messages...</div>
          )}
          {messages && messages.length === 0 && (
            <div className="py-4 text-center text-sm text-slate-400">No messages in this session.</div>
          )}
          {messages && messages.length > 0 && (
            <div className="space-y-3 mt-3">
              {messages.map((msg: AiChatMessage) => (
                <MessageBubble key={msg.id} message={msg} />
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function MessageBubble({ message }: { message: AiChatMessage }) {
  const [showToolDetails, setShowToolDetails] = useState(false);

  if (message.role === 'user') {
    return (
      <div className="flex justify-end">
        <div className="max-w-2xl bg-slate-700 text-white text-sm rounded-xl px-4 py-2.5 shadow">
          {message.contentText || '(no content)'}
        </div>
      </div>
    );
  }

  if (message.role === 'assistant') {
    return (
      <div className="flex justify-start">
        <div className="max-w-2xl bg-indigo-50 border border-indigo-100 text-slate-800 text-sm rounded-xl px-4 py-2.5 shadow-sm">
          <div className="whitespace-pre-wrap">{message.contentText || '(no content)'}</div>
          {(message.model || message.totalTokens > 0) && (
            <div className="mt-2 pt-2 border-t border-indigo-100 flex items-center gap-3 text-xs text-indigo-600">
              {message.model && (
                <span className="flex items-center gap-1">
                  <Sparkles size={12} />
                  {message.model}
                </span>
              )}
              {message.totalTokens > 0 && (
                <span className="flex items-center gap-1">
                  <Activity size={12} />
                  {message.totalTokens.toLocaleString()} tokens
                </span>
              )}
            </div>
          )}
        </div>
      </div>
    );
  }

  if (message.role === 'tool') {
    return (
      <div className="flex justify-start">
        <div className="max-w-2xl bg-amber-50 border border-amber-200 text-slate-800 text-sm rounded-xl px-4 py-2.5 shadow-sm">
          <div className="flex items-center gap-2 font-medium text-amber-900 mb-1">
            <Wrench size={14} />
            Tool: {message.toolName || 'unknown'}
          </div>
          {message.toolInputJson && (
            <div className="mt-2">
              <button
                onClick={() => setShowToolDetails(!showToolDetails)}
                className="text-xs text-amber-700 hover:text-amber-900 underline"
              >
                {showToolDetails ? 'Hide' : 'Show'} input/output
              </button>
              {showToolDetails && (
                <div className="mt-2 space-y-2">
                  {message.toolInputJson && (
                    <details className="text-xs">
                      <summary className="cursor-pointer text-amber-800 font-medium">Input</summary>
                      <pre className="mt-1 bg-amber-100 p-2 rounded overflow-x-auto text-xs">
                        {JSON.stringify(JSON.parse(message.toolInputJson), null, 2)}
                      </pre>
                    </details>
                  )}
                  {message.toolOutputJson && (
                    <details className="text-xs">
                      <summary className="cursor-pointer text-amber-800 font-medium">Output</summary>
                      <pre className="mt-1 bg-amber-100 p-2 rounded overflow-x-auto text-xs">
                        {JSON.stringify(JSON.parse(message.toolOutputJson), null, 2)}
                      </pre>
                    </details>
                  )}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    );
  }

  return null;
}

export default function AiHistoryPage() {
  const [surfaceFilter, setSurfaceFilter] = useState<string>('');

  const { data: summary, isLoading: loadingSummary } = useQuery({
    queryKey: ['ai-history', 'summary'],
    queryFn: () => api.aiHistory.usageSummary(),
  });

  const { data: sessions, isLoading: loadingSessions } = useQuery({
    queryKey: ['ai-history', 'sessions', surfaceFilter],
    queryFn: () => api.aiHistory.listSessions({ surface: surfaceFilter || undefined }),
  });

  const isLoading = loadingSummary || loadingSessions;

  return (
    <div className="space-y-6">
      <PageHeader
        title="AI History"
        subtitle="View AI assistant session history and token usage"
      />

      {/* Usage summary cards */}
      {isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {[1, 2, 3].map(i => (
            <div key={i} className="bg-white border border-slate-200 rounded-xl p-5 animate-pulse">
              <div className="h-4 bg-slate-200 rounded w-24 mb-2"></div>
              <div className="h-8 bg-slate-200 rounded w-16"></div>
            </div>
          ))}
        </div>
      )}

      {summary && !isLoading && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-white border border-slate-200 rounded-xl p-5">
            <div className="flex items-center gap-2 text-slate-600 text-sm mb-1">
              <MessageSquare size={16} />
              Total Sessions
            </div>
            <div className="text-3xl font-bold text-slate-900">{summary.totalSessions.toLocaleString()}</div>
          </div>

          <div className="bg-white border border-slate-200 rounded-xl p-5">
            <div className="flex items-center gap-2 text-slate-600 text-sm mb-1">
              <Activity size={16} />
              Total Tokens
            </div>
            <div className="text-3xl font-bold text-slate-900">{summary.totalTokens.toLocaleString()}</div>
          </div>

          <div className="bg-white border border-slate-200 rounded-xl p-5">
            <div className="flex items-center gap-2 text-slate-600 text-sm mb-1">
              <Sparkles size={16} />
              By Surface
            </div>
            <div className="mt-2 space-y-1">
              {summary.bySurface.map(({ surface, sessionCount }) => (
                <div key={surface} className="flex justify-between text-sm">
                  <span className="text-slate-600">{getSurfaceLabel(surface)}</span>
                  <span className="font-medium text-slate-900">{sessionCount}</span>
                </div>
              ))}
              {summary.bySurface.length === 0 && (
                <div className="text-sm text-slate-400">No sessions yet</div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Filter bar */}
      <div className="bg-white border border-slate-200 rounded-xl p-4">
        <div className="flex items-center gap-3">
          <Filter size={16} className="text-slate-400" />
          <label className="text-sm font-medium text-slate-700">Filter by surface:</label>
          <select
            value={surfaceFilter}
            onChange={(e) => setSurfaceFilter(e.target.value)}
            className="border border-slate-200 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option value="">All</option>
            <option value="workflow-assist">Workflow Designer</option>
            <option value="form-generator">Form Generator</option>
          </select>
        </div>
      </div>

      {/* Session list */}
      <div className="bg-white border border-slate-200 rounded-xl">
        <div className="px-5 py-4 border-b border-slate-100">
          <h3 className="text-sm font-semibold text-slate-900">
            Sessions {sessions && <span className="text-slate-400 font-normal ml-1">({sessions.length})</span>}
          </h3>
        </div>

        {isLoading && (
          <div className="p-8 text-center text-sm text-slate-400">Loading sessions...</div>
        )}

        {!isLoading && (!sessions || sessions.length === 0) && (
          <div className="p-8 text-center">
            <MessageSquare size={32} className="text-slate-200 mx-auto mb-2" />
            <p className="text-sm text-slate-400">
              No AI sessions yet. Use the workflow designer AI assistant or form generator to create your first session.
            </p>
          </div>
        )}

        {sessions && sessions.length > 0 && (
          <div>
            {sessions.map((session: AiChatSessionSummary) => (
              <SessionRow key={session.id} session={session} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
