'use client';
import Link from 'next/link';
import { PageHeader } from '@/components/ui';
import { useAuth } from '@/contexts/AuthContext';
import { Cpu, KeyRound, Plug, Building2, SlidersHorizontal, MessageSquare, BookOpen, Users } from 'lucide-react';

export default function SettingsPage() {
  const { isAdmin, canEdit } = useAuth();

  return (
    <div>
      <PageHeader title="Settings" subtitle="Platform configuration" />

      <div className="mt-6 grid gap-4 sm:grid-cols-2">

        {/* ── Admin-only cards ───────────────────────────────────────────── */}
        {isAdmin && (
          <>
            <Link href="/settings/tenant"
              className="bg-white border border-slate-200 rounded-xl p-5 flex items-center gap-4 hover:border-emerald-300 hover:shadow-sm transition-all group">
              <div className="w-10 h-10 bg-emerald-600 rounded-xl flex items-center justify-center shrink-0">
                <Building2 size={18} className="text-white" />
              </div>
              <div>
                <p className="text-sm font-semibold text-slate-900 group-hover:text-emerald-700 transition-colors">Tenant Configuration</p>
                <p className="text-xs text-slate-500 mt-0.5">Branding, execution limits, timezone, feature flags</p>
              </div>
            </Link>

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

            <Link href="/settings/team"
              className="bg-white border border-slate-200 rounded-xl p-5 flex items-center gap-4 hover:border-teal-300 hover:shadow-sm transition-all group">
              <div className="w-10 h-10 bg-teal-600 rounded-xl flex items-center justify-center shrink-0">
                <Users size={18} className="text-white" />
              </div>
              <div>
                <p className="text-sm font-semibold text-slate-900 group-hover:text-teal-700 transition-colors">Team</p>
                <p className="text-xs text-slate-500 mt-0.5">Invite members, assign roles, manage access</p>
              </div>
            </Link>

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
          </>
        )}

        {/* ── Editor+ cards ──────────────────────────────────────────────── */}
        {canEdit && (
          <Link href="/settings/presets"
            className="bg-white border border-slate-200 rounded-xl p-5 flex items-center gap-4 hover:border-sky-300 hover:shadow-sm transition-all group">
            <div className="w-10 h-10 bg-sky-600 rounded-xl flex items-center justify-center shrink-0">
              <BookOpen size={18} className="text-white" />
            </div>
            <div>
              <p className="text-sm font-semibold text-slate-900 group-hover:text-sky-700 transition-colors">Node Presets</p>
              <p className="text-xs text-slate-500 mt-0.5">Reusable node configuration templates</p>
            </div>
          </Link>
        )}

        {/* ── All-role cards ─────────────────────────────────────────────── */}
        <Link href="/settings/config"
          className="bg-white border border-slate-200 rounded-xl p-5 flex items-center gap-4 hover:border-teal-300 hover:shadow-sm transition-all group">
          <div className="w-10 h-10 bg-teal-600 rounded-xl flex items-center justify-center shrink-0">
            <SlidersHorizontal size={18} className="text-white" />
          </div>
          <div>
            <p className="text-sm font-semibold text-slate-900 group-hover:text-teal-700 transition-colors">Configuration</p>
            <p className="text-xs text-slate-500 mt-0.5">Persistent workflow state key-value store</p>
          </div>
        </Link>

        <Link href="/settings/ai-history"
          className="bg-white border border-slate-200 rounded-xl p-5 flex items-center gap-4 hover:border-indigo-300 hover:shadow-sm transition-all group">
          <div className="w-10 h-10 bg-slate-600 rounded-xl flex items-center justify-center shrink-0">
            <MessageSquare size={18} className="text-white" />
          </div>
          <div>
            <p className="text-sm font-semibold text-slate-900 group-hover:text-indigo-700 transition-colors">AI Chat History</p>
            <p className="text-xs text-slate-500 mt-0.5">Past AI assistant sessions and messages</p>
          </div>
        </Link>

      </div>
    </div>
  );
}
