'use client';

/**
 * Retrieves the stored JWT auth token from localStorage.
 * Returns null if called server-side or if no token is stored.
 *
 * @returns The JWT token string, or null if not authenticated
 */
export function getToken(): string | null {
  return typeof window !== 'undefined' ? localStorage.getItem('OrchestFlowAI_token') : null;
}

/**
 * Persists the JWT auth token to localStorage after a successful login.
 * Dispatches a custom event so AuthProvider re-reads state without a full page reload.
 *
 * @param token - The JWT token returned by the auth API
 */
export function setToken(token: string) {
  localStorage.setItem('OrchestFlowAI_token', token);
  window.dispatchEvent(new Event('orchest:auth-changed'));
}

/**
 * Removes the JWT auth token from localStorage, effectively logging the user out.
 * Dispatches a custom event so AuthProvider clears its state immediately.
 */
export function clearToken() {
  localStorage.removeItem('OrchestFlowAI_token');
  window.dispatchEvent(new Event('orchest:auth-changed'));
}

/**
 * Checks whether the stored JWT token has expired by reading the `exp` claim.
 * Returns true when there is no token, the token is malformed, or it is past its expiry time.
 *
 * @returns True if the token is absent or expired, false if it is still valid
 */
export function isTokenExpired(): boolean {
  const token = getToken();
  if (!token) return true;
  const payload = decodeJwt(token);
  if (!payload) return true;
  const exp = payload['exp'];
  if (typeof exp !== 'number') return false; // no exp claim → treat as non-expiring
  return Date.now() / 1000 > exp;
}

/**
 * Checks whether the user currently has a stored, non-expired auth token.
 *
 * @returns True if a valid, unexpired token is present in localStorage, false otherwise
 */
export function isAuthenticated(): boolean {
  return !!getToken() && !isTokenExpired();
}

/**
 * Known user roles in the system, ordered from lowest to highest privilege.
 */
export type UserRole = 'Viewer' | 'Editor' | 'Admin' | 'Approver';

/**
 * Decodes a JWT token payload without verifying the signature.
 * Used client-side to read claims already validated by the server.
 *
 * @param token - The JWT token string
 * @returns Decoded payload object, or null if the token is malformed
 */
export function decodeJwt(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    // Base64url decode — pad to a multiple of 4 chars
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64 + '=='.slice(0, (4 - (base64.length % 4)) % 4);
    return JSON.parse(atob(padded)) as Record<string, unknown>;
  } catch {
    return null;
  }
}

/**
 * Extracts the tenant id from the stored JWT token.
 * Returns null when not authenticated or when the claim is absent.
 */
export function getTenantId(): string | null {
  const token = getToken();
  if (!token) return null;
  const payload = decodeJwt(token);
  return (payload?.['tenant_id'] as string) ?? null;
}

/**
 * Extracts the user role from the stored JWT token.
 * Returns null when not authenticated or when the role claim is absent.
 *
 * @returns The user's role string, or null if unavailable
 */
export function getRoleFromToken(): UserRole | null {
  const token = getToken();
  if (!token) return null;
  const payload = decodeJwt(token);
  if (!payload) return null;
  // ASP.NET Core serialises ClaimTypes.Role as the long URN or as 'role'
  const role =
    (payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] as string) ??
    (payload['role'] as string) ??
    null;
  return (role as UserRole) ?? null;
}
