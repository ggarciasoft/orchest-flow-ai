'use client';
import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, TenantConfig } from '@/lib/api';
import { getTenantId } from '@/lib/auth';
import { PageHeader } from '@/components/ui';
import Link from 'next/link';
import { ArrowLeft, Save, Loader2 } from 'lucide-react';

/**
 * TenantSettingsPage — lets admins view and update per-tenant configuration.
 */
export default function TenantSettingsPage() {
  const qc = useQueryClient();
  const tenantId = getTenantId() ?? '';

  const { data: config, isLoading } = useQuery<TenantConfig>({
    queryKey: ['tenantConfig', tenantId],
    queryFn: () => api.tenants.getConfig(tenantId),
    enabled: !!tenantId,
  });

  const [form, setForm] = useState<Partial<TenantConfig>>({});
  const [saved, setSaved] = useState(false);

  // Populate form once config loads
  useEffect(() => {
    if (config) setForm({ ...config });
  }, [config]);

  const mutation = useMutation({
    mutationFn: () => api.tenants.updateConfig(tenantId, form),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['tenantConfig'] });
      setSaved(true);
      setTimeout(() => setSaved(false), 2500);
    },
  });

  function set<K extends keyof TenantConfig>(key: K, value: TenantConfig[K]) {
    setForm(prev => ({ ...prev, [key]: value }));
  }

  if (isLoading) return (
    <div className="space-y-4">
      {[1,2,3].map(i => <div key={i} className="h-14 bg-slate-100 rounded-xl animate-pulse" />)}
    </div>
  );

  return (
    <div className="max-w-2xl space-y-6">
      <Link href="/settings" className="flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700">
        <ArrowLeft size={14} /> Back to Settings
      </Link>

      <PageHeader title="Tenant Configuration" subtitle="Branding, execution limits, and feature flags for your workspace" />

      <div className="bg-white border border-slate-200 rounded-xl divide-y divide-slate-100">

        {/* Branding */}
        <div className="p-5">
          <h3 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-4">Branding</h3>
          <div className="space-y-4">
            <Field label="Display Name" hint="Shown in the UI instead of the internal workspace name">
              <input
                type="text"
                value={form.displayName ?? ''}
                onChange={e => set('displayName', e.target.value || null)}
                placeholder="Acme Corp"
                className={inputCls}
              />
            </Field>
            <Field label="Logo URL" hint="Optional. HTTPS URL to a PNG or SVG logo image">
              <input
                type="url"
                value={form.logoUrl ?? ''}
                onChange={e => set('logoUrl', e.target.value || null)}
                placeholder="https://example.com/logo.png"
                className={inputCls}
              />
            </Field>
          </div>
        </div>

        {/* Execution limits */}
        <div className="p-5">
          <h3 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-4">Execution Limits</h3>
          <div className="space-y-4">
            <Field label="Max Concurrent Executions" hint="Maximum number of queued + running executions at once. 0 = unlimited">
              <input
                type="number"
                min={0}
                value={form.maxConcurrentExecutions ?? 10}
                onChange={e => set('maxConcurrentExecutions', Number(e.target.value))}
                className={inputCls}
              />
            </Field>
            <Field label="Execution Timeout (seconds)" hint="Maximum wall-clock seconds per execution. 0 = unlimited">
              <input
                type="number"
                min={0}
                value={form.executionTimeoutSeconds ?? 3600}
                onChange={e => set('executionTimeoutSeconds', Number(e.target.value))}
                className={inputCls}
              />
            </Field>
          </div>
        </div>

        {/* Locale */}
        <div className="p-5">
          <h3 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-4">Locale</h3>
          <Field label="Default Timezone" hint="IANA timezone used for cron display and execution timestamps (e.g. America/New_York)">
            <input
              type="text"
              value={form.defaultTimezone ?? 'UTC'}
              onChange={e => set('defaultTimezone', e.target.value)}
              placeholder="UTC"
              className={inputCls}
            />
          </Field>
        </div>

        {/* Features */}
        <div className="p-5">
          <h3 className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-4">Features</h3>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-slate-900">Allow Guest Form Fill</p>
              <p className="text-xs text-slate-500 mt-0.5">When off, the form fill page requires authentication</p>
            </div>
            <button
              type="button"
              onClick={() => set('allowGuestFormFill', !form.allowGuestFormFill)}
              className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                form.allowGuestFormFill ? 'bg-indigo-600' : 'bg-slate-200'
              }`}
              role="switch"
              aria-checked={form.allowGuestFormFill}
            >
              <span className={`inline-block h-4 w-4 rounded-full bg-white shadow transition-transform ${
                form.allowGuestFormFill ? 'translate-x-6' : 'translate-x-1'
              }`} />
            </button>
          </div>
        </div>
      </div>

      {mutation.isError && (
        <p className="text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-4 py-2">
          {(mutation.error as Error).message ?? 'Failed to save configuration'}
        </p>
      )}

      <div className="flex items-center gap-3">
        <button
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending}
          className="flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-lg disabled:opacity-50 transition-colors"
        >
          {mutation.isPending ? <Loader2 size={14} className="animate-spin" /> : <Save size={14} />}
          {mutation.isPending ? 'Saving…' : 'Save Configuration'}
        </button>
        {saved && <span className="text-sm text-emerald-600">✓ Saved</span>}
      </div>
    </div>
  );
}

const inputCls = 'w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500';

function Field({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
  return (
    <div>
      <label className="block text-sm font-medium text-slate-700 mb-1">{label}</label>
      {hint && <p className="text-xs text-slate-400 mb-1.5">{hint}</p>}
      {children}
    </div>
  );
}
