'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { ShieldAlert } from 'lucide-react';
import { useAuth } from '@/contexts/AuthContext';

/**
 * AdminPageGuard — wraps admin-only pages.
 * Redirects non-admins to /settings once the role is known.
 * Shows a brief access-denied state during the redirect.
 */
export function AdminPageGuard({ children }: { children: React.ReactNode }) {
  const { isAdmin, role } = useAuth();
  const router = useRouter();

  useEffect(() => {
    // role is null before hydration; only redirect once we have the actual role
    if (role !== null && !isAdmin) {
      router.replace('/settings');
    }
  }, [isAdmin, role, router]);

  // Still loading (pre-hydration) or authorized — render normally
  if (role === null || isAdmin) return <>{children}</>;

  // Role is known and not Admin — show access denied briefly while redirect fires
  return (
    <div className="flex flex-col items-center justify-center min-h-[400px] text-center space-y-3">
      <ShieldAlert className="text-slate-300" size={48} />
      <p className="text-slate-600 font-medium">Admin access required</p>
      <p className="text-sm text-slate-400">You don&apos;t have permission to view this page.</p>
    </div>
  );
}
