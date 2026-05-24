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
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-6">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-lg p-8 space-y-6">

        {/* Step indicator */}
        <div className="flex items-center justify-between mb-2">
          {steps.map((label, i) => (
            <div key={i} className="flex flex-col items-center gap-1 flex-1">
              <div
                className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold border-2 transition-colors ${
                  step === i + 1
                    ? 'bg-blue-600 border-blue-600 text-white'
                    : step > i + 1
                    ? 'bg-green-500 border-green-500 text-white'
                    : 'border-gray-300 text-gray-400'
                }`}
              >
                {step > i + 1 ? '✓' : i + 1}
              </div>
              <span className={`text-xs text-center hidden sm:block ${step === i + 1 ? 'text-blue-600 font-medium' : 'text-gray-400'}`}>
                {label}
              </span>
            </div>
          ))}
        </div>

        {/* Step 1: Name your workspace */}
        {step === 1 && (
          <div className="space-y-4">
            <h2 className="text-2xl font-bold text-gray-900">Name your workspace</h2>
            <p className="text-gray-500 text-sm">This is your team&apos;s home in OrchestAI. You can change it later.</p>
            <label className="block">
              <span className="text-sm font-medium text-gray-700">Workspace name</span>
              <input
                type="text"
                className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Acme Inc."
                value={workspaceName}
                onChange={e => setWorkspaceName(e.target.value)}
                onKeyDown={e => e.key === 'Enter' && handleCreateTenant()}
                aria-label="Workspace name"
              />
            </label>
            {error && <p className="text-sm text-red-600" role="alert">{error}</p>}
            <button
              className="w-full bg-blue-600 text-white rounded-lg py-2 font-medium hover:bg-blue-700 disabled:opacity-50"
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
            <h2 className="text-2xl font-bold text-gray-900">Invite your team</h2>
            <p className="text-gray-500 text-sm">Send invite links to teammates. They&apos;ll set their own password.</p>
            <label className="block">
              <span className="text-sm font-medium text-gray-700">Email address</span>
              <input
                type="email"
                className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="teammate@example.com"
                value={inviteEmail}
                onChange={e => setInviteEmail(e.target.value)}
                aria-label="Invitee email address"
              />
            </label>
            <label className="block">
              <span className="text-sm font-medium text-gray-700">Role</span>
              <select
                className="mt-1 block w-full rounded-lg border border-gray-300 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
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
                className="flex-1 bg-blue-600 text-white rounded-lg py-2 font-medium hover:bg-blue-700 disabled:opacity-50"
                onClick={handleInvite}
                disabled={loading}
              >
                {loading ? 'Sending...' : 'Send invite'}
              </button>
              <button
                className="flex-1 border border-gray-300 text-gray-700 rounded-lg py-2 font-medium hover:bg-gray-50"
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
            <h2 className="text-2xl font-bold text-gray-900">You&apos;re all set!</h2>
            <p className="text-gray-500 text-sm">
              <span className="font-semibold text-gray-800">{tenantName}</span> is ready. Your team will receive their invite links shortly.
            </p>
            <a
              href="/dashboard"
              className="block w-full bg-blue-600 text-white rounded-lg py-2 font-medium hover:bg-blue-700 text-center"
            >
              Go to dashboard
            </a>
          </div>
        )}
      </div>
    </div>
  );
}
