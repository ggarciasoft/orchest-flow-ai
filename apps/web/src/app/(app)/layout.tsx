'use client';
import { useEffect, useMemo } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import { cn } from '@/lib/utils';
import { isAuthenticated, clearToken, UserRole } from '@/lib/auth';
import { useAuth } from '@/contexts/AuthContext';
import {
  LayoutDashboard, GitBranch, Play, CheckSquare,
  FileText, Settings, LogOut, KeyRound, ClipboardList, Cpu, Plug, Building2,
  FlaskConical, Database, MessageSquare, SlidersHorizontal, BookOpen, Users,
} from 'lucide-react';

type NavChild = { href: string; label: string; icon: React.ElementType };
type NavItem  = { href: string; label: string; icon: React.ElementType; children?: NavChild[] };

const ROLE_BADGE: Record<UserRole, string> = {
  Admin:    'bg-violet-100 text-violet-700',
  Editor:   'bg-indigo-100 text-indigo-700',
  Approver: 'bg-emerald-100 text-emerald-700',
  Viewer:   'bg-slate-100   text-slate-600',
};

const NAV_BASE: NavItem[] = [
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
      { href: '/playground',          label: 'Form Playground', icon: Play },
      { href: '/playground/external', label: 'External Data',   icon: Database },
    ],
  },
];

// Settings sub-routes with visibility rules.
// minRole: 'Admin' → Admin only; 'Editor' → Editor or Admin; undefined → all roles.
const SETTINGS_CHILDREN: (NavChild & { minRole?: UserRole })[] = [
  { href: '/settings/tenant',       label: 'Tenant',        icon: Building2,         minRole: 'Admin'  },
  { href: '/settings/team',         label: 'Team',          icon: Users,             minRole: 'Admin'  },
  { href: '/settings/providers',    label: 'AI Providers',  icon: Cpu,               minRole: 'Admin'  },
  { href: '/settings/integrations', label: 'Integrations',  icon: Plug,              minRole: 'Admin'  },
  { href: '/settings/secrets',      label: 'Secrets',       icon: KeyRound,          minRole: 'Admin'  },
  { href: '/settings/presets',      label: 'Presets',       icon: BookOpen,          minRole: 'Editor' },
  { href: '/settings/config',       label: 'Configuration', icon: SlidersHorizontal                   },
  { href: '/settings/ai-history',   label: 'AI History',    icon: MessageSquare                       },
];

/**
 * AppLayout — the shell for all authenticated app pages.
 * Handles auth guard, sidebar navigation (filtered by role), and user/role display.
 */
export default function AppLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router   = useRouter();
  const auth     = useAuth();

  // Auth guard — redirect to login when not authenticated
  useEffect(() => {
    if (!isAuthenticated()) {
      router.replace('/login');
    }
  }, [router]);

  // Build the settings nav children visible to this role
  const settingsChildren = useMemo<NavChild[]>(() => {
    return SETTINGS_CHILDREN.filter(c => {
      if (c.minRole === 'Admin')  return auth.isAdmin;
      if (c.minRole === 'Editor') return auth.canEdit;
      return true;
    });
  }, [auth.isAdmin, auth.canEdit]);

  const nav: NavItem[] = useMemo(() => [
    ...NAV_BASE,
    { href: '/settings', label: 'Settings', icon: Settings, children: settingsChildren },
  ], [settingsChildren]);

  return (
    <div className="flex h-screen bg-[#f8fafc]">
      {/* Sidebar */}
      <aside className="w-56 bg-white border-r border-slate-200 flex flex-col shrink-0">
        {/* Logo */}
        <div className="px-5 py-5 border-b border-slate-200">
          <Link href="/" className="text-base font-semibold text-slate-900 tracking-tight hover:text-indigo-600 transition-colors">
            OrchestFlowAI
          </Link>
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
                      : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
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
                              : 'text-slate-500 hover:bg-slate-100 hover:text-slate-800',
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

        {/* Footer — user info + role badge + sign out */}
        <div className="px-3 py-4 border-t border-slate-200 space-y-2">
          {auth.displayName && (
            <div className="px-3 py-2 rounded-lg bg-slate-50 border border-slate-200">
              <div className="flex items-center justify-between gap-2">
                <p className="text-xs font-medium text-slate-800 truncate">{auth.displayName}</p>
                {auth.role && (
                  <span className={cn('text-[10px] px-1.5 py-0.5 rounded font-semibold shrink-0', ROLE_BADGE[auth.role])}>
                    {auth.role}
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-400 truncate mt-0.5">{auth.email}</p>
            </div>
          )}
          <button
            onClick={() => { clearToken(); window.location.href = '/login'; }}
            className="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-slate-500 hover:bg-slate-100 hover:text-slate-700 w-full transition-colors"
            suppressHydrationWarning
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
