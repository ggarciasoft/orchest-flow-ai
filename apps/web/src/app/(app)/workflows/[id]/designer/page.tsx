'use client';
import { useParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { WorkflowDesigner } from '@/components/designer/WorkflowDesigner';

export default function DesignerPage() {
  const { id } = useParams<{ id: string }>();
  const { data: workflow } = useQuery({ queryKey: ['workflow', id], queryFn: () => api.workflows.get(id) });
  const { data: catalog } = useQuery({ queryKey: ['nodes-catalog'], queryFn: () => api.nodes.catalog() });

  if (!workflow || !catalog) {
    return (
      <div className="flex items-center justify-center h-screen text-gray-400">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto mb-3" />
          <p>Loading designer…</p>
        </div>
      </div>
    );
  }
  return <WorkflowDesigner workflow={workflow} nodeCatalog={catalog.nodes} />;
}
