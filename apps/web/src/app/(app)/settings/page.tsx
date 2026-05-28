'use client';
import Link from 'next/link';
import { PageHeader } from '@/components/ui';
import { Cpu, KeyRound } from 'lucide-react';

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
            <p className="text-xs text-slate-500 mt-0.5">OpenAI, Anthropic, Azure, Ollama, Gmail</p>
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
