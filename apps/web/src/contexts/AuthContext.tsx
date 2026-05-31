'use client';

import { createContext, useContext, useEffect, useState } from 'react';
import { getRoleFromToken, decodeJwt, getToken, UserRole } from '@/lib/auth';

export interface AuthState {
  /** The user's role from the JWT, or null before hydration / when unauthenticated. */
  role: UserRole | null;
  displayName: string;
  email: string;
  /** True when role is Editor or Admin (may create/edit/delete resources). */
  canEdit: boolean;
  /** True when role is Admin (may access tenant/provider/secret settings). */
  isAdmin: boolean;
  /** True when role is Approver or Admin (may approve/reject workflow approvals). */
  isApprover: boolean;
}

const defaultState: AuthState = {
  role: null,
  displayName: '',
  email: '',
  canEdit: false,
  isAdmin: false,
  isApprover: false,
};

function readAuthState(): AuthState {
  const role = getRoleFromToken();
  const payload = decodeJwt(getToken() ?? '');
  return {
    role,
    displayName:
      (payload?.['display_name'] as string) ??
      (payload?.['email'] as string) ??
      'User',
    email: (payload?.['email'] as string) ?? '',
    canEdit: role === 'Editor' || role === 'Admin',
    isAdmin: role === 'Admin',
    isApprover: role === 'Approver' || role === 'Admin',
  };
}

const AuthContext = createContext<AuthState>(defaultState);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  // Start with defaults (SSR / pre-hydration), populate after mount when localStorage is available.
  const [auth, setAuth] = useState<AuthState>(defaultState);

  useEffect(() => {
    // Read immediately on mount (handles page refresh while already authenticated).
    setAuth(readAuthState());

    // Re-read whenever setToken / clearToken fires (handles login → navigate without full reload).
    const handleAuthChanged = () => setAuth(readAuthState());
    window.addEventListener('orchest:auth-changed', handleAuthChanged);
    return () => window.removeEventListener('orchest:auth-changed', handleAuthChanged);
  }, []);

  return <AuthContext.Provider value={auth}>{children}</AuthContext.Provider>;
}

/**
 * useAuth — returns the current authenticated user's role and derived permission flags.
 * Values are null / false until after client-side hydration.
 */
export function useAuth(): AuthState {
  return useContext(AuthContext);
}
