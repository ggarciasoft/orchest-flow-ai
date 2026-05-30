'use client';
import { useState, useEffect, useCallback } from 'react';
import { api, WorkflowExecution, ApprovalRequest, WorkflowForm } from '@/lib/api';
import { PageHeader, Badge, statusVariant, statusLabel } from '@/components/ui';
import FormRenderer from '@/app/(app)/forms/_components/FormRenderer';
import { Play, RotateCcw, CheckCircle2, Loader2, ChevronRight } from 'lucide-react';

// ── types ────────────────────────────────────────────────────────────────────

type Phase =
  | 'idle'         // nothing started
  | 'seeding'      // calling /api/playground/seed
  | 'starting'     // calling /execute
  | 'polling'      // waiting for first pause / next pause
  | 'filling'      // approval present, showing form
  | 'submitting'   // submitting form
  | 'done'         // system.end reached
  | 'error';       // something went wrong

interface StepResult { label: string; data: Record<string, unknown> }

const STEP_LABELS = ['Personal Info', 'Employment', 'Preferences'];
const POLL_MS = 2000;

// ── component ────────────────────────────────────────────────────────────────

export default function PlaygroundPage() {
  const [phase, setPhase] = useState<Phase>('idle');
  const [error, setError] = useState<string | null>(null);
  const [execution, setExecution] = useState<WorkflowExecution | null>(null);
  const [approval, setApproval] = useState<ApprovalRequest | null>(null);
  const [form, setForm] = useState<WorkflowForm | null>(null);
  const [values, setValues] = useState<Record<string, unknown>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [stepResults, setStepResults] = useState<StepResult[]>([]);
  const [currentStep, setCurrentStep] = useState(0); // 0-indexed

  // ── polling ─────────────────────────────────────────────────────────────────

  const poll = useCallback(async (execId: string) => {
    try {
      const exec = await api.executions.get(execId);
      setExecution(exec);

      if (exec.status === 'Completed') {
        setPhase('done');
        return;
      }

      if (exec.status === 'Failed' || exec.status === 'Cancelled') {
        setError(`Execution ${exec.status.toLowerCase()}: ${exec.errorMessage ?? 'unknown error'}`);
        setPhase('error');
        return;
      }

      if (exec.status === 'Paused') {
        // Fetch the pending approval
        try {
          const app = await api.approvals.getByExecution(execId);
          setApproval(app);

          // Determine step from approval count
          const stepIndex = stepResults.length;
          setCurrentStep(stepIndex);

          // Load the form schema
          const payload = JSON.parse(app.payloadJson ?? '{}');
          const formId = payload._formId as string;
          const nodeExecId = app.nodeExecutionId;
          if (formId && nodeExecId) {
            const fillSchema = await api.forms.getFillSchema(formId, execId, nodeExecId);
            setForm(fillSchema);
            setValues({});
            setFieldErrors({});
            setPhase('filling');
          }
        } catch {
          // Approval not ready yet, keep polling
          setTimeout(() => poll(execId), POLL_MS);
        }
        return;
      }

      // Still Running/Queued — keep polling
      setTimeout(() => poll(execId), POLL_MS);
    } catch (e) {
      setError((e as Error).message);
      setPhase('error');
    }
  }, [stepResults.length]);

  // ── actions ─────────────────────────────────────────────────────────────────

  const handleStart = async () => {
    setPhase('seeding');
    setError(null);
    setStepResults([]);
    setCurrentStep(0);
    setExecution(null);
    setApproval(null);
    setForm(null);
    try {
      const { workflowId } = await api.playground.seed();
      setPhase('starting');
      const exec = await api.workflows.execute(workflowId, {});
      setExecution(exec);
      setPhase('polling');
      setTimeout(() => poll(exec.id), POLL_MS);
    } catch (e) {
      setError((e as Error).message);
      setPhase('error');
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!approval || !form || !execution) return;

    // Client-side required validation
    const missing = form.fields.filter(f => f.required && !values[f.key] && values[f.key] !== false);
    if (missing.length > 0) {
      const errs: Record<string, string> = {};
      missing.forEach(f => { errs[f.key] = 'Required'; });
      setFieldErrors(errs);
      return;
    }

    setPhase('submitting');
    try {
      // Save this step's results
      setStepResults(prev => [
        ...prev,
        { label: STEP_LABELS[currentStep] ?? `Step ${currentStep + 1}`, data: { ...values } },
      ]);

      const payload = JSON.parse(approval.payloadJson ?? '{}');
      const formId = payload._formId as string;
      await api.forms.submit(formId, {
        workflowExecutionId: execution.id,
        nodeExecutionId: approval.nodeExecutionId,
        values,
      });

      setApproval(null);
      setForm(null);
      setPhase('polling');
      setTimeout(() => poll(execution.id), POLL_MS);
    } catch (e) {
      setError((e as Error).message);
      setPhase('error');
    }
  };

  const handleReset = () => {
    setPhase('idle');
    setError(null);
    setExecution(null);
    setApproval(null);
    setForm(null);
    setValues({});
    setFieldErrors({});
    setStepResults([]);
    setCurrentStep(0);
  };

  // ── render ───────────────────────────────────────────────────────────────────

  const isRunning = ['seeding', 'starting', 'polling', 'submitting'].includes(phase);

  return (
    <div className="max-w-2xl space-y-6">
      <PageHeader
        title="Workflow Playground"
        subtitle="Run the sample User Onboarding workflow step by step to see how form nodes, approvals, and execution flow work."
      />

      {/* Step indicator */}
      <div className="flex items-center gap-1">
        {STEP_LABELS.map((label, idx) => {
          const done = idx < stepResults.length;
          const active = phase === 'filling' && idx === currentStep;
          return (
            <div key={label} className="flex items-center gap-1">
              <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-medium border transition-colors ${
                done    ? 'bg-emerald-50 border-emerald-300 text-emerald-700' :
                active  ? 'bg-indigo-50 border-indigo-300 text-indigo-700' :
                          'bg-slate-50 border-slate-200 text-slate-400'
              }`}>
                {done ? <CheckCircle2 size={12} /> : <span className="w-4 h-4 rounded-full border flex items-center justify-center text-[10px]">{idx + 1}</span>}
                {label}
              </div>
              {idx < STEP_LABELS.length - 1 && (
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

      {/* Main area */}
      {phase === 'idle' && (
        <div className="bg-white border border-slate-200 rounded-xl p-8 text-center space-y-4">
          <div className="text-4xl">🧪</div>
          <h2 className="text-lg font-semibold text-slate-800">Ready to run</h2>
          <p className="text-sm text-slate-500 max-w-sm mx-auto">
            Click Start to seed the sample workflow and walk through three form steps — Personal Info, Employment, and Preferences.
          </p>
          <button
            onClick={handleStart}
            className="inline-flex items-center gap-2 px-6 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg transition-colors"
          >
            <Play size={14} /> Start Playground
          </button>
        </div>
      )}

      {(phase === 'seeding' || phase === 'starting') && (
        <div className="bg-white border border-slate-200 rounded-xl p-8 text-center space-y-3">
          <Loader2 size={32} className="animate-spin text-indigo-500 mx-auto" />
          <p className="text-sm text-slate-500">
            {phase === 'seeding' ? 'Setting up playground workflow…' : 'Starting execution…'}
          </p>
        </div>
      )}

      {phase === 'polling' && (
        <div className="bg-white border border-slate-200 rounded-xl p-8 text-center space-y-3">
          <Loader2 size={32} className="animate-spin text-indigo-400 mx-auto" />
          <p className="text-sm text-slate-500">Waiting for next step…</p>
        </div>
      )}

      {phase === 'filling' && form && (
        <div className="bg-white border border-slate-200 rounded-xl p-6">
          <form onSubmit={handleSubmit} className="space-y-5">
            <FormRenderer
              name={form.name}
              description={form.description}
              fields={form.fields}
              values={values}
              onChange={(key, val) => {
                setValues(prev => ({ ...prev, [key]: val }));
                setFieldErrors(prev => { const n = { ...prev }; delete n[key]; return n; });
              }}
              fieldErrors={fieldErrors}
            />
            <div className="flex gap-3 pt-2">
              <button
                type="submit"
                disabled={phase === 'submitting'}
                className="flex items-center gap-2 px-5 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg disabled:opacity-50 transition-colors"
              >
                {phase === 'submitting' ? <Loader2 size={14} className="animate-spin" /> : <ChevronRight size={14} />}
                {phase === 'submitting' ? 'Submitting…' : `Next${currentStep < STEP_LABELS.length - 1 ? '' : ' — Finish'}`}
              </button>
            </div>
          </form>
        </div>
      )}

      {phase === 'done' && (
        <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-5">
          <div className="text-center space-y-2">
            <CheckCircle2 size={40} className="text-emerald-500 mx-auto" />
            <h2 className="text-lg font-semibold text-slate-800">All steps complete!</h2>
            <p className="text-sm text-slate-500">The workflow reached the end node. Here's what was collected:</p>
          </div>

          <div className="space-y-4">
            {stepResults.map((step, idx) => (
              <div key={idx} className="border border-slate-200 rounded-xl overflow-hidden">
                <div className="px-4 py-2 bg-slate-50 border-b border-slate-200">
                  <span className="text-xs font-semibold text-slate-600">{step.label}</span>
                </div>
                <div className="p-4 space-y-1.5">
                  {Object.entries(step.data).map(([k, v]) => (
                    <div key={k} className="flex gap-3 text-sm">
                      <span className="text-slate-400 font-mono w-32 shrink-0">{k}</span>
                      <span className="text-slate-700 break-all">
                        {v === true ? '✓' : v === false ? '✗' : String(v ?? '—')}
                      </span>
                    </div>
                  ))}
                </div>
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
    </div>
  );
}
