'use client';
import { useEffect, useState } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import { cn } from '@/lib/utils';
import { isAuthenticated, clearToken, decodeJwt, getToken } from '@/lib/auth';
import {
  LayoutDashboard, GitBranch, Play, CheckSquare,
  FileText, Settings, LogOut, KeyRound, ClipboardList, Cpu, Plug, Building2, FlaskConical, Database, MessageSquare
} from 'lucide-react';

type NavItem = {
  href: string;
  label: string;
  icon: React.ElementType;
  children?: { href: string; label: string; icon: React.ElementType }[];
};

const nav: NavItem[] = [
  { href: '/dashboard',  label: 'Dashboard',  icon: LayoutDashboard },
  { href: '/workflows',  label: 'Workflows',   icon: GitBranch },
  { href: '/forms',      label: 'Forms',       icon: ClipboardList },
  { href: '/executions', label: 'Executions',  icon: Play },
  { href: '/approvals',  label: 'Approvals',   icon: CheckSquare },
  { href: '/documents',  label: 'Documents',   icon: FileText },
  {
    href: '/playground',
    label: 'Playground',
    icon: FlaskConical,
    children: [
      { href: '/playground',          label: 'Form Playground',     icon: Play },
      { href: '/playground/external', label: 'External Data',       icon: Database },
    ],
  },
  {
    href: '/settings',
    label: 'Settings',
    icon: Settings,
    children: [
      { href: '/settings/tenant',        label: 'Tenant',        icon: Building2 },
      { href: '/settings/providers',     label: 'AI Providers',  icon: Cpu },
      { href: '/settings/integrations',  label: 'Integrations',  icon: Plug },
      { href: '/settings/secrets',       label: 'Secrets',       icon: KeyRound },
      { href: '/settings/ai-history',    label: 'AI History',    icon: MessageSquare },
    ],
  },
];

/**
 * AppLayout — the shell for all authenticated app pages.
 * Renders the sidebar nav and main content area.
 * Redirects to /login if no JWT token is present.
 */
export default function AppLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();

  // Decode current user from JWT for display in sidebar (client-only to avoid hydration mismatch)
  const [currentUser, setCurrentUser] = useState<{ displayName: string; email: string } | null>(null);

  // Auth guard + user decode — runs only on the client after mount
  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace('/login');
      return;
    }
    const payload = decodeJwt(getToken() ?? '');
    if (payload) {
      setCurrentUser({
        displayName: (payload['display_name'] as string) ?? (payload['email'] as string) ?? 'User',
        email: (payload['email'] as string) ?? '',
      });
    }
  }, [router]);

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
          {nav.map(({ href, label, icon: Icon, children }) => {
            const active = pathname === href || (pathname.startsWith(href + '/') && !children?.some(c => pathname.startsWith(c.href)));
            const childActive = children?.some(c => pathname === c.href || pathname.startsWith(c.href + '/'));
            return (
              <div key={href}>
                <Link
                  href={href}
                  className={cn(
                    'flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors',
                    active || childActive
                      ? 'bg-indigo-50 text-indigo-700 font-medium'
                      : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'
                  )}
                >
                  <Icon size={16} />
                  {label}
                </Link>
                {/* Sub-items — shown when parent or a child is active */}
                {children && (active || childActive) && (
                  <div className="ml-4 mt-0.5 space-y-0.5 border-l border-slate-200 pl-3">
                    {children.map(({ href: chref, label: clabel, icon: CIcon }) => {
                      const cActive = pathname === chref ||
                        (pathname.startsWith(chref + '/') &&
                         !children.some(sibling => sibling.href !== chref && pathname.startsWith(sibling.href)));
                      return (
                        <Link
                          key={chref}
                          href={chref}
                          className={cn(
                            'flex items-center gap-2 px-2 py-1.5 rounded-lg text-xs transition-colors',
                            cActive
                              ? 'bg-violet-50 text-violet-700 font-medium'
                              : 'text-slate-500 hover:bg-slate-100 hover:text-slate-800'
                          )}
                        >
                          <CIcon size={13} />
                          {clabel}
                        </Link>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </nav>

        {/* Footer */}
        <div className="px-3 py-4 border-t border-slate-200 space-y-2">
          {/* Logged-in user */}
          {currentUser && (
            <div className="px-3 py-2 rounded-lg bg-slate-50 border border-slate-200">
              <p className="text-xs font-medium text-slate-800 truncate">{currentUser.displayName}</p>
              <p className="text-xs text-slate-400 truncate">{currentUser.email}</p>
            </div>
          )}
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
