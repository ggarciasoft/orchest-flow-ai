'use client';
import { useState, useEffect, useCallback, useRef } from 'react';
import { api, WorkflowExecution } from '@/lib/api';
import { PageHeader, Badge, statusVariant, statusLabel } from '@/components/ui';
import {
  Play, RotateCcw, Copy, Check, Loader2, ChevronRight,
  Terminal, Send, Database, CheckCircle2,
} from 'lucide-react';

// ── constants ────────────────────────────────────────────────────────────────

const POLL_MS = 2000;
const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5080';

const CHECKPOINT_LABELS = ['Customer', 'Order'] as const;

// ── types ────────────────────────────────────────────────────────────────────

type Phase =
  | 'setup'     // initial config screen
  | 'idle'
  | 'seeding'
  | 'starting'
  | 'polling'
  | 'waiting'    // paused, showing resume URL
  | 'sending'    // user is posting data
  | 'done'
  | 'error';

interface DbConfig {
  customer: { connectionString: string; statement: string };
  order: { connectionString: string; statement: string };
}

interface CheckpointState {
  label: string;
  token: string;
  resumeUrl: string;
  receivedData?: Record<string, unknown>;
}

interface LogEntry {
  ts: string;
  kind: 'info' | 'success' | 'error';
  message: string;
}

// ── helpers ──────────────────────────────────────────────────────────────────

function now(): string {
  return new Date().toLocaleTimeString('en-US', { hour12: false });
}

function defaultPayload(label: string): string {
  if (label === 'Customer') {
    return JSON.stringify({ name: 'Jane Smith', email: 'jane@example.com' }, null, 2);
  }
  return JSON.stringify({ items: ['Widget A', 'Widget B'], amount: 99.95 }, null, 2);
}

// ── small CopyButton component ────────────────────────────────────────────────

function CopyButton({ text, label }: { text: string; label?: string }) {
  const [copied, setCopied] = useState(false);
  const copy = () => {
    void navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  return (
    <button
      onClick={copy}
      className="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs rounded-md border border-slate-200 hover:bg-slate-50 text-slate-600 transition-colors"
    >
      {copied ? <Check size={12} className="text-emerald-500" /> : <Copy size={12} />}
      {label ?? (copied ? 'Copied!' : 'Copy')}
    </button>
  );
}

// ── SetupForm component ───────────────────────────────────────────────────────

interface SetupFormProps {
  onSave: (config: DbConfig) => void;
  onSkip: () => void;
}

function SetupForm({ onSave, onSkip }: SetupFormProps) {
  const [customerConnStr, setCustomerConnStr]         = useState('');
  const [customerStatement, setCustomerStatement]     = useState('INSERT INTO customers (name, email) VALUES (@name, @email)');
  const [orderConnStr, setOrderConnStr]               = useState('');
  const [orderStatement, setOrderStatement]           = useState('INSERT INTO orders (items, amount) VALUES (@items, @amount)');
  const [validationError, setValidationError] = useState<string | null>(null);

  const customerTouchedRef = useRef(false);
  const orderTouchedRef    = useRef(false);

  const handleSave = () => {
    const customerTouched = customerTouchedRef.current;
    const orderTouched    = orderTouchedRef.current;

    if ((customerTouched || orderTouched) && (!customerConnStr || !orderConnStr)) {
      setValidationError('If you configure one database, both connection strings are required.');
      return;
    }

    setValidationError(null);
    onSave({
      customer: { connectionString: customerConnStr, statement: customerStatement },
      order:    { connectionString: orderConnStr,    statement: orderStatement },
    });
  };

  const customerCreateTable = `CREATE TABLE IF NOT EXISTS customers (
  id SERIAL PRIMARY KEY,
  name TEXT NOT NULL,
  email TEXT NOT NULL,
  created_at TIMESTAMPTZ DEFAULT NOW()
);`;

  const orderCreateTable = `CREATE TABLE IF NOT EXISTS orders (
  id SERIAL PRIMARY KEY,
  items TEXT NOT NULL,
  amount NUMERIC(10,2) NOT NULL,
  created_at TIMESTAMPTZ DEFAULT NOW()
);`;

  return (
    <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
      <div className="px-6 py-4 border-b border-slate-200">
        <h2 className="text-lg font-semibold text-slate-800">Database Setup</h2>
      </div>

      <div className="p-6 space-y-8">
        {/* Customer DB */}
        <div className="space-y-4">
          <div>
            <h3 className="text-sm font-semibold text-slate-700">Customer DB</h3>
            <p className="text-xs text-slate-500 mt-1">Runs after the Customer checkpoint</p>
          </div>

          <div className="space-y-2">
            <label className="block text-xs font-medium text-slate-600">Connection String</label>
            <input
              type="text"
              placeholder="Host=localhost;Database=mydb;Username=...;Password=..."
              value={customerConnStr}
              onChange={e => { setCustomerConnStr(e.target.value); customerTouchedRef.current = true; setValidationError(null); }}
              className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
          </div>

          <div className="space-y-2">
            <label className="block text-xs font-medium text-slate-600">SQL Statement</label>
            <textarea
              value={customerStatement}
              onChange={e => setCustomerStatement(e.target.value)}
              className="w-full h-20 px-3 py-2 text-sm font-mono border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-300 resize-y"
            />
          </div>

          <div className="border border-slate-200 rounded-lg overflow-hidden">
            <div className="px-3 py-2 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
              <span className="text-xs font-medium text-slate-600">CREATE TABLE snippet</span>
              <CopyButton text={customerCreateTable} />
            </div>
            <pre className="p-3 text-xs font-mono text-slate-700 overflow-x-auto">{customerCreateTable}</pre>
          </div>
        </div>

        {/* Order DB */}
        <div className="space-y-4">
          <div>
            <h3 className="text-sm font-semibold text-slate-700">Order DB</h3>
            <p className="text-xs text-slate-500 mt-1">Runs after the Order checkpoint</p>
          </div>

          <div className="space-y-2">
            <label className="block text-xs font-medium text-slate-600">Connection String</label>
            <input
              type="text"
              placeholder="Host=localhost;Database=mydb;Username=...;Password=..."
              value={orderConnStr}
              onChange={e => { setOrderConnStr(e.target.value); orderTouchedRef.current = true; setValidationError(null); }}
              className="w-full px-3 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-300"
            />
          </div>

          <div className="space-y-2">
            <label className="block text-xs font-medium text-slate-600">SQL Statement</label>
            <textarea
              value={orderStatement}
              onChange={e => setOrderStatement(e.target.value)}
              className="w-full h-20 px-3 py-2 text-sm font-mono border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-300 resize-y"
            />
          </div>

          <div className="border border-slate-200 rounded-lg overflow-hidden">
            <div className="px-3 py-2 bg-slate-50 border-b border-slate-200 flex items-center justify-between">
              <span className="text-xs font-medium text-slate-600">CREATE TABLE snippet</span>
              <CopyButton text={orderCreateTable} />
            </div>
            <pre className="p-3 text-xs font-mono text-slate-700 overflow-x-auto">{orderCreateTable}</pre>
          </div>
        </div>

        {validationError && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-700">
            {validationError}
          </div>
        )}
      </div>

      <div className="px-6 py-4 bg-slate-50 border-t border-slate-200 flex items-center gap-3">
        <button
          onClick={onSkip}
          className="px-4 py-2 text-sm font-medium text-slate-600 border border-slate-200 rounded-lg hover:bg-white transition-colors"
        >
          Skip DB setup
        </button>
        <button
          onClick={handleSave}
          className="px-4 py-2 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg transition-colors"
        >
          Save &amp; Continue
        </button>
      </div>
    </div>
  );
}

// ── main component ────────────────────────────────────────────────────────────

export default function ExternalPlaygroundPage() {
  const [phase, setPhase]               = useState<Phase>('setup');
  const [error, setError]               = useState<string | null>(null);
  const [execution, setExecution]       = useState<WorkflowExecution | null>(null);
  const [checkpoints, setCheckpoints]   = useState<CheckpointState[]>([]);
  const [currentCp, setCurrentCp]       = useState(0);
  const [log, setLog]                   = useState<LogEntry[]>([]);
  const [jsonPayload, setJsonPayload]   = useState('');
  const [jsonError, setJsonError]       = useState<string | null>(null);
  const [dbConfig, setDbConfig]         = useState<DbConfig | null>(null);
  const logEndRef = useRef<HTMLDivElement>(null);

  // auto-scroll log
  useEffect(() => { logEndRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [log]);

  const addLog = useCallback((kind: LogEntry['kind'], message: string) => {
    setLog(prev => [...prev, { ts: now(), kind, message }]);
  }, []);

  // ── polling ─────────────────────────────────────────────────────────────────

  const poll = useCallback(async (execId: string, cpIndex: number) => {
    try {
      const exec = await api.executions.get(execId);
      setExecution(exec);

      if (exec.status === 'Completed') {
        addLog('success', 'Workflow reached the end node — all done!');
        setPhase('done');
        return;
      }

      if (exec.status === 'Failed' || exec.status === 'Cancelled') {
        const msg = `Execution ${exec.status.toLowerCase()}: ${exec.errorMessage ?? 'unknown error'}`;
        addLog('error', msg);
        setError(msg);
        setPhase('error');
        return;
      }

      if (exec.status === 'Paused') {
        // Fetch the pending node outputs to get the resume token
        const timeline = await api.executions.timeline(execId);
        const pausedNode = timeline.nodes.find(n =>
          n.nodeType === 'system.data-checkpoint' &&
          n.status === 'WaitingForApproval'
        );
        if (pausedNode?.outputJson) {
          const outputs = JSON.parse(pausedNode.outputJson) as Record<string, unknown>;
          const token      = outputs['_correlationToken'] as string;
          const resumeUrl  = outputs['_resumeUrl'] as string;
          const label      = CHECKPOINT_LABELS[cpIndex] ?? `Checkpoint ${cpIndex + 1}`;
          setCheckpoints(prev => {
            const next = [...prev];
            next[cpIndex] = { label, token, resumeUrl };
            return next;
          });
          setCurrentCp(cpIndex);
          setJsonPayload(defaultPayload(label));
          setJsonError(null);
          addLog('info', `Paused at ${label} checkpoint — waiting for external data POST`);
          setPhase('waiting');
          return;
        }
        // Token not in outputs yet — keep polling
        setTimeout(() => poll(execId, cpIndex), POLL_MS);
        return;
      }

      // Still Running/Queued — keep polling
      setTimeout(() => poll(execId, cpIndex), POLL_MS);
    } catch (e) {
      const msg = (e as Error).message;
      addLog('error', msg);
      setError(msg);
      setPhase('error');
    }
  }, [addLog]);

  // ── actions ─────────────────────────────────────────────────────────────────

  const handleStart = async () => {
    setPhase('seeding');
    setError(null);
    setCheckpoints([]);
    setCurrentCp(0);
    setLog([]);
    setExecution(null);
    addLog('info', 'Seeding External Data Intake workflow…');

    try {
      const { workflowId } = await api.playground.seedExternal(dbConfig);
      addLog('info', `Workflow seeded (id: ${workflowId.slice(0, 8)}…). Starting execution…`);
      setPhase('starting');
      const exec = await api.workflows.execute(workflowId, {});
      setExecution(exec);
      addLog('info', `Execution started (id: ${exec.id.slice(0, 8)}…). Polling for first checkpoint…`);
      setPhase('polling');
      setTimeout(() => poll(exec.id, 0), POLL_MS);
    } catch (e) {
      const msg = (e as Error).message;
      addLog('error', msg);
      setError(msg);
      setPhase('error');
    }
  };

  const handleSend = async () => {
    if (!execution) return;
    const cp = checkpoints[currentCp];
    if (!cp) return;

    let data: Record<string, unknown>;
    try {
      data = JSON.parse(jsonPayload) as Record<string, unknown>;
      if (typeof data !== 'object' || Array.isArray(data)) throw new Error('Must be a JSON object');
      setJsonError(null);
    } catch (e) {
      setJsonError(`Invalid JSON: ${(e as Error).message}`);
      return;
    }

    setPhase('sending');
    addLog('info', `POSTing to ${cp.resumeUrl}…`);

    try {
      await api.webhooks.resume(cp.token, data);
      // Store what we sent
      setCheckpoints(prev => {
        const next = [...prev];
        next[currentCp] = { ...next[currentCp], receivedData: data };
        return next;
      });
      addLog('success', `Data received at ${cp.label} checkpoint — resuming workflow…`);
      setPhase('polling');
      setTimeout(() => poll(execution.id, currentCp + 1), POLL_MS);
    } catch (e) {
      const msg = (e as Error).message;
      addLog('error', msg);
      setError(msg);
      setPhase('error');
    }
  };

  const handleReset = () => {
    setPhase('setup');
    setError(null);
    setExecution(null);
    setCheckpoints([]);
    setCurrentCp(0);
    setLog([]);
    setJsonPayload('');
    setJsonError(null);
  };

  // ── derived ──────────────────────────────────────────────────────────────────

  const cp = checkpoints[currentCp];
  const fullResumeUrl = cp ? `${API_BASE}${cp.resumeUrl}` : '';
  const curlCmd = cp
    ? `curl -X POST "${fullResumeUrl}" \\\n  -H "Content-Type: application/json" \\\n  -d '${JSON.stringify(JSON.parse(jsonPayload || '{}'), null, 0)}'`
    : '';

  // ── render ───────────────────────────────────────────────────────────────────

  return (
    <div className="max-w-3xl space-y-6">
      <PageHeader
        title="External Data Intake"
        subtitle="Walk through a workflow that pauses and waits for external systems to POST data — no forms, purely API-driven."
      />

      {/* Checkpoint progress */}
      <div className="flex items-center gap-1">
        {CHECKPOINT_LABELS.map((label, idx) => {
          const done   = idx < checkpoints.length && checkpoints[idx]?.receivedData !== undefined;
          const active = phase === 'waiting' && idx === currentCp;
          return (
            <div key={label} className="flex items-center gap-1">
              <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium border transition-colors ${
                done    ? 'bg-emerald-50 border-emerald-300 text-emerald-700' :
                active  ? 'bg-indigo-50 border-indigo-300 text-indigo-700' :
                          'bg-slate-50 border-slate-200 text-slate-400'
              }`}>
                {done
                  ? <CheckCircle2 size={12} />
                  : <Database size={12} />
                }
                {label}
              </div>
              {idx < CHECKPOINT_LABELS.length - 1 && (
                <ChevronRight size={12} className="text-slate-300 shrink-0" />
              )}
            </div>
          );
        })}
      </div>

      {/* Status bar */}
      {execution && (
        <div className="flex items-center gap-3 bg-white border border-slate-200 rounded-xl px-4 py-3">
          <Badge variant={statusVariant(execution.status)}>{statusLabel(execution.status)}</Badge>
          <span className="text-xs text-slate-400 font-mono">{execution.id.slice(0, 20)}…</span>
        </div>
      )}

      {/* Error */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-xl p-4 text-red-700 text-sm">{error}</div>
      )}

      {/* Setup */}
      {phase === 'setup' && (
        <div className="space-y-6">
          <div className="bg-indigo-50 border border-indigo-200 rounded-xl p-4">
            <p className="text-sm text-indigo-700">
              The workflow includes two database nodes that execute SQL after each checkpoint. Configure them below, or skip to run the workflow without DB writes.
            </p>
          </div>

          <SetupForm
            onSave={(config) => {
              setDbConfig(config);
              setPhase('idle');
            }}
            onSkip={() => {
              setDbConfig(null);
              setPhase('idle');
            }}
          />
        </div>
      )}

      {/* Idle */}
      {phase === 'idle' && (
        <div className="bg-white border border-slate-200 rounded-xl p-8 text-center space-y-4">
          <div className="text-4xl">📡</div>
          <h2 className="text-lg font-semibold text-slate-800">Ready to Start</h2>
          <p className="text-sm text-slate-500 max-w-sm mx-auto">
            {dbConfig
              ? 'Database configuration saved. Click below to start the workflow.'
              : 'Database setup skipped. The workflow will run without DB writes.'}
          </p>
          <button
            onClick={handleStart}
            className="inline-flex items-center gap-2 px-6 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg transition-colors"
          >
            <Play size={14} /> Start Playground
          </button>
        </div>
      )}

      {/* Seeding / starting / polling spinner */}
      {(phase === 'seeding' || phase === 'starting' || phase === 'polling') && (
        <div className="bg-white border border-slate-200 rounded-xl p-8 text-center space-y-3">
          <Loader2 size={32} className="animate-spin text-indigo-400 mx-auto" />
          <p className="text-sm text-slate-500">
            {phase === 'seeding'  ? 'Setting up workflow…'   :
             phase === 'starting' ? 'Starting execution…'    :
                                    'Waiting for checkpoint…'}
          </p>
        </div>
      )}

      {/* Waiting — show resume URL + simulate panel */}
      {(phase === 'waiting' || phase === 'sending') && cp && (
        <div className="space-y-4">
          {/* Resume URL card */}
          <div className="bg-white border border-indigo-200 rounded-xl overflow-hidden">
            <div className="px-4 py-3 bg-indigo-50 border-b border-indigo-200 flex items-center justify-between">
              <span className="text-sm font-semibold text-indigo-700">
                ⏸ Paused at <strong>{cp.label}</strong> checkpoint
              </span>
            </div>
            <div className="p-4 space-y-3">
              <p className="text-xs text-slate-500">
                The workflow is waiting for an external system to POST JSON data to this URL:
              </p>
              <div className="flex items-center gap-2 bg-slate-50 border border-slate-200 rounded-lg px-3 py-2">
                <code className="text-xs font-mono text-slate-700 flex-1 break-all">{fullResumeUrl}</code>
                <CopyButton text={fullResumeUrl} />
              </div>
            </div>
          </div>

          {/* Simulate panel */}
          <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            <div className="px-4 py-3 bg-slate-50 border-b border-slate-200 flex items-center gap-2">
              <Send size={14} className="text-slate-500" />
              <span className="text-sm font-semibold text-slate-700">Simulate External System</span>
            </div>
            <div className="p-4 space-y-3">
              <p className="text-xs text-slate-500">
                Edit the JSON body below and click Send — or copy the curl command to test from a real terminal.
              </p>

              <textarea
                className={`w-full h-32 font-mono text-xs bg-slate-50 border rounded-lg p-3 resize-y focus:outline-none focus:ring-2 focus:ring-indigo-300 ${
                  jsonError ? 'border-red-300' : 'border-slate-200'
                }`}
                value={jsonPayload}
                onChange={e => { setJsonPayload(e.target.value); setJsonError(null); }}
                spellCheck={false}
              />
              {jsonError && <p className="text-xs text-red-600">{jsonError}</p>}

              <div className="flex items-center gap-3">
                <button
                  onClick={handleSend}
                  disabled={phase === 'sending'}
                  className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg disabled:opacity-50 transition-colors"
                >
                  {phase === 'sending'
                    ? <Loader2 size={14} className="animate-spin" />
                    : <Send size={14} />}
                  {phase === 'sending' ? 'Sending…' : 'Send Data'}
                </button>
                <CopyButton text={curlCmd} label="Copy curl" />
              </div>
            </div>

            {/* curl preview */}
            <div className="border-t border-slate-100 px-4 py-3">
              <div className="flex items-center gap-2 mb-2">
                <Terminal size={12} className="text-slate-400" />
                <span className="text-xs text-slate-400 font-medium">curl equivalent</span>
              </div>
              <pre className="text-xs font-mono text-slate-600 bg-slate-50 border border-slate-200 rounded-lg p-3 overflow-x-auto whitespace-pre-wrap break-all">
                {curlCmd}
              </pre>
            </div>
          </div>
        </div>
      )}

      {/* Sending spinner */}
      {phase === 'sending' && (
        <div className="flex items-center gap-2 text-sm text-slate-500">
          <Loader2 size={14} className="animate-spin" /> Posting data and resuming workflow…
        </div>
      )}

      {/* Done */}
      {phase === 'done' && (
        <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-5">
          <div className="text-center space-y-2">
            <CheckCircle2 size={40} className="text-emerald-500 mx-auto" />
            <h2 className="text-lg font-semibold text-slate-800">All checkpoints complete!</h2>
            <p className="text-sm text-slate-500">Here&apos;s the data collected at each checkpoint:</p>
          </div>

          <div className="space-y-4">
            {checkpoints.map((c, idx) => (
              <div key={idx} className="border border-slate-200 rounded-xl overflow-hidden">
                <div className="px-4 py-2 bg-slate-50 border-b border-slate-200 flex items-center gap-2">
                  <Database size={13} className="text-slate-400" />
                  <span className="text-xs font-semibold text-slate-600">{c.label}</span>
                </div>
                {c.receivedData && (
                  <div className="p-4 space-y-1.5">
                    {Object.entries(c.receivedData).map(([k, v]) => (
                      <div key={k} className="flex gap-3 text-sm">
                        <span className="text-slate-400 font-mono w-32 shrink-0">{k}</span>
                        <span className="text-slate-700 break-all">{String(v ?? '—')}</span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>

          <button
            onClick={handleReset}
            className="flex items-center gap-2 px-4 py-2 border border-slate-200 text-slate-600 hover:bg-slate-50 text-sm rounded-lg transition-colors"
          >
            <RotateCcw size={14} /> Run Again
          </button>
        </div>
      )}

      {phase === 'error' && (
        <button
          onClick={handleReset}
          className="flex items-center gap-2 px-4 py-2 border border-slate-200 text-slate-600 hover:bg-slate-50 text-sm rounded-lg transition-colors"
        >
          <RotateCcw size={14} /> Try Again
        </button>
      )}

      {/* Activity Log */}
      {log.length > 0 && (
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="px-4 py-3 bg-slate-50 border-b border-slate-200 flex items-center gap-2">
            <Terminal size={14} className="text-slate-500" />
            <span className="text-sm font-semibold text-slate-700">Activity Log</span>
          </div>
          <div className="p-3 max-h-48 overflow-y-auto space-y-1 font-mono text-xs">
            {log.map((entry, i) => (
              <div key={i} className={`flex gap-2 ${
                entry.kind === 'success' ? 'text-emerald-600' :
                entry.kind === 'error'   ? 'text-red-600'     :
                                           'text-slate-500'
              }`}>
                <span className="text-slate-300 shrink-0">{entry.ts}</span>
                <span>{entry.message}</span>
              </div>
            ))}
            <div ref={logEndRef} />
          </div>
        </div>
      )}
    </div>
  );
}
