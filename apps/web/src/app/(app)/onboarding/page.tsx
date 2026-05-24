'use client';
import { useState } from 'react';
import { api } from '@/lib/api';

/**
 * OnboardingPage — multi-step guided flow for creating a new tenant workspace
 * and inviting team members.
 *
 * Step 1: Name your workspace (creates the tenant via POST /api/tenants)
 * Step 2: Invite your team (sends invites via POST /api/tenants/{id}/invite)
 * Step 3: All set — shows summary and link to dashboard
 */
export default function OnboardingPage() {
  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [tenantId, setTenantId] = useState<string>('');
  const [tenantName, setTenantName] = useState('');
  const [workspaceName, setWorkspaceName] = useState('');
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('Viewer');
  const [inviteLink, setInviteLink] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  /** Step indicator labels for progress display. */
  const steps = ['Name your workspace', 'Invite your team', "You're all set!"];

  /** Handles step 1: create the tenant. */
  async function handleCreateTenant() {
    if (!workspaceName.trim()) { setError('Workspace name is required.'); return; }
    setError(null);
    setLoading(true);
    try {
      const tenant = await api.tenants.create(workspaceName.trim());
      setTenantId(tenant.id);
      setTenantName(tenant.name);
      setStep(2);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to create workspace.');
    } finally {
      setLoading(false);
    }
  }

  /** Handles step 2: invite a team member. */
  async function handleInvite() {
    if (!inviteEmail.trim()) { setError('Email is required.'); return; }
    setError(null);
    setLoading(true);
    try {
      const invite = await api.tenants.invite(tenantId, inviteEmail.trim(), inviteRole);
      const acceptUrl = `${window.location.origin}/invite/${tenantId}?token=${invite.token}`;
      setInviteLink(acceptUrl);
      setInviteEmail('');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to send invite.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen bg-[#f8fafc] flex items-center justify-center p-6">
      <div className="w-full max-w-lg mx-auto bg-white border border-slate-200 rounded-xl p-8 shadow-sm space-y-6">

        {/* Step indicator */}
        <div className="flex items-center justify-center gap-4 mb-2">
          {steps.map((label, i) => (
            <div key={i} className="flex flex-col items-center gap-1">
              <div
                className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold transition-colors ${
                  step === i + 1
                    ? 'bg-indigo-600 text-white'
                    : step > i + 1
                    ? 'bg-indigo-200 text-indigo-700'
                    : 'bg-slate-200 text-slate-400'
                }`}
              >
                {i + 1}
              </div>
            </div>
          ))}
        </div>

        {/* Step 1: Name your workspace */}
        {step === 1 && (
          <div className="space-y-4">
            <h2 className="text-xl font-semibold text-slate-900">Name your workspace</h2>
            <p className="text-sm text-slate-500">This is your team&apos;s home in OrchestFlowAI. You can change it later.</p>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Workspace name</span>
              <input
                type="text"
                className="mt-1.5 block w-full border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                placeholder="Acme Inc."
                value={workspaceName}
                onChange={e => setWorkspaceName(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleCreateTenant()}
                aria-label="Workspace name"
              />
            </label>
            {error && <p className="text-sm text-red-600" role="alert">{error}</p>}
            <button
              className="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium px-5 py-2.5 rounded-lg transition-colors w-full disabled:opacity-50"
              onClick={handleCreateTenant}
              disabled={loading}
            >
              {loading ? 'Creating...' : 'Continue'}
            </button>
          </div>
        )}

        {/* Step 2: Invite your team */}
        {step === 2 && (
          <div className="space-y-4">
            <h2 className="text-xl font-semibold text-slate-900">Invite your team</h2>
            <p className="text-sm text-slate-500">Send invite links to teammates. They&apos;ll set their own password.</p>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Email address</span>
              <input
                type="email"
                className="mt-1.5 block w-full border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                placeholder="teammate@example.com"
                value={inviteEmail}
                onChange={e => setInviteEmail(e.target.value)}
                aria-label="Invitee email address"
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium text-slate-700">Role</span>
              <select
                className="mt-1.5 block w-full border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
                value={inviteRole}
                onChange={e => setInviteRole(e.target.value)}
                aria-label="Invitee role"
              >
                <option value="Admin">Admin</option>
                <option value="Editor">Editor</option>
                <option value="Approver">Approver</option>
                <option value="Viewer">Viewer</option>
              </select>
            </label>
            {error && <p className="text-sm text-red-600" role="alert">{error}</p>}
            {inviteLink && (
              <div className="bg-green-50 border border-green-200 rounded-lg p-3 text-xs break-all">
                <p className="font-medium text-green-800 mb-1">Invite link generated:</p>
                <span className="text-green-700">{inviteLink}</span>
              </div>
            )}
            <div className="flex gap-3">
              <button
                className="flex-1 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium px-5 py-2.5 rounded-lg transition-colors disabled:opacity-50"
                onClick={handleInvite}
                disabled={loading}
              >
                {loading ? 'Sending...' : 'Send invite'}
              </button>
              <button
                className="flex-1 border border-slate-200 text-slate-700 rounded-lg py-2.5 text-sm font-medium hover:bg-slate-50 transition-colors"
                onClick={() => setStep(3)}
              >
                Done inviting
              </button>
            </div>
          </div>
        )}

        {/* Step 3: All set! */}
        {step === 3 && (
          <div className="space-y-4 text-center">
            <div className="text-5xl">🎉</div>
            <h2 className="text-xl font-semibold text-slate-900">You&apos;re all set!</h2>
            <p className="text-sm text-slate-500">
              <span className="font-semibold text-slate-800">{tenantName}</span> is ready. Your team will receive their invite links shortly.
            </p>
            <a
              href="/dashboard"
              className="block w-full bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium px-5 py-2.5 rounded-lg transition-colors text-center"
            >
              Go to dashboard
            </a>
          </div>
        )}
      </div>
    </div>
  );
}
