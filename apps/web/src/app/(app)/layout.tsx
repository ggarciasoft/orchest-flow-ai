'use client';
import { useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import { cn } from '@/lib/utils';
import { isAuthenticated, clearToken } from '@/lib/auth';
import {
  LayoutDashboard, GitBranch, Play, CheckSquare,
  FileText, Settings, LogOut, KeyRound, ClipboardList
} from 'lucide-react';

const nav = [
  { href: '/dashboard',  label: 'Dashboard',  icon: LayoutDashboard },
  { href: '/workflows',  label: 'Workflows',   icon: GitBranch },
  { href: '/forms',      label: 'Forms',       icon: ClipboardList },
  { href: '/executions', label: 'Executions',  icon: Play },
  { href: '/approvals',  label: 'Approvals',   icon: CheckSquare },
  { href: '/documents',  label: 'Documents',   icon: FileText },
  { href: '/settings',   label: 'Settings',    icon: Settings },
  { href: '/settings/secrets', label: 'Secrets', icon: KeyRound },
];

/**
 * AppLayout — the shell for all authenticated app pages.
 * Renders the sidebar nav and main content area.
 * Redirects to /login if no JWT token is present.
 */
export default function AppLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();

  // Auth guard: redirect to login if no token is stored
  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace('/login');
    }
  }, [router]);

  // Render nothing while redirecting to avoid flash of protected content
  if (typeof window !== 'undefined' && !isAuthenticated()) {
    return null;
  }

  return (
    <div className="flex h-screen bg-[#f8fafc]">
      {/* Sidebar */}
      <aside className="w-56 bg-white border-r border-slate-200 flex flex-col shrink-0">
        {/* Logo */}
        <div className="px-5 py-5 border-b border-slate-200">
          <span className="text-base font-semibold text-slate-900 tracking-tight">OrchestFlowAI</span>
        </div>

        {/* Nav */}
        <nav className="flex-1 px-3 py-4 space-y-0.5 overflow-y-auto">
          {nav.map(({ href, label, icon: Icon }) => {
            const active = pathname === href || pathname.startsWith(href + '/');
            return (
              <Link
                key={href}
                href={href}
                className={cn(
                  'flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors',
                  active
                    ? 'bg-indigo-50 text-indigo-700 font-medium'
                    : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                )}
              >
                <Icon size={16} />
                {label}
              </Link>
            );
          })}
        </nav>

        {/* Footer */}
        <div className="px-3 py-4 border-t border-slate-200">
          <button
            onClick={() => { clearToken(); window.location.href = '/login'; }}
            className="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-slate-500 hover:bg-slate-100 hover:text-slate-700 w-full transition-colors"
          >
            <LogOut size={16} />
            Sign out
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-y-auto">
        <div className="max-w-7xl mx-auto px-8 py-8">
          {children}
        </div>
      </main>
    </div>
  );
}
