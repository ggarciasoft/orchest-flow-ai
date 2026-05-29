'use client';
import { useState, useRef, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api, ApprovalComment } from '@/lib/api';
import { Send, Loader2, MessageCircle } from 'lucide-react';

interface ApprovalCommentThreadProps {
  approvalId: string;
  /** Whether new comments can be posted (false when approval is resolved) */
  canPost?: boolean;
}

/**
 * ApprovalCommentThread — a chat-like discussion thread on an approval request.
 * Lists existing comments and allows authenticated users to post new ones.
 */
export default function ApprovalCommentThread({ approvalId, canPost = true }: ApprovalCommentThreadProps) {
  const qc = useQueryClient();
  const [text, setText] = useState('');
  const bottomRef = useRef<HTMLDivElement>(null);

  const { data: comments, isLoading } = useQuery({
    queryKey: ['approval-comments', approvalId],
    queryFn: () => api.approvals.listComments(approvalId),
    refetchInterval: 10_000,
  });

  const postMutation = useMutation({
    mutationFn: () => api.approvals.addComment(approvalId, text.trim()),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['approval-comments', approvalId] });
      setText('');
    },
  });

  // Scroll to bottom when new comments arrive
  useEffect(() => {
    if (comments?.length) {
      bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [comments?.length]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (text.trim()) postMutation.mutate();
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      if (text.trim()) postMutation.mutate();
    }
  };

  const formatTime = (iso: string) => {
    const d = new Date(iso);
    return d.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  };

  return (
    <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
      {/* Header */}
      <div className="px-5 py-3 border-b border-slate-100 flex items-center gap-2">
        <MessageCircle size={15} className="text-slate-400" />
        <span className="text-sm font-medium text-slate-700">
          Discussion
          {comments && comments.length > 0 && (
            <span className="ml-1.5 text-xs text-slate-400 font-normal">({comments.length})</span>
          )}
        </span>
      </div>

      {/* Messages */}
      <div className="px-5 py-4 space-y-4 max-h-80 overflow-y-auto">
        {isLoading && (
          <div className="flex items-center gap-2 text-slate-400 text-sm">
            <Loader2 size={13} className="animate-spin" /> Loading…
          </div>
        )}
        {!isLoading && (!comments || comments.length === 0) && (
          <p className="text-sm text-slate-400 text-center py-2">No comments yet. Be the first to add context.</p>
        )}
        {comments?.map((c: ApprovalComment) => (
          <div key={c.id} className="flex gap-3">
            {/* Avatar */}
            <div className="w-7 h-7 rounded-full bg-indigo-100 text-indigo-600 flex items-center justify-center text-xs font-semibold shrink-0 mt-0.5">
              {c.authorName.charAt(0).toUpperCase()}
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-baseline gap-2">
                <span className="text-xs font-semibold text-slate-800">{c.authorName}</span>
                <span className="text-xs text-slate-400">{formatTime(c.createdAt)}</span>
              </div>
              <p className="text-sm text-slate-700 mt-0.5 whitespace-pre-wrap break-words">{c.text}</p>
            </div>
          </div>
        ))}
        <div ref={bottomRef} />
      </div>

      {/* Input */}
      {canPost && (
        <form onSubmit={handleSubmit} className="px-5 py-3 border-t border-slate-100 flex gap-2 items-end">
          <textarea
            className="flex-1 border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 resize-none"
            placeholder="Add a comment… (Enter to send, Shift+Enter for new line)"
            rows={2}
            value={text}
            onChange={e => setText(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={postMutation.isPending}
          />
          <button
            type="submit"
            disabled={!text.trim() || postMutation.isPending}
            className="shrink-0 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-40 text-white px-3 py-2 rounded-lg flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            {postMutation.isPending ? <Loader2 size={14} className="animate-spin" /> : <Send size={14} />}
            Send
          </button>
        </form>
      )}
    </div>
  );
}
