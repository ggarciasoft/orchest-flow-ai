const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5080';

async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const token = typeof window !== 'undefined' ? localStorage.getItem('orchestai_token') : null;
  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    },
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error((err as Record<string, string>).detail ?? (err as Record<string, string>).title ?? `HTTP ${res.status}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const api = {
  auth: {
    login: (email: string, password: string) =>
      apiFetch<{ token: string; user: User }>('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
    me: () => apiFetch<User>('/api/auth/me'),
  },
  workflows: {
    list: (params?: { search?: string; page?: number; pageSize?: number }) => {
      const q = new URLSearchParams();
      if (params?.search) q.set('search', params.search);
      if (params?.page) q.set('page', String(params.page));
      if (params?.pageSize) q.set('pageSize', String(params.pageSize));
      return apiFetch<PagedResponse<Workflow>>(`/api/workflows?${q}`);
    },
    get: (id: string) => apiFetch<Workflow>(`/api/workflows/${id}`),
    create: (data: { name: string; description: string; definition: object }) =>
      apiFetch<Workflow>('/api/workflows', { method: 'POST', body: JSON.stringify(data) }),
    execute: (id: string, input: Record<string, unknown>) =>
      apiFetch<WorkflowExecution>(`/api/workflows/${id}/execute`, { method: 'POST', body: JSON.stringify({ input }) }),
    validate: (id: string) => apiFetch<ValidationResult>(`/api/workflows/${id}/validate`, { method: 'POST' }),
  },
  executions: {
    list: (params?: { workflowId?: string; status?: string; page?: number }) => {
      const q = new URLSearchParams();
      if (params?.workflowId) q.set('workflowId', params.workflowId);
      if (params?.status) q.set('status', params.status);
      if (params?.page) q.set('page', String(params.page));
      return apiFetch<PagedResponse<WorkflowExecution>>(`/api/executions?${q}`);
    },
    get: (id: string) => apiFetch<WorkflowExecution>(`/api/executions/${id}`),
    timeline: (id: string) => apiFetch<ExecutionTimeline>(`/api/executions/${id}/timeline`),
  },
  approvals: {
    list: (status?: string) => {
      const q = status ? `?status=${status}` : '';
      return apiFetch<PagedResponse<ApprovalRequest>>(`/api/approvals${q}`);
    },
    get: (id: string) => apiFetch<ApprovalRequest>(`/api/approvals/${id}`),
    approve: (id: string, comment?: string) =>
      apiFetch<ApprovalRequest>(`/api/approvals/${id}/approve`, { method: 'POST', body: JSON.stringify({ comment }) }),
    reject: (id: string, comment?: string) =>
      apiFetch<ApprovalRequest>(`/api/approvals/${id}/reject`, { method: 'POST', body: JSON.stringify({ comment }) }),
  },
  documents: {
    upload: (file: File) => {
      const form = new FormData();
      form.append('file', file);
      const token = typeof window !== 'undefined' ? localStorage.getItem('orchestai_token') : null;
      return fetch(`${API_BASE}/api/documents/upload`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: form,
      }).then(r => r.json()) as Promise<DocumentMeta>;
    },
    get: (id: string) => apiFetch<DocumentMeta>(`/api/documents/${id}`),
  },
  nodes: {
    catalog: () => apiFetch<{ nodes: NodeDescriptor[] }>('/api/nodes/catalog'),
  },
};

export interface User { id: string; email: string; displayName: string; role: string; }
export interface Workflow { id: string; name: string; description: string; activeVersion?: number; createdAt: string; updatedAt: string; }
export interface WorkflowExecution { id: string; workflowId: string; workflowVersionId: string; status: string; startedAt: string; completedAt?: string; correlationId: string; errorMessage?: string; }
export interface NodeExecution { id: string; workflowExecutionId: string; nodeId: string; nodeType: string; status: string; startedAt?: string; completedAt?: string; inputJson?: string; outputJson?: string; errorMessage?: string; retryCount: number; step: number; }
export interface ExecutionTimeline { executionId: string; nodes: NodeExecution[]; }
export interface ApprovalRequest { id: string; workflowExecutionId: string; nodeExecutionId: string; status: string; payloadJson: string; requestedAt: string; respondedAt?: string; decision?: string; comment?: string; }
export interface DocumentMeta { id: string; filename: string; mimeType: string; sizeBytes: number; sha256: string; createdAt: string; }
export interface PagedResponse<T> { items: T[]; page: number; pageSize: number; total: number; }
export interface NodeDescriptor { type: string; displayName: string; description: string; category: string; version: string; iconKey?: string; inputs: NodePort[]; outputs: NodePort[]; configuration: NodePort[]; }
export interface NodePort { key: string; displayName: string; description: string; type: string; required?: boolean; defaultValue?: unknown; allowedValues?: string[]; }
export interface ValidationResult { isValid: boolean; errors: { nodeId: string; message: string; }[]; }
