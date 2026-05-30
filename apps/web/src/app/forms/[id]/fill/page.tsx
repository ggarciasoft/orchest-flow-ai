'use client';
import { useState, use } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { useSearchParams } from 'next/navigation';
import { api, WorkflowForm } from '@/lib/api';
import FormRenderer from '@/app/(app)/forms/_components/FormRenderer';
import { CheckCircle } from 'lucide-react';

/**
 * FormFillPage — standalone page (no app nav) for filling a custom form
 * during a paused workflow execution.
 *
 * URL: /forms/[id]/fill?executionId=...&nodeExecutionId=...
 */
export default function FormFillPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const searchParams = useSearchParams();
  const executionId = searchParams.get('executionId') ?? '';
  const nodeExecutionId = searchParams.get('nodeExecutionId') ?? '';

  const [values, setValues] = useState<Record<string, unknown>>({});
  const [submitted, setSubmitted] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const { data: form, isLoading, error } = useQuery<WorkflowForm>({
    queryKey: ['forms', id, 'fill', executionId, nodeExecutionId],
    queryFn: () => api.forms.getFillSchema(id, executionId, nodeExecutionId),
    enabled: !!id && !!executionId && !!nodeExecutionId,
  });

  const submitMutation = useMutation({
    mutationFn: () =>
      api.forms.submit(id, {
        workflowExecutionId: executionId,
        nodeExecutionId,
        values,
      }),
    onSuccess: () => setSubmitted(true),
    onError: (err: Error) => setSubmitError(err.message),
  });

  const handleChange = (key: string, value: unknown) => {
    setValues(prev => ({ ...prev, [key]: value }));
    // Clear per-field error on change
    setFieldErrors(prev => { const next = { ...prev }; delete next[key]; return next; });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError(null);
    if (form) {
      const missing = form.fields.filter(f => f.required && !values[f.key] && values[f.key] !== false);
      if (missing.length > 0) {
        setSubmitError(`Please fill in required fields: ${missing.map(f => f.label).join(', ')}`);
        return;
      }
      // Client-side regex validation (skip for file/boolean fields)
      const newFieldErrors: Record<string, string> = {};
      for (const field of form.fields) {
        if (!field.validationRegex) continue;
        if (field.type === 'file' || field.type === 'boolean') continue;
        const val = values[field.key];
        if (typeof val !== 'string' || val === '') continue;
        try {
          if (!new RegExp(field.validationRegex).test(val)) {
            newFieldErrors[field.key] = field.validationMessage ?? 'Invalid format';
          }
        } catch {
          // ignore malformed regex on client
        }
      }
      if (Object.keys(newFieldErrors).length > 0) {
        setFieldErrors(newFieldErrors);
        return;
      }
    }
    submitMutation.mutate();
  };

  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center py-12 px-4">
      <div className="w-full max-w-lg bg-white border border-slate-200 rounded-2xl shadow-sm p-8">
        {submitted ? (
          <div className="text-center space-y-3 py-8">
            <CheckCircle size={48} className="text-green-500 mx-auto" />
            <h2 className="text-xl font-semibold text-slate-900">Form submitted successfully.</h2>
            <p className="text-slate-500 text-sm">You can close this page.</p>
          </div>
        ) : isLoading ? (
          <div className="space-y-4 animate-pulse">
            <div className="h-7 bg-slate-200 rounded w-1/2" />
            <div className="h-4 bg-slate-100 rounded w-3/4" />
            {[1, 2, 3].map(i => <div key={i} className="h-10 bg-slate-100 rounded" />)}
          </div>
        ) : error ? (
          <div className="text-center py-8 space-y-2">
            <p className="text-red-600 font-medium">Failed to load form</p>
            <p className="text-sm text-slate-500">{(error as Error).message}</p>
          </div>
        ) : !executionId || !nodeExecutionId ? (
          <div className="text-center py-8">
            <p className="text-slate-500 text-sm">Missing required URL parameters: <code>executionId</code> and <code>nodeExecutionId</code></p>
          </div>
        ) : form ? (
          <form onSubmit={handleSubmit} className="space-y-6">
            <FormRenderer
              name={form.name}
              description={form.description}
              fields={form.fields}
              values={values}
              onChange={handleChange}
              fieldErrors={fieldErrors}
            />

            {submitError && (
              <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg px-4 py-3">
                {submitError}
              </div>
            )}

            <button
              type="submit"
              disabled={submitMutation.isPending}
              className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2.5 rounded-lg text-sm disabled:opacity-50 transition-colors"
            >
              {submitMutation.isPending ? 'Submitting…' : 'Submit'}
            </button>
          </form>
        ) : null}
      </div>
    </div>
  );
}
