'use client';
import { useState, useEffect, use } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { api, FormFieldDefinition, FormVersionSummary, WorkflowForm } from '@/lib/api';
import { ArrowLeft, Plus, ArrowUp, ArrowDown, Pencil, Trash2, Eye, Save, FlaskConical, Sparkles } from 'lucide-react';
import { Badge } from '@/components/ui';
import FormRenderer from '../_components/FormRenderer';
import FormVersionHistory from '../_components/FormVersionHistory';
import FormAiAssistPanel from '../_components/FormAiAssistPanel';

/** Auto-generates a slug from a display name (kebab-case, alphanumeric + dash). */
function slugify(name: string): string {
  return name
    .toLowerCase()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9-]/g, '')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');
}

const TYPE_OPTIONS = ['text', 'number', 'select', 'date', 'email', 'boolean'] as const;

const emptyField = (): FormFieldDefinition => ({
  key: '',
  label: '',
  type: 'text',
  required: false,
  placeholder: '',
  options: [],
});

/**
 * FormBuilderPage — create or edit a custom form.
 * Route: /forms/new  OR  /forms/[id]
 */
export default function FormBuilderPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const isNew = id === 'new';
  const router = useRouter();
  const qc = useQueryClient();

  // Form metadata
  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [slugManual, setSlugManual] = useState(false);
  const [description, setDescription] = useState('');

  // Fields
  const [fields, setFields] = useState<FormFieldDefinition[]>([]);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [editingField, setEditingField] = useState<FormFieldDefinition>(emptyField());
  const [showPreview, setShowPreview] = useState(false);
  const [showAiPanel, setShowAiPanel] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [regexTestValue, setRegexTestValue] = useState('');
  const [showRegexTest, setShowRegexTest] = useState(false);

  // Load existing form
  const { data: form } = useQuery<WorkflowForm>({
    queryKey: ['forms', id],
    queryFn: () => api.forms.get(id),
    enabled: !isNew,
  });

  useEffect(() => {
    if (form) {
      setName(form.name);
      setSlug(form.slug);
      setSlugManual(true);
      setDescription(form.description ?? '');
      setFields(form.fields);
    }
  }, [form]);

  // Auto-slug from name
  useEffect(() => {
    if (!slugManual) {
      setSlug(slugify(name));
    }
  }, [name, slugManual]);

  const saveMutation = useMutation({
    mutationFn: () => {
      const data = { name, slug, description, fields };
      return isNew ? api.forms.create(data) : api.forms.update(id, data);
    },
    onSuccess: (saved) => {
      qc.invalidateQueries({ queryKey: ['forms'] });
      if (isNew) {
        router.replace(`/forms/${saved.id}`);
      }
    },
    onError: (err: Error) => setSaveError(err.message),
  });

  // Field editing helpers
  const openNew = () => {
    setEditingField(emptyField());
    setEditingIndex(fields.length); // sentinel = new
    setRegexTestValue('');
    setShowRegexTest(false);
  };

  const openEdit = (idx: number) => {
    setEditingField({ ...fields[idx] });
    setEditingIndex(idx);
    setRegexTestValue('');
    setShowRegexTest(false);
  };

  const saveField = () => {
    if (!editingField.key || !editingField.label) return;
    setFields(prev => {
      const next = [...prev];
      if (editingIndex === fields.length) {
        next.push(editingField);
      } else if (editingIndex !== null) {
        next[editingIndex] = editingField;
      }
      return next;
    });
    setEditingIndex(null);
  };

  const removeField = (idx: number) => setFields(prev => prev.filter((_, i) => i !== idx));

  const moveField = (idx: number, dir: -1 | 1) => {
    setFields(prev => {
      const next = [...prev];
      const target = idx + dir;
      if (target < 0 || target >= next.length) return next;
      [next[idx], next[target]] = [next[target], next[idx]];
      return next;
    });
  };

  const TYPE_COLORS: Record<string, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
    text: 'default', number: 'info', select: 'warning', date: 'default', email: 'info', boolean: 'success',
  };

  return (
    <div className="p-8 space-y-6">
      {/* Top bar */}
      <div className="flex items-center justify-between">
        <button
          onClick={() => router.push('/forms')}
          className="flex items-center gap-2 text-slate-500 hover:text-slate-800 text-sm"
        >
          <ArrowLeft size={16} /> Back to Forms
        </button>
        <div className="flex gap-3">
          <button
            onClick={() => setShowAiPanel(v => !v)}
            className={`flex items-center gap-2 border text-sm font-medium px-4 py-2 rounded-lg transition-colors ${showAiPanel ? 'bg-purple-50 border-purple-300 text-purple-700' : 'border-slate-200 text-slate-700 hover:bg-slate-50'}`}
          >
            <Sparkles size={15} /> AI
          </button>
          <button
            onClick={() => setShowPreview(true)}
            className="flex items-center gap-2 border border-slate-200 text-slate-700 hover:bg-slate-50 text-sm font-medium px-4 py-2 rounded-lg"
          >
            <Eye size={15} /> Preview
          </button>
          <button
            onClick={() => { setSaveError(null); saveMutation.mutate(); }}
            disabled={saveMutation.isPending || !name}
            className="flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium px-4 py-2 rounded-lg disabled:opacity-50"
          >
            <Save size={15} /> {saveMutation.isPending ? 'Saving…' : 'Save'}
          </button>
        </div>
      </div>

      {saveError && (
        <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg px-4 py-3">{saveError}</div>
      )}

      <div className="flex gap-6">
        <div className={`flex-1 grid grid-cols-1 lg:grid-cols-3 gap-6 ${showAiPanel ? 'min-w-0' : ''}`}>
        {/* Left panel — metadata */}
        <div className="lg:col-span-1 space-y-4">
          <div className="bg-white border border-slate-200 rounded-xl p-5 space-y-4">
            <h2 className="font-semibold text-slate-900">Form Details</h2>

            <div className="space-y-1">
              <label className="block text-xs font-medium text-slate-700">Name <span className="text-red-500">*</span></label>
              <input
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
                placeholder="e.g. Customer Intake"
                value={name}
                onChange={e => setName(e.target.value)}
              />
            </div>

            <div className="space-y-1">
              <label className="block text-xs font-medium text-slate-700">Slug</label>
              <input
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-indigo-400"
                value={slug}
                onChange={e => { setSlugManual(true); setSlug(e.target.value); }}
              />
              {slug && (
                <p className="text-xs text-slate-400">
                  Node type: <span className="font-mono text-indigo-600">form.{slug}</span>
                </p>
              )}
            </div>

            <div className="space-y-1">
              <label className="block text-xs font-medium text-slate-700">Description</label>
              <textarea
                className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 resize-none"
                placeholder="What is this form for?"
                rows={3}
                value={description}
                onChange={e => setDescription(e.target.value)}
              />
            </div>
          </div>
        </div>

        {/* Version history — only shown for existing forms */}
        {!isNew && (
          <div className="lg:col-span-1 mt-0">
            <FormVersionHistory
              formId={id}
              onActivate={(v: FormVersionSummary) => {
                try {
                  const parsed = JSON.parse(v.fieldsJson);
                  setFields(parsed);
                } catch { /* ignore */ }
              }}
            />
          </div>
        )}

        {/* Right panel — fields */}
        <div className="lg:col-span-2 space-y-4">
          <div className="bg-white border border-slate-200 rounded-xl p-5 space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="font-semibold text-slate-900">Fields</h2>
              <button
                onClick={openNew}
                className="flex items-center gap-1.5 text-sm text-indigo-600 hover:text-indigo-800 font-medium"
              >
                <Plus size={14} /> Add Field
              </button>
            </div>

            {fields.length === 0 && (
              <p className="text-sm text-slate-400 text-center py-6">No fields yet. Click "Add Field" to get started.</p>
            )}

            <div className="space-y-2">
              {fields.map((field, idx) => (
                <div
                  key={idx}
                  className="flex items-center gap-3 px-4 py-3 rounded-lg border border-slate-100 hover:border-slate-200 bg-slate-50"
                >
                  <div className="flex flex-col gap-0.5">
                    <button onClick={() => moveField(idx, -1)} disabled={idx === 0} className="text-slate-400 hover:text-slate-700 disabled:opacity-30">
                      <ArrowUp size={12} />
                    </button>
                    <button onClick={() => moveField(idx, 1)} disabled={idx === fields.length - 1} className="text-slate-400 hover:text-slate-700 disabled:opacity-30">
                      <ArrowDown size={12} />
                    </button>
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-medium text-slate-800">{field.label}</span>
                      {field.required && <span className="text-red-500 text-xs">*</span>}
                    </div>
                    <span className="text-xs text-slate-400 font-mono">{field.key}</span>
                  </div>

                  <Badge variant={TYPE_COLORS[field.type] ?? 'default'}>{field.type}</Badge>

                  <button onClick={() => openEdit(idx)} className="text-slate-400 hover:text-indigo-600 transition-colors">
                    <Pencil size={14} />
                  </button>
                  <button onClick={() => removeField(idx)} className="text-slate-400 hover:text-red-600 transition-colors">
                    <Trash2 size={14} />
                  </button>
                </div>
              ))}
            </div>
          </div>
        </div>
        </div>
        {showAiPanel && (
          <FormAiAssistPanel
            formName={name}
            formDescription={description}
            getCurrentFieldsJson={() => JSON.stringify(fields)}
            onPreview={(suggested) => setFields(suggested)}
            onAccept={(suggested) => { setFields(suggested); setShowAiPanel(false); }}
            onClose={() => setShowAiPanel(false)}
          />
        )}
      </div>

      {/* Field editor modal */}
      {editingIndex !== null && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl p-6 w-full max-w-md shadow-xl space-y-4">
            <h2 className="font-semibold text-lg text-slate-900">
              {editingIndex === fields.length ? 'Add Field' : 'Edit Field'}
            </h2>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="block text-xs font-medium text-slate-700">Key <span className="text-red-500">*</span></label>
                <input
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-indigo-400"
                  placeholder="field_key"
                  value={editingField.key}
                  onChange={e => setEditingField(f => ({ ...f, key: e.target.value.replace(/[^a-z0-9_]/gi, '') }))}
                />
              </div>
              <div className="space-y-1">
                <label className="block text-xs font-medium text-slate-700">Label <span className="text-red-500">*</span></label>
                <input
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
                  placeholder="Display Label"
                  value={editingField.label}
                  onChange={e => setEditingField(f => ({ ...f, label: e.target.value }))}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="block text-xs font-medium text-slate-700">Type</label>
                <select
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
                  value={editingField.type}
                  onChange={e => setEditingField(f => ({ ...f, type: e.target.value as FormFieldDefinition['type'] }))}
                >
                  {TYPE_OPTIONS.map(t => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
              <div className="flex items-end pb-2">
                <label className="flex items-center gap-2 text-sm text-slate-700 cursor-pointer">
                  <input
                    type="checkbox"
                    className="rounded"
                    checked={editingField.required ?? false}
                    onChange={e => setEditingField(f => ({ ...f, required: e.target.checked }))}
                  />
                  Required
                </label>
              </div>
            </div>

            {(editingField.type === 'text' || editingField.type === 'number' || editingField.type === 'email') && (
              <div className="space-y-1">
                <label className="block text-xs font-medium text-slate-700">Placeholder</label>
                <input
                  className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
                  placeholder="Placeholder text…"
                  value={editingField.placeholder ?? ''}
                  onChange={e => setEditingField(f => ({ ...f, placeholder: e.target.value }))}
                />
              </div>
            )}

            {(editingField.type === 'text' || editingField.type === 'number' || editingField.type === 'email') && (
              <div className="space-y-3">
                <div className="space-y-1">
                  <label className="block text-xs font-medium text-slate-700">Regex validation</label>
                  <div className="flex gap-2">
                    <input
                      className="flex-1 border border-slate-200 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-indigo-400"
                      placeholder="^[A-Za-z]+$"
                      value={editingField.validationRegex ?? ''}
                      onChange={e => setEditingField(f => ({ ...f, validationRegex: e.target.value || undefined }))}
                    />
                    <button
                      type="button"
                      title="Test regex"
                      onClick={() => { setShowRegexTest(v => !v); setRegexTestValue(''); }}
                      className="flex items-center gap-1 px-3 py-2 text-xs rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50"
                    >
                      <FlaskConical size={13} /> Test
                    </button>
                  </div>
                  {showRegexTest && editingField.validationRegex && (
                    <div className="flex items-center gap-2 mt-1">
                      <input
                        className="flex-1 border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
                        placeholder="Enter sample value…"
                        value={regexTestValue}
                        onChange={e => setRegexTestValue(e.target.value)}
                      />
                      <span className="text-lg">
                        {regexTestValue === '' ? '—' : (() => { try { return new RegExp(editingField.validationRegex!).test(regexTestValue) ? '✅' : '❌'; } catch { return '⚠️'; } })()}
                      </span>
                    </div>
                  )}
                </div>
                <div className="space-y-1">
                  <label className="block text-xs font-medium text-slate-700">Validation message</label>
                  <input
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
                    placeholder="Invalid format"
                    value={editingField.validationMessage ?? ''}
                    onChange={e => setEditingField(f => ({ ...f, validationMessage: e.target.value || undefined }))}
                  />
                </div>
              </div>
            )}

            {editingField.type === 'select' && (
              <div className="space-y-3">
                {/* Toggle: static options vs dynamic from output key */}
                <div className="flex items-center gap-3">
                  <label className="flex items-center gap-2 text-xs font-medium text-slate-700 cursor-pointer">
                    <input type="radio" name="optionsMode" checked={!editingField.optionsFrom}
                      onChange={() => setEditingField(f => ({ ...f, optionsFrom: undefined }))}
                    /> Static options
                  </label>
                  <label className="flex items-center gap-2 text-xs font-medium text-slate-700 cursor-pointer">
                    <input type="radio" name="optionsMode" checked={!!editingField.optionsFrom}
                      onChange={() => setEditingField(f => ({ ...f, optionsFrom: '', options: [] }))}
                    /> Load from workflow output
                  </label>
                </div>

                {!editingField.optionsFrom ? (
                  <div className="space-y-1">
                    <label className="block text-xs font-medium text-slate-700">Options <span className="text-slate-400">(comma-separated)</span></label>
                    <input
                      className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
                      placeholder="Option A, Option B, Option C"
                      value={(editingField.options ?? []).join(', ')}
                      onChange={e => setEditingField(f => ({
                        ...f,
                        options: e.target.value.split(',').map(s => s.trim()).filter(Boolean),
                      }))}
                    />
                  </div>
                ) : (
                  <div className="space-y-1">
                    <label className="block text-xs font-medium text-slate-700">Output key <span className="text-red-500">*</span></label>
                    <input
                      className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-indigo-400"
                      placeholder="e.g. rows  or  categories"
                      value={editingField.optionsFrom ?? ''}
                      onChange={e => setEditingField(f => ({ ...f, optionsFrom: e.target.value }))}
                    />
                    <p className="text-xs text-slate-400">The output key from a previous node whose value is a JSON array. E.g. a <code className="bg-slate-100 px-1 rounded">data.db-query</code> node outputting <code className="bg-slate-100 px-1 rounded">rows=["Food","Transport"]</code>.</p>
                  </div>
                )}
              </div>
            )}

            <div className="flex gap-3 justify-end pt-1">
              <button
                onClick={() => setEditingIndex(null)}
                className="px-4 py-2 text-sm rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50"
              >
                Cancel
              </button>
              <button
                onClick={saveField}
                disabled={!editingField.key || !editingField.label}
                className="px-4 py-2 text-sm rounded-lg bg-indigo-600 text-white hover:bg-indigo-700 disabled:opacity-50"
              >
                {editingIndex === fields.length ? 'Add' : 'Update'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Preview modal */}
      {showPreview && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 overflow-y-auto py-12">
          <div className="bg-white rounded-xl p-8 w-full max-w-lg shadow-xl space-y-6">
            <div className="flex items-center justify-between">
              <h2 className="font-semibold text-lg text-slate-900">Form Preview</h2>
              <button onClick={() => setShowPreview(false)} className="text-slate-400 hover:text-slate-700 text-xl leading-none">&times;</button>
            </div>
            <FormRenderer
              name={name || 'Untitled Form'}
              description={description}
              fields={fields}
              preview
            />
          </div>
        </div>
      )}
    </div>
  );
}
