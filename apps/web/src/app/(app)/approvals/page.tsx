'use client';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { CheckCircle, XCircle } from 'lucide-react';

/**
 * ApprovalsPage — inbox for pending human approval requests.
 * Allows reviewers to approve or reject workflow pauses with an optional comment.
 */
export default function ApprovalsPage() {
  const qc = useQueryClient();
  // Per-approval comment state keyed by approval id
  const [comment, setComment] = useState<Record<string, string>>({});
  const { data, isLoading } = useQuery({ queryKey: ['approvals', 'Pending'], queryFn: () => api.approvals.list('Pending') });

  // Invalidate approval list after approve/reject so the inbox updates immediately
  const approve = useMutation({
    mutationFn: ({ id, c }: { id: string; c?: string }) => api.approvals.approve(id, c),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['approvals'] }),
  });
  const reject = useMutation({
    mutationFn: ({ id, c }: { id: string; c?: string }) => api.approvals.reject(id, c),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['approvals'] }),
  });

  return (
    <div className="p-8 space-y-6">
      <div>
        <h2 className="text-3xl font-bold">Approval Inbox</h2>
        <p className="text-gray-500 mt-1">Review and act on pending workflow approvals</p>
      </div>

      {isLoading ? (
        <div className="space-y-3">{[1,2].map(i => <div key={i} className="h-40 bg-gray-200 rounded-xl animate-pulse" />)}</div>
      ) : data?.items.length === 0 ? (
        <div className="text-center py-16 text-gray-400">
          <CheckCircle size={48} className="mx-auto mb-4 opacity-30" />
          <p className="text-lg font-medium">All clear!</p>
          <p className="text-sm mt-1">No pending approvals</p>
        </div>
      ) : (
        <div className="space-y-4">
          {data?.items.map(a => {
            let payload: Record<string, unknown> = {};
            try { payload = JSON.parse(a.payloadJson); } catch { /* ignore */ }
            return (
              <div key={a.id} className="bg-white border border-yellow-200 rounded-xl p-6 space-y-4">
                <div className="flex items-center justify-between">
                  <h3 className="font-semibold text-lg"><Link href={`/approvals/${a.id}`}>{String(payload._approvalTitle ?? 'Approval Required')}</Link></h3>
                  <span className="text-xs px-2 py-1 rounded-full bg-yellow-100 text-yellow-800 font-medium">Pending</span>
                </div>
                <div className="bg-gray-50 rounded-lg p-4 text-sm space-y-1.5">
                  {Object.entries(payload).filter(([k]) => !k.startsWith('_')).map(([k, v]) => (
                    <div key={k} className="flex gap-2">
                      <span className="font-medium text-gray-600 shrink-0">{k}:</span>
                      <span className="text-gray-800 break-all">{String(v)}</span>
                    </div>
                  ))}
                </div>
                <div className="text-xs text-gray-400">Requested: {formatDate(a.requestedAt)}</div>
                <textarea
                  className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                  placeholder="Add a comment (optional)…"
                  rows={2}
                  value={comment[a.id] ?? ''}
                  onChange={e => setComment(c => ({ ...c, [a.id]: e.target.value }))}
                />
                <div className="flex gap-3">
                  <button
                    onClick={() => approve.mutate({ id: a.id, c: comment[a.id] })}
                    className="flex items-center gap-2 bg-green-600 hover:bg-green-700 text-white text-sm font-medium px-4 py-2 rounded-lg">
                    <CheckCircle size={14} />Approve
                  </button>
                  <button
                    onClick={() => reject.mutate({ id: a.id, c: comment[a.id] })}
                    className="flex items-center gap-2 bg-red-600 hover:bg-red-700 text-white text-sm font-medium px-4 py-2 rounded-lg">
                    <XCircle size={14} />Reject
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
