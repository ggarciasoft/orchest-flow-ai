'use client';
import { useState } from 'react';
import { useParams, useRouter, useSearchParams } from 'next/navigation';
import { api } from '@/lib/api';

/**
 * AcceptInvitePage — allows an invitee to set their password and complete registration.
 *
 * URL: /invite/[tenantId]?token=<invite_token>
 * On success, redirects to /login.
 */
export default function AcceptInvitePage() {
  const params = useParams<{ token: string }>();
  const searchParams = useSearchParams();
  const router = useRouter();

  // The dynamic segment is the tenantId; the query param is the actual invite token
  const tenantId = params.token;
  const inviteToken = searchParams.get('token') ?? '';

  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  /** Submits the password to accept the invite and create the user account. */
  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (!password) { setError('Password is required.'); return; }
    if (password !== confirmPassword) { setError('Passwords do not match.'); return; }
    if (password.length < 8) { setError('Password must be at least 8 characters.'); return; }

    setLoading(true);
    try {
      await api.tenants.acceptInvite(tenantId, inviteToken, password);
      setSuccess(true);
      setTimeout(() => router.push('/login'), 2000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to accept invite.');
    } finally {
      setLoading(false);
    }
  }

  if (success) {
    return (
      <div className="min-h-screen bg-[#f8fafc] flex items-center justify-center p-6">
        <div className="max-w-sm mx-auto bg-white border border-slate-200 rounded-xl p-8 shadow-sm text-center space-y-4">
          <div className="text-5xl">✅</div>
          <h2 className="text-2xl font-bold text-slate-900">Account created!</h2>
          <p className="text-slate-500 text-sm">Redirecting you to login…</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#f8fafc] flex items-center justify-center p-6">
      <div className="max-w-sm mx-auto bg-white border border-slate-200 rounded-xl p-8 shadow-sm space-y-6">
        {/* Logo mark */}
        <div className="flex items-center justify-center">
          <div className="w-10 h-10 rounded-xl bg-indigo-600 flex items-center justify-center">
            <span className="text-white font-bold text-lg">O</span>
          </div>
        </div>

        <div>
          <h2 className="text-2xl font-bold text-slate-900">Join your team on OrchestFlowAI</h2>
          <p className="text-slate-500 text-sm mt-1">Set a password to complete your registration.</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <label className="block">
            <span className="text-sm font-medium text-slate-700">Password</span>
            <input
              type="password"
              className="mt-1 block w-full rounded-lg border border-slate-200 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="At least 8 characters"
              value={password}
              onChange={e => setPassword(e.target.value)}
              aria-label="Password"
              required
            />
          </label>
          <label className="block">
            <span className="text-sm font-medium text-slate-700">Confirm password</span>
            <input
              type="password"
              className="mt-1 block w-full rounded-lg border border-slate-200 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="Repeat your password"
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
              aria-label="Confirm password"
              required
            />
          </label>
          {error && <p className="text-sm text-red-600" role="alert">{error}</p>}
          <button
            type="submit"
            className="w-full bg-indigo-600 text-white rounded-lg py-2 font-medium hover:bg-indigo-700 disabled:opacity-50"
            disabled={loading}
          >
            {loading ? 'Creating account…' : 'Create account'}
          </button>
        </form>
      </div>
    </div>
  );
}
