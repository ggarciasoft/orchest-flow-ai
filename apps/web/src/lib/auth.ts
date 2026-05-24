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
 *
 * @param token - The JWT token returned by the auth API
 */
export function setToken(token: string) {
  localStorage.setItem('OrchestFlowAI_token', token);
}

/**
 * Removes the JWT auth token from localStorage, effectively logging the user out.
 */
export function clearToken() {
  localStorage.removeItem('OrchestFlowAI_token');
}

/**
 * Checks whether the user currently has a stored auth token.
 * Does not validate token expiry — use server-side validation for that.
 *
 * @returns True if a token is present in localStorage, false otherwise
 */
export function isAuthenticated(): boolean {
  return !!getToken();
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
