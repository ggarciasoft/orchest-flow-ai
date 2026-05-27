import { isTokenExpired } from './auth';

/** Base URL for all API requests. Configurable via NEXT_PUBLIC_API_BASE_URL env var. */
const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5080';

/**
 * Clears the stored token and redirects the browser to the login page.
 * Safe to call on server side (no-op when window is unavailable).
 */
function redirectToLogin(): void {
  if (typeof window === 'undefined') return;
  localStorage.removeItem('OrchestFlowAI_token');
  window.location.replace('/login');
}

/**
 * Core HTTP client for all OrchestFlowAI API requests.
 * Automatically attaches the JWT auth token from localStorage and parses JSON responses.
 * Throws an Error with the API's detail/title message on non-2xx responses.
 *
 * @param path - API path relative to API_BASE (e.g. "/api/workflows")
 * @param options - Optional fetch RequestInit options (method, body, headers)
 * @returns Parsed JSON response typed as T
 * @throws Error with message from API response body or "HTTP {status}"
 */
async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  // Read token from localStorage — null on server-side rendering
  const token = typeof window !== 'undefined' ? localStorage.getItem('OrchestFlowAI_token') : null;

  // If a token exists but is already expired, redirect immediately without making a request
  if (typeof window !== 'undefined' && token && isTokenExpired()) {
    redirectToLogin();
    // Return a promise that never resolves — the page will redirect before any caller handles it
    return new Promise<T>(() => {});
  }

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    },
  });
  if (!res.ok) {
    // 401 Unauthorized — token was rejected by the server; clear it and redirect to login
    if (res.status === 401) {
      redirectToLogin();
      return new Promise<T>(() => {});
    }
    // Extract structured error message from API response if available
    const err = await res.json().catch(() => ({}));
    throw new Error((err as Record<string, string>).detail ?? (err as Record<string, string>).title ?? `HTTP ${res.status}`);
  }
  // 204 No Content or empty body — return undefined rather than attempting to parse
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
}

/** All OrchestFlowAI API methods organized by resource domain. */
/**
 * Node configuration presets — reusable named config sets.
 */
export interface PresetResponse {
  id: string;
  name: string;
  nodeType: string;
  configJson: string;
  createdAt: string;
}

export const api = {
  /** Authentication endpoints for login and current user info. */
  auth: {
    /** Authenticates a user and returns a JWT token on success. */
    login: (email: string, password: string) =>
      apiFetch<{ token: string; user: User }>('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
    /** Returns the currently authenticated user's profile. */
    me: () => apiFetch<User>('/api/auth/me'),
  },
  /** Workflow definition CRUD and execution endpoints. */
  workflows: {
    /** Lists workflows for the current tenant with optional search and pagination. */
    list: (params?: { search?: string; page?: number; pageSize?: number }) => {
      const q = new URLSearchParams();
      if (params?.search) q.set('search', params.search);
      if (params?.page) q.set('page', String(params.page));
      if (params?.pageSize) q.set('pageSize', String(params.pageSize));
      return apiFetch<PagedResponse<Workflow>>(`/api/workflows?${q}`);
    },
    /** Fetches a single workflow by id. */
    get: (id: string) => apiFetch<Workflow>(`/api/workflows/${id}`),
    /** Creates a new workflow with an initial empty version. */
    create: (data: { name: string; description: string; definition: object }) =>
      apiFetch<Workflow>('/api/workflows', { method: 'POST', body: JSON.stringify(data) }),
    /** Enqueues a workflow execution with optional input payload. */
    execute: (id: string, input: Record<string, unknown>) =>
      apiFetch<WorkflowExecution>(`/api/workflows/${id}/execute`, { method: 'POST', body: JSON.stringify({ input }) }),
    /** Validates the active workflow version's node graph without executing it. */
    validate: (id: string) => apiFetch<ValidationResult>(`/api/workflows/${id}/validate`, { method: 'POST' }),
    /** Fetches the active version's definition JSON for loading into the designer. */
    getActiveVersion: (id: string) =>
      apiFetch<{ versionId: string; versionNumber: number; definitionJson: string }>(`/api/workflows/${id}/versions/active`),
    /** Saves a new version of a workflow definition and activates it. */
    saveVersion: (id: string, definition: object) =>
      apiFetch<{ id: string; versionNumber: number }>(`/api/workflows/${id}/versions`, { method: 'POST', body: JSON.stringify({ definition }) }),
    /** Activates a specific workflow version by id. */
    activateVersion: (workflowId: string, versionId: string) =>
      apiFetch<void>(`/api/workflows/${workflowId}/versions/${versionId}/activate`, { method: 'POST' }),
    /** Lists all versions of a workflow ordered newest first. */
    listVersions: (workflowId: string) =>
      apiFetch<WorkflowVersionSummary[]>(`/api/workflows/${workflowId}/versions`),
    /** Fetches the full definition JSON for a specific version. */
    getVersion: (workflowId: string, versionId: string) =>
      apiFetch<WorkflowVersionDetail>(`/api/workflows/${workflowId}/versions/${versionId}`),
  },
  /** Workflow execution history and node timeline endpoints. */
  executions: {
    /** Lists executions with optional workflow, status, and page filters. */
    list: (params?: { workflowId?: string; status?: string; page?: number }) => {
      const q = new URLSearchParams();
      if (params?.workflowId) q.set('workflowId', params.workflowId);
      if (params?.status) q.set('status', params.status);
      if (params?.page) q.set('page', String(params.page));
      return apiFetch<PagedResponse<WorkflowExecution>>(`/api/executions?${q}`);
    },
    /** Fetches a single execution by id. */
    get: (id: string) => apiFetch<WorkflowExecution>(`/api/executions/${id}`),
    /** Fetches the ordered node execution timeline for an execution. */
    timeline: (id: string) => apiFetch<ExecutionTimeline>(`/api/executions/${id}/timeline`),
  },
  /** Human approval inbox endpoints. */
  approvals: {
    /** Lists approval requests filtered by status (e.g. "Pending"). */
    list: (status?: string) => {
      const q = status ? `?status=${status}` : '';
      return apiFetch<PagedResponse<ApprovalRequest>>(`/api/approvals${q}`);
    },
    /** Fetches a single approval request by id. */
    get: (id: string) => apiFetch<ApprovalRequest>(`/api/approvals/${id}`),
    /** Approves a pending approval request with an optional comment. */
    approve: (id: string, comment?: string) =>
      apiFetch<ApprovalRequest>(`/api/approvals/${id}/approve`, { method: 'POST', body: JSON.stringify({ comment }) }),
    /** Rejects a pending approval request with an optional comment. */
    reject: (id: string, comment?: string) =>
      apiFetch<ApprovalRequest>(`/api/approvals/${id}/reject`, { method: 'POST', body: JSON.stringify({ comment }) }),
  },
  /** Document upload and retrieval endpoints. */
  documents: {
    /**
     * Uploads a file using multipart/form-data.
     * Uses a raw fetch (not apiFetch) because Content-Type must not be set manually for FormData.
     */
    upload: (file: File) => {
      const form = new FormData();
      form.append('file', file);
      const token = typeof window !== 'undefined' ? localStorage.getItem('OrchestFlowAI_token') : null;
      return fetch(`${API_BASE}/api/documents/upload`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: form,
      }).then(r => r.json()) as Promise<DocumentMeta>;
    },
    /** Fetches document metadata by id. */
    get: (id: string) => apiFetch<DocumentMeta>(`/api/documents/${id}`),
  },
  /** Node catalog endpoint — returns all registered node descriptors. */
  nodes: {
    /** Returns the full catalog of available node types from the registry. */
    catalog: () => apiFetch<{ nodes: NodeDescriptor[] }>('/api/nodes/catalog'),
    /** Returns all available LLM models from registered providers. */
    models: () => apiFetch<{ models: { value: string; label: string }[] }>('/api/nodes/models'),
  },
  /** Tenant management and invite endpoints. */
  tenants: {
    /**
     * Creates a new tenant workspace.
     * @param name - The workspace name.
     */
    create: (name: string) =>
      apiFetch<TenantResponse>('/api/tenants', { method: 'POST', body: JSON.stringify({ name }) }),
    /**
     * Invites a user to the given tenant by email.
     * @param tenantId - The tenant id to invite into.
     * @param email - The invitee's email address.
     * @param role - The role to assign on acceptance.
     */
    invite: (tenantId: string, email: string, role: string) =>
      apiFetch<TenantInviteResponse>(`/api/tenants/${tenantId}/invite`, {
        method: 'POST',
        body: JSON.stringify({ email, role }),
      }),
    /**
     * Accepts a tenant invite and creates the user account.
     * @param tenantId - The tenant id.
     * @param token - The invite token.
     * @param password - The desired password for the new account.
     */
    acceptInvite: (tenantId: string, token: string, password: string) =>
      apiFetch<{ message: string }>(`/api/tenants/${tenantId}/invite/accept`, {
        method: 'POST',
        body: JSON.stringify({ token, password }),
      }),
  },
  /** Node configuration presets — reusable named config sets. */
  presets: {
    /** Lists all presets, optionally filtered by node type. */
    list: (nodeType?: string) =>
      apiFetch<PresetResponse[]>(`/api/presets${nodeType ? `?nodeType=${nodeType}` : ''}`),
    /** Gets a single preset by id. */
    get: (id: string) => apiFetch<PresetResponse>(`/api/presets/${id}`),
    /** Creates a new preset. */
    create: (data: { name: string; nodeType: string; configJson: string }) =>
      apiFetch<PresetResponse>('/api/presets', { method: 'POST', body: JSON.stringify(data) }),
    /** Updates an existing preset. */
    update: (id: string, data: { name: string; configJson: string }) =>
      apiFetch<PresetResponse>(`/api/presets/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    /** Deletes a preset. */
    delete: (id: string) => apiFetch<void>(`/api/presets/${id}`, { method: 'DELETE' }),
  },

  /** Gmail credential management endpoints. */
  gmail: {
    /** Starts the Gmail OAuth2 flow. Returns a URL to redirect the browser to. */
    authStartUrl: (params: { name: string; clientId?: string; clientSecret?: string }) => {
      const q = new URLSearchParams({ name: params.name });
      if (params.clientId) q.set('clientId', params.clientId);
      if (params.clientSecret) q.set('clientSecret', params.clientSecret);
      return `${API_BASE}/api/gmail/auth/start?${q}`;
    },
    /** Lists saved Gmail credentials for the tenant (names + emails only). */
    list: () => apiFetch<GmailCredentialSummary[]>('/api/gmail/credentials'),
    /** Deletes a Gmail credential by id. */
    delete: (id: string) => apiFetch<void>(`/api/gmail/credentials/${id}`, { method: 'DELETE' }),
  },
  /** Platform settings endpoints. */
  settings: {
    /** Returns current platform settings (API keys masked). */
    get: () => apiFetch<Record<string, string | null>>('/api/settings'),
    /** Updates platform settings. Empty string values are ignored (keeps existing). */
    update: (updates: Record<string, string>) =>
      apiFetch<void>('/api/settings', { method: 'PUT', body: JSON.stringify(updates) }),
    /** Tests the OpenAI connection. */
    testOpenAI: () =>
      apiFetch<{ success: boolean; message: string }>('/api/settings/test/openai', { method: 'POST' }),
  },
  /** Secret vault endpoints — encrypted named values for use in node config as {{secret:name}}. */
  secrets: {
    /** Lists secrets for the tenant (names only, never values). */
    list: () => apiFetch<SecretSummary[]>('/api/secrets'),
    /** Creates a new secret. */
    create: (name: string, value: string) =>
      apiFetch<{ id: string; name: string; createdAt: string }>('/api/secrets', { method: 'POST', body: JSON.stringify({ name, value }) }),
    /** Updates a secret's value or name. */
    update: (id: string, data: { name?: string; value?: string }) =>
      apiFetch<void>(`/api/secrets/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
    /** Deletes a secret. */
    delete: (id: string) => apiFetch<void>(`/api/secrets/${id}`, { method: 'DELETE' }),
  },
};

// ---- Type Definitions ----

/** Authenticated user profile. */
export interface User { id: string; email: string; displayName: string; role: string; }

/** Workflow definition metadata. */
export interface Workflow { id: string; name: string; description: string; activeVersion?: number; createdAt: string; updatedAt: string; }
export interface WorkflowVersionSummary { id: string; versionNumber: number; isActive: boolean; createdBy: string | null; createdAt: string; }
export interface WorkflowVersionDetail extends WorkflowVersionSummary { definitionJson: string; }

/** A single workflow execution instance. */
export interface WorkflowExecution { id: string; workflowId: string; workflowVersionId: string; status: string; startedAt: string; completedAt?: string; correlationId: string; errorMessage?: string; workflowName?: string; versionNumber?: number; }

/** Execution record for a single node within a workflow run. */
export interface NodeExecution { id: string; workflowExecutionId: string; nodeId: string; nodeType: string; status: string; startedAt?: string; completedAt?: string; inputJson?: string; outputJson?: string; errorMessage?: string; retryCount: number; step: number; }

/** Ordered list of node executions for a workflow run. */
export interface ExecutionTimeline { executionId: string; nodes: NodeExecution[]; }

/** A pending or resolved human approval request. */
export interface ApprovalRequest { id: string; workflowExecutionId: string; nodeExecutionId: string; status: string; payloadJson: string; requestedAt: string; respondedAt?: string; decision?: string; comment?: string; }

/** Metadata for an uploaded document. */
export interface DocumentMeta { id: string; filename: string; mimeType: string; sizeBytes: number; sha256: string; createdAt: string; }

/** Generic paginated response wrapper. */
export interface PagedResponse<T> { items: T[]; page: number; pageSize: number; total: number; }

/** Describes a node type available in the workflow designer palette. */
export interface NodeDescriptor { type: string; displayName: string; description: string; category: string; version: string; iconKey?: string; inputs: NodePort[]; outputs: NodePort[]; configuration: NodePort[]; }

/** A single input, output, or configuration port on a node descriptor. */
export interface NodePort { key: string; displayName: string; description: string; type: string; required?: boolean; defaultValue?: unknown; allowedValues?: string[]; optionsSource?: string; optionDescriptions?: Record<string, string>; }

/** Result of a workflow definition validation check. */
export interface ValidationResult { isValid: boolean; errors: { nodeId: string; message: string; }[]; }

/** Tenant workspace metadata. */
export interface TenantResponse { id: string; name: string; createdAt: string; }

/** Tenant invite response — includes the token for the MVP invite flow. */
export interface TenantInviteResponse { id: string; tenantId: string; email: string; role: string; token: string; expiresAt: string; }

/** Summary of a saved Gmail credential (no secrets). */
/** Summary of a saved secret (name only, never the value). */
export interface SecretSummary { id: string; name: string; createdAt: string; updatedAt: string; }

export interface GmailCredentialSummary { id: string; name: string; email: string | null; createdAt: string; updatedAt: string; }

