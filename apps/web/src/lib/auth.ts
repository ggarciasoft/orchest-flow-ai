'use client';

/**
 * Retrieves the stored JWT auth token from localStorage.
 * Returns null if called server-side or if no token is stored.
 *
 * @returns The JWT token string, or null if not authenticated
 */
export function getToken(): string | null {
  return typeof window !== 'undefined' ? localStorage.getItem('orchestai_token') : null;
}

/**
 * Persists the JWT auth token to localStorage after a successful login.
 *
 * @param token - The JWT token returned by the auth API
 */
export function setToken(token: string) {
  localStorage.setItem('orchestai_token', token);
}

/**
 * Removes the JWT auth token from localStorage, effectively logging the user out.
 */
export function clearToken() {
  localStorage.removeItem('orchestai_token');
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
