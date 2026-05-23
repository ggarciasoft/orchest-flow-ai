'use client';
import { useState, useRef } from 'react';
import { api } from '@/lib/api';
import { FileText, Upload, CheckCircle } from 'lucide-react';

export default function DocumentsPage() {
  const [uploads, setUploads] = useState<{ name: string; id: string; size: number }[]>([]);
  const [uploading, setUploading] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleUpload = async (files: FileList | null) => {
    if (!files?.length) return;
    setUploading(true);
    try {
      for (const file of Array.from(files)) {
        const doc = await api.documents.upload(file);
        setUploads(u => [...u, { name: file.name, id: doc.id, size: file.size }]);
      }
    } finally { setUploading(false); }
  };

  return (
    <div className="p-8 space-y-6">
      <div>
        <h2 className="text-3xl font-bold">Documents</h2>
        <p className="text-gray-500 mt-1">Upload contract PDFs and other documents for your workflows</p>
      </div>

      <div
        className="border-2 border-dashed border-gray-300 hover:border-blue-400 transition-colors rounded-xl cursor-pointer"
        onClick={() => inputRef.current?.click()}
        onDragOver={e => e.preventDefault()}
        onDrop={e => { e.preventDefault(); handleUpload(e.dataTransfer.files); }}
      >
        <div className="py-12 text-center">
          <Upload size={40} className="mx-auto text-gray-400 mb-3" />
          <p className="font-medium text-gray-600">Click to upload or drag & drop</p>
          <p className="text-sm text-gray-400 mt-1">PDF files supported · Multiple files OK</p>
          {uploading && <p className="text-sm text-blue-500 mt-3 font-medium">Uploading…</p>}
        </div>
      </div>
      <input ref={inputRef} type="file" accept=".pdf" multiple className="hidden" onChange={e => handleUpload(e.target.files)} />

      {uploads.length > 0 && (
        <div className="space-y-2">
          <h3 className="font-semibold text-gray-700">Uploaded Documents</h3>
          {uploads.map(u => (
            <div key={u.id} className="flex items-center gap-3 p-4 bg-white rounded-xl border">
              <FileText size={20} className="text-red-500 shrink-0" />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{u.name}</p>
                <p className="text-xs text-gray-400 font-mono">{u.id}</p>
              </div>
              <div className="flex items-center gap-2 text-xs text-gray-400">
                <span>{(u.size / 1024).toFixed(1)} KB</span>
                <CheckCircle size={14} className="text-green-500" />
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
