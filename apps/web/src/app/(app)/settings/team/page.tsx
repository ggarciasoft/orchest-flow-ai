'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { UserPlus, Trash2, Clock, CheckCircle2, XCircle, ChevronDown } from 'lucide-react';
import { api, TenantMemberResponse, TenantInviteResponse } from '@/lib/api';
import { getTenantId } from '@/lib/auth';
import { useAuth } from '@/contexts/AuthContext';
import { AdminPageGuard } from '@/components/AdminPageGuard';
import { PageHeader } from '@/components/ui';

const ROLES = ['Admin', 'Editor', 'Approver', 'Viewer'] as const;
type Role = typeof ROLES[number];

const ROLE_COLORS: Record<string, string> = {
  Admin:    'bg-violet-100 text-violet-700 border-violet-200',
  Editor:   'bg-indigo-100 text-indigo-700 border-indigo-200',
  Approver: 'bg-amber-100  text-amber-700  border-amber-200',
  Viewer:   'bg-slate-100  text-slate-600  border-slate-200',
};

function RoleBadge({ role }: { role: string }) {
  return (
    <span className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium ${ROLE_COLORS[role] ?? 'bg-slate-100 text-slate-600'}`}>
      {role}
    </span>
  );
}

function InviteForm({ tenantId, onSuccess }: { tenantId: string; onSuccess: () => void }) {
  const [email, setEmail]   = useState('');
  const [role, setRole]     = useState<Role>('Viewer');
  const [error, setError]   = useState('');
  const [success, setSuccess] = useState('');

  const mutation = useMutation({
    mutationFn: () => api.tenants.invite(tenantId, email.trim(), role),
    onSuccess: () => {
      setEmail('');
      setRole('Viewer');
      setError('');
      setSuccess(`Invite sent to ${email.trim()}`);
      setTimeout(() => setSuccess(''), 4000);
      onSuccess();
    },
    onError: async (err: unknown) => {
      const msg = err instanceof Error ? err.message : 'Failed to send invite.';
      setError(msg);
    },
  });

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <h3 className="mb-4 text-sm font-semibold text-slate-800">Invite a new member</h3>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
        <div className="flex-1">
          <label className="mb-1 block text-xs font-medium text-slate-600">Email address</label>
          <input
            type="email"
            placeholder="colleague@company.com"
            value={email}
            onChange={e => { setEmail(e.target.value); setError(''); }}
            className="w-full rounded-lg border border-slate-200 px-3 py-2 text-sm outline-none focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100"
          />
        </div>
        <div className="w-full sm:w-36">
          <label className="mb-1 block text-xs font-medium text-slate-600">Role</label>
          <div className="relative">
            <select
              value={role}
              onChange={e => setRole(e.target.value as Role)}
              className="w-full appearance-none rounded-lg border border-slate-200 px-3 py-2 pr-8 text-sm outline-none focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100"
            >
              {ROLES.map(r => <option key={r}>{r}</option>)}
            </select>
            <ChevronDown className="pointer-events-none absolute right-2.5 top-2.5 h-3.5 w-3.5 text-slate-400" />
          </div>
        </div>
        <button
          onClick={() => { if (email.trim()) mutation.mutate(); }}
          disabled={mutation.isPending || !email.trim()}
          className="flex items-center gap-2 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
        >
          <UserPlus className="h-4 w-4" />
          {mutation.isPending ? 'Sending…' : 'Send invite'}
        </button>
      </div>
      {error   && <p className="mt-2 text-xs text-red-600">{error}</p>}
      {success && <p className="mt-2 flex items-center gap-1 text-xs text-emerald-600"><CheckCircle2 className="h-3.5 w-3.5" />{success}</p>}
    </div>
  );
}

function MembersList({
  members,
  currentUserId,
  tenantId,
  onChanged,
}: {
  members: TenantMemberResponse[];
  currentUserId: string;
  tenantId: string;
  onChanged: () => void;
}) {
  const [roleEdit, setRoleEdit] = useState<{ id: string; role: string } | null>(null);

  const changeRole = useMutation({
    mutationFn: ({ userId, role }: { userId: string; role: string }) =>
      api.tenants.updateMemberRole(tenantId, userId, role),
    onSuccess: () => { setRoleEdit(null); onChanged(); },
  });

  const remove = useMutation({
    mutationFn: (userId: string) => api.tenants.removeMember(tenantId, userId),
    onSuccess: onChanged,
  });

  return (
    <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
      <div className="border-b border-slate-100 px-5 py-3">
        <h3 className="text-sm font-semibold text-slate-800">Members <span className="ml-1 text-xs font-normal text-slate-400">({members.length})</span></h3>
      </div>
      <ul className="divide-y divide-slate-100">
        {members.map(m => (
          <li key={m.id} className="flex items-center justify-between gap-4 px-5 py-3">
            <div className="min-w-0">
              <p className="truncate text-sm font-medium text-slate-800">{m.displayName}</p>
              <p className="truncate text-xs text-slate-400">{m.email}</p>
            </div>
            <div className="flex shrink-0 items-center gap-3">
              {roleEdit?.id === m.id ? (
                <div className="flex items-center gap-1">
                  <select
                    value={roleEdit.role}
                    onChange={e => setRoleEdit({ id: m.id, role: e.target.value })}
                    className="rounded border border-slate-200 px-2 py-1 text-xs focus:outline-none focus:ring-1 focus:ring-indigo-300"
                  >
                    {ROLES.map(r => <option key={r}>{r}</option>)}
                  </select>
                  <button
                    onClick={() => changeRole.mutate({ userId: m.id, role: roleEdit.role })}
                    disabled={changeRole.isPending}
                    className="rounded bg-indigo-600 px-2 py-1 text-xs text-white hover:bg-indigo-700 disabled:opacity-50"
                  >
                    Save
                  </button>
                  <button
                    onClick={() => setRoleEdit(null)}
                    className="rounded px-1.5 py-1 text-xs text-slate-500 hover:text-slate-700"
                  >
                    <XCircle className="h-3.5 w-3.5" />
                  </button>
                </div>
              ) : (
                <button
                  onClick={() => setRoleEdit({ id: m.id, role: m.role })}
                  disabled={m.id === currentUserId}
                  title={m.id === currentUserId ? 'Cannot change your own role' : 'Change role'}
                  className="disabled:opacity-40"
                >
                  <RoleBadge role={m.role} />
                </button>
              )}
              <button
                onClick={() => remove.mutate(m.id)}
                disabled={m.id === currentUserId || remove.isPending}
                title={m.id === currentUserId ? 'Cannot remove yourself' : 'Remove member'}
                className="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-500 disabled:opacity-30"
              >
                <Trash2 className="h-4 w-4" />
              </button>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}

function PendingInvitesList({
  invites,
  tenantId,
  onChanged,
}: {
  invites: TenantInviteResponse[];
  tenantId: string;
  onChanged: () => void;
}) {
  const revoke = useMutation({
    mutationFn: (inviteId: string) => api.tenants.revokeInvite(tenantId, inviteId),
    onSuccess: onChanged,
  });

  if (invites.length === 0) return null;

  return (
    <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
      <div className="border-b border-slate-100 px-5 py-3">
        <h3 className="text-sm font-semibold text-slate-800 flex items-center gap-2">
          <Clock className="h-4 w-4 text-amber-500" />
          Pending invites
          <span className="text-xs font-normal text-slate-400">({invites.length})</span>
        </h3>
      </div>
      <ul className="divide-y divide-slate-100">
        {invites.map(i => {
          const expiresAt = new Date(i.expiresAt);
          const isExpired = expiresAt < new Date();
          return (
            <li key={i.id} className="flex items-center justify-between gap-4 px-5 py-3">
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-slate-800">{i.email}</p>
                <p className={`text-xs ${isExpired ? 'text-red-400' : 'text-slate-400'}`}>
                  {isExpired ? 'Expired' : `Expires ${expiresAt.toLocaleDateString()}`}
                </p>
              </div>
              <div className="flex shrink-0 items-center gap-3">
                <RoleBadge role={i.role} />
                <button
                  onClick={() => revoke.mutate(i.id)}
                  disabled={revoke.isPending}
                  title="Revoke invite"
                  className="rounded p-1 text-slate-400 hover:bg-red-50 hover:text-red-500 disabled:opacity-30"
                >
                  <XCircle className="h-4 w-4" />
                </button>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

export default function TeamPage() {
  const tenantId = getTenantId() ?? '';
  const { email: currentEmail } = useAuth();
  const queryClient = useQueryClient();

  const { data: members = [], isLoading: membersLoading } = useQuery({
    queryKey: ['team-members', tenantId],
    queryFn: () => api.tenants.listMembers(tenantId),
    enabled: !!tenantId,
  });

  const { data: invites = [], isLoading: invitesLoading } = useQuery({
    queryKey: ['team-invites', tenantId],
    queryFn: () => api.tenants.listInvites(tenantId),
    enabled: !!tenantId,
  });

  const currentUser = members.find(m => m.email === currentEmail);
  const currentUserId = currentUser?.id ?? '';

  function refetch() {
    queryClient.invalidateQueries({ queryKey: ['team-members', tenantId] });
    queryClient.invalidateQueries({ queryKey: ['team-invites', tenantId] });
  }

  const isLoading = membersLoading || invitesLoading;

  return (
    <AdminPageGuard>
      <div className="mx-auto max-w-3xl space-y-6 p-6">
        <PageHeader
          title="Team"
          subtitle="Manage team members and send invitations."
        />

        <InviteForm tenantId={tenantId} onSuccess={refetch} />

        {isLoading ? (
          <div className="py-8 text-center text-sm text-slate-400">Loading…</div>
        ) : (
          <>
            <MembersList
              members={members}
              currentUserId={currentUserId}
              tenantId={tenantId}
              onChanged={refetch}
            />
            <PendingInvitesList invites={invites} tenantId={tenantId} onChanged={refetch} />
          </>
        )}
      </div>
    </AdminPageGuard>
  );
}
