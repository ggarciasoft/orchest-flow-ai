'use client';
import { useState, useRef, useEffect } from 'react';
import { api, DocumentMeta, PagedResponse } from '@/lib/api';
import { FileText, Upload, Search, ChevronLeft, ChevronRight, Loader2 } from 'lucide-react';
import { PageHeader, EmptyState } from '@/components/ui';

export default function DocumentsPage() {
  const [documents, setDocuments] = useState<DocumentMeta[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [total, setTotal] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const searchTimeoutRef = useRef<NodeJS.Timeout>();

  const fetchDocuments = async () => {
    setLoading(true);
    try {
      const response: PagedResponse<DocumentMeta> = await api.documents.list({ search: search || undefined, page, pageSize });
      setDocuments(response.items);
      setTotal(response.total);
    } catch (err) {
      console.error('Failed to load documents:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDocuments();
  }, [page]);

  useEffect(() => {
    if (searchTimeoutRef.current) clearTimeout(searchTimeoutRef.current);
    searchTimeoutRef.current = setTimeout(() => {
      setPage(1);
      fetchDocuments();
    }, 300);
    return () => { if (searchTimeoutRef.current) clearTimeout(searchTimeoutRef.current); };
  }, [search]);

  const handleUpload = async (files: FileList | null) => {
    if (!files?.length) return;
    setUploading(true);
    try {
      for (const file of Array.from(files)) {
        await api.documents.upload(file);
      }
      setPage(1);
      await fetchDocuments();
    } catch (err) {
      console.error('Upload failed:', err);
    } finally {
      setUploading(false);
    }
  };

  const totalPages = Math.ceil(total / pageSize);

  return (
    <div className="p-8 space-y-6">
      <PageHeader title="Documents" subtitle="Upload contract PDFs and other documents for your workflows" />

      <div
        className="border-2 border-dashed border-gray-300 hover:border-blue-400 transition-colors rounded-xl cursor-pointer"
        onClick={() => inputRef.current?.click()}
        onDragOver={e => e.preventDefault()}
        onDrop={e => { e.preventDefault(); handleUpload(e.dataTransfer.files); }}
      >
        <div className="py-12 text-center">
          <Upload size={40} className="mx-auto text-gray-400 mb-3" />
          <p className="font-medium text-gray-600">Click to upload or drag &amp; drop</p>
          <p className="text-sm text-gray-400 mt-1">PDF files supported · Multiple files OK</p>
          {uploading && (
            <div className="flex items-center justify-center gap-2 mt-3">
              <Loader2 size={16} className="text-blue-500 animate-spin" />
              <p className="text-sm text-blue-500 font-medium">Uploading…</p>
            </div>
          )}
        </div>
      </div>
      <input ref={inputRef} type="file" accept=".pdf" multiple className="hidden" onChange={e => handleUpload(e.target.files)} />

      <div className="flex items-center gap-3">
        <div className="relative flex-1">
          <Search size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
          <input
            type="text"
            placeholder="Search documents by filename..."
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="w-full pl-10 pr-4 py-2 border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
            suppressHydrationWarning
          />
        </div>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 size={32} className="text-gray-400 animate-spin" />
        </div>
      ) : documents.length === 0 ? (
        <EmptyState
          icon={FileText}
          title={search ? 'No documents match your search' : 'No documents yet'}
          subtitle={search ? 'Try a different search term' : 'Upload a PDF to get started'}
        />
      ) : (
        <div className="space-y-4">
          <div className="bg-white border border-slate-200 rounded-xl divide-y divide-slate-100">
            {documents.map(doc => (
              <div key={doc.id} className="flex items-center justify-between px-5 py-3">
                <div className="flex items-center gap-3 min-w-0 flex-1">
                  <FileText size={20} className="text-red-500 shrink-0" />
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-medium text-slate-900 truncate">{doc.filename}</p>
                    <p className="text-xs text-slate-400 font-mono">{doc.id}</p>
                  </div>
                </div>
                <div className="flex items-center gap-4 text-xs text-slate-400 shrink-0 ml-4">
                  <span>{(doc.sizeBytes / 1024).toFixed(1)} KB</span>
                  <span>{new Date(doc.createdAt).toLocaleDateString()}</span>
                </div>
              </div>
            ))}
          </div>

          {totalPages > 1 && (
            <div className="flex items-center justify-between">
              <p className="text-sm text-gray-600">
                Page {page} of {totalPages} ({total} total documents)
              </p>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                  disabled={page === 1}
                  className="px-3 py-1.5 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
                >
                  <ChevronLeft size={16} />
                  Previous
                </button>
                <button
                  onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                  disabled={page === totalPages}
                  className="px-3 py-1.5 text-sm border border-slate-200 rounded-lg hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1"
                >
                  Next
                  <ChevronRight size={16} />
                </button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
