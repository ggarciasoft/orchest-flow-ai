'use client';
import { useState } from 'react';
import { FormFieldDefinition } from '@/lib/api';
import { api } from '@/lib/api';
import { Paperclip, Loader2, CheckCircle2, XCircle } from 'lucide-react';

interface FormRendererProps {
  name: string;
  description?: string;
  fields: FormFieldDefinition[];
  /** When true, inputs are visual-only (non-functional). */
  preview?: boolean;
  values?: Record<string, unknown>;
  onChange?: (key: string, value: unknown) => void;
  /** Per-field validation error messages keyed by field key. */
  fieldErrors?: Record<string, string>;
}

/**
 * FormRenderer — renders a WorkflowForm's fields as HTML inputs.
 * Used both for the fill page and the preview modal in the builder.
 *
 * File fields upload via the Documents API and store
 * { id, filename, mimeType } as the field value.
 */
export default function FormRenderer({ name, description, fields, preview = false, values = {}, onChange, fieldErrors = {} }: FormRendererProps) {
  const inputClass =
    'w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 bg-white';

  const [uploadingFields, setUploadingFields] = useState<Record<string, boolean>>({});
  const [uploadErrors, setUploadErrors] = useState<Record<string, string>>({});

  const handleChange = (key: string, value: unknown) => {
    if (!preview && onChange) onChange(key, value);
  };

  const handleFileChange = async (key: string, file: File | null) => {
    if (!file || preview) return;
    setUploadingFields(p => ({ ...p, [key]: true }));
    setUploadErrors(p => { const n = { ...p }; delete n[key]; return n; });
    try {
      const meta = await api.documents.upload(file);
      handleChange(key, { id: meta.id, filename: meta.filename, mimeType: meta.mimeType });
    } catch (e) {
      setUploadErrors(p => ({ ...p, [key]: (e as Error).message ?? 'Upload failed' }));
    } finally {
      setUploadingFields(p => { const n = { ...p }; delete n[key]; return n; });
    }
  };

  const renderField = (field: FormFieldDefinition) => {
    if (field.type === 'boolean') {
      return (
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            className="rounded"
            checked={Boolean(values[field.key])}
            onChange={e => handleChange(field.key, e.target.checked)}
            disabled={preview}
          />
          <span className="text-sm text-slate-600">{field.placeholder || field.label}</span>
        </label>
      );
    }

    if (field.type === 'select') {
      return (
        <select
          className={inputClass}
          value={String(values[field.key] ?? '')}
          onChange={e => handleChange(field.key, e.target.value)}
          disabled={preview}
        >
          <option value="">Select…</option>
          {(field.options ?? []).map(opt => (
            <option key={opt} value={opt}>{opt}</option>
          ))}
        </select>
      );
    }

    if (field.type === 'file') {
      const uploaded = values[field.key] as { id: string; filename: string; mimeType: string } | undefined;
      const uploading = uploadingFields[field.key];
      const uploadError = uploadErrors[field.key];

      return (
        <div className="space-y-2">
          {preview ? (
            <div className="flex items-center gap-2 border border-slate-200 rounded-lg px-3 py-2 bg-slate-50 text-sm text-slate-400">
              <Paperclip size={14} />
              <span>File upload</span>
            </div>
          ) : (
            <>
              <label className={`flex items-center gap-2 border border-dashed rounded-lg px-4 py-3 cursor-pointer transition-colors ${
                uploaded ? 'border-emerald-300 bg-emerald-50' : 'border-slate-300 hover:border-indigo-300 hover:bg-indigo-50'
              }`}>
                {uploading ? (
                  <Loader2 size={16} className="text-indigo-500 animate-spin shrink-0" />
                ) : uploaded ? (
                  <CheckCircle2 size={16} className="text-emerald-500 shrink-0" />
                ) : (
                  <Paperclip size={16} className="text-slate-400 shrink-0" />
                )}
                <span className="text-sm text-slate-600 truncate">
                  {uploading ? 'Uploading…' : uploaded ? uploaded.filename : (field.placeholder || 'Click to choose a file')}
                </span>
                <input
                  type="file"
                  accept={field.accept}
                  className="sr-only"
                  onChange={e => handleFileChange(field.key, e.target.files?.[0] ?? null)}
                  disabled={uploading}
                />
              </label>
              {uploaded && !uploading && (
                <button
                  type="button"
                  onClick={() => handleChange(field.key, undefined)}
                  className="flex items-center gap-1 text-xs text-red-500 hover:text-red-700 transition-colors"
                >
                  <XCircle size={12} /> Remove
                </button>
              )}
            </>
          )}
          {uploadError && <p className="text-xs text-red-600">{uploadError}</p>}
        </div>
      );
    }

    // All other types (text, number, date, email)
    return (
      <input
        type={field.type}
        className={inputClass}
        placeholder={field.placeholder}
        value={String(values[field.key] ?? '')}
        onChange={e => handleChange(field.key, e.target.value)}
        disabled={preview}
      />
    );
  };

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-semibold text-slate-900">{name}</h1>
        {description && <p className="text-sm text-slate-500 mt-1">{description}</p>}
      </div>

      {fields.length === 0 && (
        <p className="text-sm text-slate-400 italic">No fields defined yet.</p>
      )}

      {fields.map(field => (
        <div key={field.key} className="space-y-1">
          <label className="block text-sm font-medium text-slate-700">
            {field.label}
            {field.required && <span className="text-red-500 ml-0.5">*</span>}
          </label>

          {renderField(field)}

          {fieldErrors[field.key] && (
            <p className="text-xs text-red-600 mt-0.5">{fieldErrors[field.key]}</p>
          )}
        </div>
      ))}
    </div>
  );
}
