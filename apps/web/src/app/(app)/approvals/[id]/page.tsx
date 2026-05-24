'use client';
import { useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { CheckCircle, XCircle, ArrowLeft, Clock } from 'lucide-react';
import { formatDate, statusColor } from '@/lib/utils';
import Link from 'next/link';
import { PageHeader, Badge, statusVariant } from '@/components/ui';

/**
 * ApprovalDetailPage — shows full context for a single approval request.
 * Displays the payload, approve/reject actions, and the execution timeline.
 */
export default function ApprovalDetailPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const qc = useQueryClient();
  const [comment, setComment] = useState('');

  const { data: approval, isLoading } = useQuery({
    queryKey: ['approval', id],
    queryFn: () => api.approvals.get(id),
  });

  const { data: timeline } = useQuery({
    queryKey: ['timeline', approval?.workflowExecutionId],
    queryFn: () => api.executions.timeline(approval!.workflowExecutionId),
    enabled: !!approval?.workflowExecutionId,
  });

  const approve = useMutation({
    mutationFn: () => api.approvals.approve(id, comment),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['approvals'] }); router.push('/approvals'); },
  });

  const reject = useMutation({
    mutationFn: () => api.approvals.reject(id, comment),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['approvals'] }); router.push('/approvals'); },
  });

  if (isLoading || !approval) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-400">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-3" />
          <p>Loading approval…</p>
        </div>
      </div>
    );
  }

  // Parse the approval payload — skip internal keys starting with _
  let payload: Record<string, unknown> = {};
  try { payload = JSON.parse(approval.payloadJson); } catch { /* ignore */ }
  const title = String(payload._approvalTitle ?? payload.title ?? 'Approval Required');
  const visibleFields = Object.entries(payload).filter(([k]) => !k.startsWith('_'));

  return (
    <div className="p-8 max-w-3xl mx-auto space-y-6">
      <Link href="/approvals" className="flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700">
        <ArrowLeft size={14} /> Back to Approval Inbox
      </Link>
      <PageHeader title={title} />

      {/* Header */}
      <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-3">
        <div className="flex items-center justify-between">
          <Badge variant={statusVariant(approval.status)}>{approval.status}</Badge>
        </div>
        <p className="text-xs text-gray-400 flex items-center gap-1">
          <Clock size={12} /> Requested: {formatDate(approval.requestedAt)}
        </p>
        {approval.respondedAt && (
          <p className="text-xs text-gray-400">Responded: {formatDate(approval.respondedAt)}</p>
        )}
        {approval.comment && (
          <p className="text-sm text-gray-600 bg-gray-50 rounded-lg p-3">"{approval.comment}"</p>
        )}
      </div>

      {/* Payload fields */}
      {visibleFields.length > 0 && (
        <div className="bg-white border border-slate-200 rounded-xl p-6">
          <h3 className="text-xs font-medium text-slate-500 uppercase tracking-wide mb-4">Context</h3>
          <div className="space-y-2">
            {visibleFields.map(([key, value]) => (
              <div key={key} className="flex gap-3 text-sm">
                <span className="text-xs font-medium text-slate-500 uppercase tracking-wide shrink-0 w-32">{key}</span>
                <span className="text-sm text-slate-900 break-all">{String(value)}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Approve / Reject actions — only shown for pending approvals */}
      {approval.status === 'Pending' && (
        <div className="bg-white border border-slate-200 rounded-xl p-6 space-y-4">
          <h3 className="text-xs font-medium text-slate-500 uppercase tracking-wide">Decision</h3>
          <textarea
            className="w-full border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            placeholder="Add a comment (optional)…"
            rows={3}
            value={comment}
            onChange={e => setComment(e.target.value)}
          />
          <div className="flex gap-3">
            <button
              onClick={() => approve.mutate()}
              disabled={approve.isPending}
              className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white text-sm font-medium px-5 py-2.5 rounded-lg"
            >
              <CheckCircle size={16} /> Approve
            </button>
            <button
              onClick={() => reject.mutate()}
              disabled={reject.isPending}
              className="flex items-center gap-2 bg-red-600 hover:bg-red-700 disabled:opacity-50 text-white text-sm font-medium px-5 py-2.5 rounded-lg"
            >
              <XCircle size={16} /> Reject
            </button>
          </div>
        </div>
      )}

      {/* Execution timeline */}
      {timeline && timeline.nodes.length > 0 && (
        <div className="bg-white border border-slate-200 rounded-xl divide-y divide-slate-100">
          <div className="px-6 py-4"><h3 className="text-xs font-medium text-slate-500 uppercase tracking-wide">Execution Timeline</h3></div>
          <div className="divide-y divide-slate-100">
            {timeline.nodes.map((node, i) => (
              <div key={node.id} className="flex items-center justify-between px-6 py-3">
                <div className="flex items-center gap-3">
                  <span className="text-xs font-mono text-slate-400 w-6">#{i + 1}</span>
                  <div>
                    <p className="text-sm font-medium text-slate-900">{node.nodeType}</p>
                    <p className="text-xs text-slate-400 font-mono">{node.nodeId}</p>
                  </div>
                </div>
                <div className="text-right">
                  <Badge variant={statusVariant(node.status)}>{node.status}</Badge>
                  {node.completedAt && (
                    <p className="text-xs text-gray-400 mt-1">{formatDate(node.completedAt)}</p>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
