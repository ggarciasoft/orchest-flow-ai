'use client';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, DocumentMeta } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import { formatDate } from '@/lib/utils';
import { CheckCircle, XCircle, ClipboardList, ArrowRight, FileText, Search, Loader2 } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Badge, EmptyState, statusLabel } from '@/components/ui';

/**
 * ApprovalsPage — inbox for pending approval requests.
 * - Form-node approvals: shows a "Fill Form →" link to the detail page.
 * - DocumentSelection approvals: shows a searchable document picker.
 * - Human-approval nodes: inline Approve/Reject with optional comment.
 */
export default function ApprovalsPage() {
  const { isApprover } = useAuth();
  const qc = useQueryClient();
  const [comment, setComment] = useState<Record<string, string>>({});
  const [docSearch, setDocSearch] = useState<Record<string, string>>({});
  const [selectingDoc, setSelectingDoc] = useState<string | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['approvals', 'Pending'],
    queryFn: () => api.approvals.list('Pending'),
    refetchInterval: 10_000,
  });

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
      <PageHeader title="Task Inbox" subtitle="Review and act on pending workflow tasks" />

      {isLoading ? (
        <div className="space-y-3">{[1, 2].map(i => <div key={i} className="h-40 bg-gray-200 rounded-xl animate-pulse" />)}</div>
      ) : data?.items.length === 0 ? (
        <EmptyState icon={CheckCircle} title="All clear!" subtitle="No pending approvals" />
      ) : (
        <div className="bg-white border border-slate-200 rounded-xl divide-y divide-slate-100">
          {data?.items.map(a => {
            let payload: Record<string, unknown> = {};
            try { payload = JSON.parse(a.payloadJson); } catch { /* ignore */ }

            const isFormApproval = !!payload._formId;
            const isDocSelection = payload._approvalKind === 'DocumentSelection';
            const approvalTitle = isFormApproval
              ? String(payload._formName ?? 'Form Submission Required')
              : String(payload._approvalTitle ?? payload.title ?? 'Approval Required');
            const visibleFields = Object.entries(payload).filter(([k]) => !k.startsWith('_'));

            return (
              <div key={a.id} className="p-6 space-y-4">
                {/* Header */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <h3 className="font-semibold text-lg">
                      <Link href={`/approvals/${a.id}`} className="hover:text-indigo-600 transition-colors">
                        {approvalTitle}
                      </Link>
                    </h3>
                    {isFormApproval && (
                      <span className="flex items-center gap-1 text-xs text-indigo-600 bg-indigo-50 border border-indigo-200 rounded-full px-2 py-0.5">
                        <ClipboardList size={11} /> Form
                      </span>
                    )}
                    {isDocSelection && (
                      <span className="flex items-center gap-1 text-xs text-violet-600 bg-violet-50 border border-violet-200 rounded-full px-2 py-0.5">
                        <FileText size={11} /> Document
                      </span>
                    )}
                  </div>
                  {(a.workflowName || a.workflowVersionNumber != null) && (
                    <p className="text-xs text-slate-400 mt-0.5">
                      {a.workflowName && <span>{a.workflowName}</span>}
                      {a.workflowVersionNumber != null && <span className="ml-1">v{a.workflowVersionNumber}</span>}
                      {isFormApproval && a.formVersionNumber != null && <span className="ml-2 text-violet-500">Form v{a.formVersionNumber}</span>}
                    </p>
                  )}
                  <Badge variant="warning">{statusLabel(a.status)}</Badge>
                </div>

                {/* Form approval */}
                {isFormApproval ? (
                  <Link
                    href={`/approvals/${a.id}`}
                    className="flex items-center gap-2 text-sm font-medium text-indigo-600 hover:text-indigo-800 bg-indigo-50 border border-indigo-200 rounded-lg px-4 py-3 w-full transition-colors"
                  >
                    <ClipboardList size={15} />
                    Fill in the form to continue this workflow
                    <ArrowRight size={14} className="ml-auto" />
                  </Link>

                /* Document selection */
                ) : isDocSelection ? (
                  <DocumentPicker
                    approvalId={a.id}
                    prompt={String(payload._prompt ?? 'Please select a document to continue.')}
                    search={docSearch[a.id] ?? ''}
                    onSearchChange={s => setDocSearch(prev => ({ ...prev, [a.id]: s }))}
                    selecting={selectingDoc === a.id}
                    onSelect={async (doc) => {
                      setSelectingDoc(a.id);
                      try {
                        await api.approvals.selectDocument(a.id, {
                          documentId: doc.id,
                          filename: doc.filename,
                          mimeType: doc.mimeType,
                          sizeBytes: doc.sizeBytes,
                          sha256: doc.sha256,
                        });
                        qc.invalidateQueries({ queryKey: ['approvals'] });
                      } finally {
                        setSelectingDoc(null);
                      }
                    }}
                  />

                /* Human approval */
                ) : (
                  <>
                    {visibleFields.length > 0 && (
                      <div className="bg-gray-50 rounded-lg p-4 text-sm space-y-1.5">
                        {visibleFields.map(([k, v]) => (
                          <div key={k} className="flex gap-2">
                            <span className="font-medium text-gray-600 shrink-0">{k}:</span>
                            <span className="text-gray-800 break-all">{String(v)}</span>
                          </div>
                        ))}
                      </div>
                    )}
                    {isApprover ? (
                      <>
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
                      </>
                    ) : (
                      <p className="text-xs text-slate-400 italic">You need the Approver or Admin role to act on this request.</p>
                    )}
                  </>
                )}

                <div className="text-xs text-gray-400">Requested: {formatDate(a.requestedAt)}</div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

// ── Document picker sub-component ────────────────────────────────────────────

function DocumentPicker({
  approvalId,
  prompt,
  search,
  onSearchChange,
  selecting,
  onSelect,
}: {
  approvalId: string;
  prompt: string;
  search: string;
  onSearchChange: (s: string) => void;
  selecting: boolean;
  onSelect: (doc: DocumentMeta) => Promise<void>;
}) {
  const { data, isLoading } = useQuery({
    queryKey: ['documents', 'picker', search],
    queryFn: () => api.documents.list({ page: 1, pageSize: 50, search: search || undefined }),
    staleTime: 30_000,
  });

  return (
    <div className="space-y-3">
      <p className="text-sm text-slate-600">{prompt}</p>

      {/* Search */}
      <div className="relative">
        <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
        <input
          type="text"
          placeholder="Search documents…"
          value={search}
          onChange={e => onSearchChange(e.target.value)}
          className="w-full border rounded-lg pl-8 pr-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet-500"
        />
      </div>

      {/* List */}
      <div className="border rounded-lg divide-y max-h-64 overflow-y-auto">
        {isLoading ? (
          <div className="flex items-center justify-center py-8 text-slate-400 text-sm gap-2">
            <Loader2 size={14} className="animate-spin" /> Loading documents…
          </div>
        ) : !data?.items.length ? (
          <div className="py-8 text-center text-slate-400 text-sm">No documents found</div>
        ) : (
          data.items.map(doc => (
            <button
              key={doc.id}
              disabled={selecting}
              onClick={() => onSelect(doc)}
              className="w-full text-left px-4 py-3 hover:bg-violet-50 transition-colors flex items-center gap-3 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <FileText size={16} className="text-violet-500 shrink-0" />
              <div className="min-w-0 flex-1">
                <p className="text-sm font-medium text-slate-800 truncate">{doc.filename}</p>
                <p className="text-xs text-slate-400">{doc.mimeType} · {formatBytes(doc.sizeBytes)} · {formatDate(doc.createdAt)}</p>
              </div>
              {selecting && <Loader2 size={14} className="animate-spin text-violet-500 shrink-0" />}
            </button>
          ))
        )}
      </div>
    </div>
  );
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
