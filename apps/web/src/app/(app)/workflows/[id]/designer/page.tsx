'use client';
import { useParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { WorkflowDesigner } from '@/components/designer/WorkflowDesigner';

/**
 * DesignerPage — loads workflow metadata, node catalog, and the active version
 * definition, then renders the full WorkflowDesigner canvas.
 * Shows a loading spinner while data is being fetched.
 */
export default function DesignerPage() {
  const { id } = useParams<{ id: string }>();

  const { data: workflow, isLoading: workflowLoading, error: workflowError } = useQuery({
    queryKey: ['workflow', id],
    queryFn: () => api.workflows.get(id),
    retry: false,
  });

  const { data: catalog, isLoading: catalogLoading } = useQuery({
    queryKey: ['nodes-catalog'],
    queryFn: () => api.nodes.catalog(),
  });

  // Fetch the active version definition to hydrate the canvas
  const { data: activeVersion } = useQuery({
    queryKey: ['workflow-version', id],
    queryFn: () => api.workflows.getActiveVersion(id),
    // Don't fail the whole page if no version exists yet
    retry: false,
  });

  if (workflowLoading || catalogLoading) {
    return (
      <div className="flex items-center justify-center h-screen text-gray-400">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-3" />
          <p>Loading designer…</p>
        </div>
      </div>
    );
  }

  if (workflowError || !workflow) {
    return (
      <div className="flex items-center justify-center h-screen bg-slate-50">
        <div className="text-center space-y-4 max-w-sm">
          <div className="text-5xl">🔍</div>
          <h2 className="text-xl font-semibold text-slate-800">Workflow not found</h2>
          <p className="text-sm text-slate-500">
            This workflow doesn't exist or you don't have access to it.
          </p>
          <a
            href="/workflows"
            className="inline-flex items-center gap-2 px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-lg hover:bg-indigo-700 transition-colors"
          >
            ← Back to Workflows
          </a>
        </div>
      </div>
    );
  }

  if (!catalog) {
    return (
      <div className="flex items-center justify-center h-screen text-gray-400">
        <p>Failed to load node catalog.</p>
      </div>
    );
  }

  return (
    <WorkflowDesigner
      workflow={workflow}
      nodeCatalog={catalog.nodes}
      initialDefinitionJson={activeVersion?.definitionJson}
      activeVersionNumber={workflow.activeVersion ?? activeVersion?.versionNumber}
    />
  );
}
