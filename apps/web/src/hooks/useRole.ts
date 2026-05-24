'use client';

import { useState, useEffect } from 'react';
import { getRoleFromToken, UserRole } from '@/lib/auth';

/**
 * useRole — React hook that returns the current user's role from the stored JWT.
 *
 * Reads the role claim from the JWT in localStorage on mount. Returns null when
 * the user is not authenticated or when the JWT has no role claim.
 *
 * @returns The current user's role ('Viewer' | 'Editor' | 'Admin' | 'Approver'), or null
 */
export function useRole(): UserRole | null {
  const [role, setRole] = useState<UserRole | null>(null);

  useEffect(() => {
    // Read role on mount — localStorage is only available in the browser
    setRole(getRoleFromToken());
  }, []);

  return role;
}

/**
 * useCanEdit — convenience hook that returns true when the user has Editor or Admin role.
 * Use this to conditionally render or disable edit/delete controls.
 *
 * @returns True if the user may edit resources
 */
export function useCanEdit(): boolean {
  const role = useRole();
  return role === 'Editor' || role === 'Admin';
}

/**
 * useIsAdmin — convenience hook that returns true when the user has Admin role.
 * Use this to gate admin-only UI sections such as user management.
 *
 * @returns True if the user is an Admin
 */
export function useIsAdmin(): boolean {
  const role = useRole();
  return role === 'Admin';
}
