'use client';
import { FormFieldDefinition } from '@/lib/api';

interface FormRendererProps {
  name: string;
  description?: string;
  fields: FormFieldDefinition[];
  /** When true, inputs are visual-only (non-functional). */
  preview?: boolean;
  values?: Record<string, unknown>;
  onChange?: (key: string, value: unknown) => void;
}

/**
 * FormRenderer — renders a WorkflowForm's fields as HTML inputs.
 * Used both for the fill page and the preview modal in the builder.
 */
export default function FormRenderer({ name, description, fields, preview = false, values = {}, onChange }: FormRendererProps) {
  const inputClass =
    'w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 bg-white';

  const handleChange = (key: string, value: unknown) => {
    if (!preview && onChange) onChange(key, value);
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

          {field.type === 'boolean' ? (
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
          ) : field.type === 'select' ? (
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
          ) : (
            <input
              type={field.type}
              className={inputClass}
              placeholder={field.placeholder}
              value={String(values[field.key] ?? '')}
              onChange={e => handleChange(field.key, e.target.value)}
              disabled={preview}
            />
          )}
        </div>
      ))}
    </div>
  );
}
