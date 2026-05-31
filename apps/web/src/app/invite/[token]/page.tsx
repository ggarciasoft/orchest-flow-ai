'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter, useSearchParams } from 'next/navigation';
import { api, InvitePreviewResponse } from '@/lib/api';
import { setToken } from '@/lib/auth';
import { CheckCircle2, AlertTriangle, Loader2 } from 'lucide-react';

/**
 * AcceptInvitePage — allows an invitee to set their password and join the workspace.
 *
 * URL: /invite/[tenantId]?token=<invite_token>
 * On success, auto-logs in and redirects to the dashboard.
 */
export default function AcceptInvitePage() {
  const params = useParams<{ token: string }>();
  const searchParams = useSearchParams();
  const router = useRouter();

  // The dynamic segment is the tenantId; the query param is the actual invite token
  const tenantId = params.token;
  const inviteToken = searchParams.get('token') ?? '';

  const [preview, setPreview]           = useState<InvitePreviewResponse | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [previewLoading, setPreviewLoading] = useState(true);

  const [password, setPassword]             = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError]   = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  // Fetch invite preview on mount — shows workspace name, email, role
  useEffect(() => {
    if (!tenantId || !inviteToken) {
      setPreviewError('Invalid invite link.');
      setPreviewLoading(false);
      return;
    }
    api.tenants.invitePreview(tenantId, inviteToken)
      .then(data => setPreview(data))
      .catch(() => setPreviewError('This invite link is invalid, expired, or has already been used.'))
      .finally(() => setPreviewLoading(false));
  }, [tenantId, inviteToken]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (!password)                           { setError('Password is required.');                     return; }
    if (password !== confirmPassword)        { setError('Passwords do not match.');                   return; }
    if (password.length < 8)                 { setError('Password must be at least 8 characters.');   return; }

    setLoading(true);
    try {
      const result = await api.tenants.acceptInvite(tenantId, inviteToken, password);
      // Auto-login with the returned JWT
      setToken(result.token);
      setSuccess(true);
      setTimeout(() => router.push('/workflows'), 1500);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to accept invite.');
    } finally {
      setLoading(false);
    }
  }

  if (previewLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50">
        <Loader2 className="h-6 w-6 animate-spin text-indigo-500" />
      </div>
    );
  }

  if (previewError) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
        <div className="max-w-sm rounded-xl border border-slate-200 bg-white p-8 text-center shadow-sm">
          <AlertTriangle className="mx-auto mb-4 h-10 w-10 text-amber-400" />
          <h2 className="text-lg font-semibold text-slate-900">Invite not found</h2>
          <p className="mt-2 text-sm text-slate-500">{previewError}</p>
        </div>
      </div>
    );
  }

  if (success) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
        <div className="max-w-sm rounded-xl border border-slate-200 bg-white p-8 text-center shadow-sm space-y-4">
          <CheckCircle2 className="mx-auto h-12 w-12 text-emerald-500" />
          <h2 className="text-xl font-bold text-slate-900">Welcome to {preview?.tenantName}!</h2>
          <p className="text-sm text-slate-500">Taking you to the dashboard…</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-6">
      <div className="w-full max-w-sm rounded-xl border border-slate-200 bg-white p-8 shadow-sm space-y-6">
        {/* Logo */}
        <div className="flex items-center justify-center">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-600">
            <span className="text-lg font-bold text-white">O</span>
          </div>
        </div>

        {/* Invite summary */}
        <div className="rounded-lg border border-indigo-100 bg-indigo-50 px-4 py-3 text-sm">
          <p className="font-medium text-indigo-800">
            You&apos;re joining <strong>{preview?.tenantName}</strong>
          </p>
          <p className="mt-0.5 text-indigo-600">
            as <strong>{preview?.role}</strong> · {preview?.email}
          </p>
        </div>

        <div>
          <h2 className="text-xl font-bold text-slate-900">Create your account</h2>
          <p className="mt-1 text-sm text-slate-500">Set a password to complete your registration.</p>
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
            className="w-full rounded-lg bg-indigo-600 py-2 font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
            disabled={loading}
          >
            {loading ? 'Creating account…' : 'Create account & join'}
          </button>
        </form>

        <p className="text-center text-xs text-slate-400">
          Already have an account?{' '}
          <a href="/login" className="text-indigo-600 hover:underline">Sign in</a>
        </p>
      </div>
    </div>
  );
}
