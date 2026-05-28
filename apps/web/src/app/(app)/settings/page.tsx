'use client';
import Link from 'next/link';
import { PageHeader } from '@/components/ui';
import { Cpu, KeyRound, Plug } from 'lucide-react';

export default function SettingsPage() {
  return (
    <div>
      <PageHeader title="Settings" subtitle="Platform configuration" />

      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        {/* AI Providers */}
        <Link href="/settings/providers"
          className="bg-white border border-slate-200 rounded-xl p-5 flex items-center gap-4 hover:border-indigo-300 hover:shadow-sm transition-all group">
          <div className="w-10 h-10 bg-indigo-600 rounded-xl flex items-center justify-center shrink-0">
            <Cpu size={18} className="text-white" />
          </div>
          <div>
            <p className="text-sm font-semibold text-slate-900 group-hover:text-indigo-700 transition-colors">AI Providers</p>
            <p className="text-xs text-slate-500 mt-0.5">OpenAI, Anthropic, Azure OpenAI, Ollama</p>
          </div>
        </Link>

        {/* Integrations */}
        <Link href="/settings/integrations"
          className="bg-white border border-slate-200 rounded-xl p-5 flex items-center gap-4 hover:border-rose-300 hover:shadow-sm transition-all group">
          <div className="w-10 h-10 bg-rose-500 rounded-xl flex items-center justify-center shrink-0">
            <Plug size={18} className="text-white" />
          </div>
          <div>
            <p className="text-sm font-semibold text-slate-900 group-hover:text-rose-600 transition-colors">Integrations</p>
            <p className="text-xs text-slate-500 mt-0.5">Gmail and other external service connections</p>
          </div>
        </Link>

        {/* Secrets */}
        <Link href="/settings/secrets"
          className="bg-white border border-slate-200 rounded-xl p-5 flex items-center gap-4 hover:border-violet-300 hover:shadow-sm transition-all group">
          <div className="w-10 h-10 bg-amber-600 rounded-xl flex items-center justify-center shrink-0">
            <KeyRound size={18} className="text-white" />
          </div>
          <div>
            <p className="text-sm font-semibold text-slate-900 group-hover:text-violet-700 transition-colors">Secrets Vault</p>
            <p className="text-xs text-slate-500 mt-0.5">Encrypted named values for workflow nodes</p>
          </div>
        </Link>
      </div>
    </div>
  );
}
