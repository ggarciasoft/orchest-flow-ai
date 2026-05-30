'use client';
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import type { Workflow } from '@/lib/api';
import { Clock, Webhook, MousePointerClick, CheckCircle2 } from 'lucide-react';

interface Props {
  workflow: Workflow;
}

type TriggerType = 'Manual' | 'Webhook' | 'Cron';

const TRIGGER_OPTIONS: { value: TriggerType; label: string; icon: React.ReactNode; description: string }[] = [
  { value: 'Manual',  label: 'Manual',  icon: <MousePointerClick size={16} />, description: 'Run only when triggered manually from the UI or API.' },
  { value: 'Webhook', label: 'Webhook', icon: <Webhook size={16} />,            description: 'Run when an HTTP POST is sent to the webhook URL.' },
  { value: 'Cron',    label: 'Scheduled (Cron)', icon: <Clock size={16} />,     description: 'Run automatically on a cron schedule (UTC).' },
];

/** Common cron presets to make the UI friendlier. */
const CRON_PRESETS: { label: string; value: string }[] = [
  { label: 'Every minute',  value: '* * * * *' },
  { label: 'Every 5 min',   value: '*/5 * * * *' },
  { label: 'Every hour',    value: '0 * * * *' },
  { label: 'Daily at 9 AM', value: '0 9 * * *' },
  { label: 'Every Monday',  value: '0 9 * * 1' },
];

export function TriggerSettingsPanel({ workflow }: Props) {
  const qc = useQueryClient();
  const [triggerType, setTriggerType] = useState<TriggerType>((workflow.triggerType as TriggerType) ?? 'Manual');
  const [cronExpression, setCronExpression] = useState(workflow.cronExpression ?? '');
  const [saved, setSaved] = useState(false);

  const mutation = useMutation({
    mutationFn: () =>
      api.workflows.updateTrigger(workflow.id, {
        triggerType,
        cronExpression: triggerType === 'Cron' ? cronExpression : null,
        webhookSecret: triggerType === 'Webhook' ? (workflow.webhookSecret ?? undefined) : null,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['workflow', workflow.id] });
      setSaved(true);
      setTimeout(() => setSaved(false), 2500);
    },
  });

  const isDirty =
    triggerType !== (workflow.triggerType ?? 'Manual') ||
    (triggerType === 'Cron' && cronExpression !== (workflow.cronExpression ?? ''));

  return (
    <div className="h-full flex flex-col bg-white border-l border-slate-200 w-72 overflow-y-auto">
      <div className="p-4 border-b border-slate-100">
        <h3 className="text-sm font-semibold text-slate-800">Trigger Settings</h3>
        <p className="text-xs text-slate-500 mt-0.5">How this workflow is started</p>
      </div>

      <div className="p-4 space-y-3 flex-1">
        {/* Trigger type selector */}
        {TRIGGER_OPTIONS.map(opt => (
          <button
            key={opt.value}
            onClick={() => setTriggerType(opt.value)}
            className={`w-full text-left flex items-start gap-3 p-3 rounded-xl border transition-colors ${
              triggerType === opt.value
                ? 'border-indigo-400 bg-indigo-50'
                : 'border-slate-200 hover:border-slate-300 hover:bg-slate-50'
            }`}
          >
            <span className={`mt-0.5 ${triggerType === opt.value ? 'text-indigo-600' : 'text-slate-400'}`}>
              {opt.icon}
            </span>
            <div>
              <p className={`text-sm font-medium ${triggerType === opt.value ? 'text-indigo-800' : 'text-slate-700'}`}>
                {opt.label}
              </p>
              <p className="text-xs text-slate-500 mt-0.5">{opt.description}</p>
            </div>
          </button>
        ))}

        {/* Cron expression input */}
        {triggerType === 'Cron' && (
          <div className="space-y-2 pt-1">
            <label className="block text-xs font-medium text-slate-700">
              Cron Expression <span className="text-slate-400 font-normal">(UTC)</span>
            </label>
            <input
              type="text"
              value={cronExpression}
              onChange={e => setCronExpression(e.target.value)}
              placeholder="0 9 * * *"
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-indigo-500"
            />
            {/* Presets */}
            <div className="flex flex-wrap gap-1.5">
              {CRON_PRESETS.map(p => (
                <button
                  key={p.value}
                  onClick={() => setCronExpression(p.value)}
                  className="text-xs px-2 py-0.5 rounded bg-slate-100 hover:bg-indigo-100 hover:text-indigo-700 text-slate-600 transition-colors"
                >
                  {p.label}
                </button>
              ))}
            </div>
            {cronExpression && (
              <p className="text-xs text-slate-400 font-mono bg-slate-50 rounded p-2">
                {cronExpression}
              </p>
            )}
          </div>
        )}

        {/* Webhook URL display */}
        {triggerType === 'Webhook' && (
          <div className="bg-slate-50 rounded-xl p-3 text-xs text-slate-500 border border-slate-200">
            <p className="font-medium text-slate-700 mb-1">Webhook URL</p>
            <p className="font-mono break-all text-slate-400">/api/external-webhooks/trigger/{workflow.id}</p>
            <p className="mt-1.5 text-slate-400">Send a POST request with an optional JSON body to trigger this workflow.</p>
          </div>
        )}
      </div>

      {/* Save */}
      <div className="p-4 border-t border-slate-100">
        {mutation.isError && (
          <p className="text-xs text-red-600 mb-2">{(mutation.error as Error).message}</p>
        )}
        <button
          onClick={() => mutation.mutate()}
          disabled={!isDirty || mutation.isPending || (triggerType === 'Cron' && !cronExpression.trim())}
          className="w-full flex items-center justify-center gap-2 py-2 rounded-lg text-sm font-medium bg-indigo-600 hover:bg-indigo-700 text-white disabled:opacity-50 transition-colors"
        >
          {saved ? <><CheckCircle2 size={14} /> Saved</> : mutation.isPending ? 'Saving…' : 'Save Trigger'}
        </button>
      </div>
    </div>
  );
}
