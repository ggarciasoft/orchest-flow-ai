'use client';
import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useMutation } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { ArrowLeft, GitBranch } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Button } from '@/components/ui';

export default function NewWorkflowPage() {
  const router = useRouter();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');

  const create = useMutation({
    mutationFn: () =>
      api.workflows.create({
        name: name.trim(),
        description: description.trim(),
        definition: { nodes: [], edges: [] },
      }),
    onSuccess: (workflow) => {
      router.push(`/workflows/${workflow.id}/designer`);
    },
    onError: (err: Error) => {
      setError(err.message ?? 'Failed to create workflow');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) { setError('Workflow name is required'); return; }
    setError('');
    create.mutate();
  };

  return (
    <div className="max-w-2xl mx-auto">
      <Link href="/workflows" className="flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700 mb-4">
        <ArrowLeft size={16} /> Back to Workflows
      </Link>
      <PageHeader
        title="New Workflow"
        subtitle="Create a new AI workflow definition"
      />

      <div className="bg-white border border-slate-200 rounded-xl p-6 max-w-lg">
        <form onSubmit={handleSubmit} className="space-y-5">
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-slate-700 mb-1.5">
              Workflow Name <span className="text-red-500">*</span>
            </label>
            <input
              id="name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Contract Risk Analysis"
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              autoFocus
            />
          </div>

          <div>
            <label htmlFor="description" className="block text-sm font-medium text-slate-700 mb-1.5">
              Description <span className="text-slate-400 font-normal">(optional)</span>
            </label>
            <textarea
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe what this workflow does..."
              rows={3}
              className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white placeholder:text-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent resize-none"
            />
          </div>

          {error && (
            <p className="text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg p-3">{error}</p>
          )}

          <div className="flex items-center gap-3 pt-2">
            <Button
              type="submit"
              disabled={create.isPending}
            >
              {create.isPending ? 'Creating...' : 'Create & Open Designer'}
            </Button>
            <Link href="/workflows">
              <Button type="button" variant="ghost">
                Cancel
              </Button>
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
}
