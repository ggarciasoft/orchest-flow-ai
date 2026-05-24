'use client';
import { useEffect } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import Link from 'next/link';
import { LayoutDashboard, GitBranch, Play, CheckSquare, FileText, Settings } from 'lucide-react';
import { isAuthenticated } from '@/lib/auth';

const nav = [
  { label: 'Dashboard', href: '/dashboard', icon: LayoutDashboard },
  { label: 'Workflows', href: '/workflows', icon: GitBranch },
  { label: 'Executions', href: '/executions', icon: Play },
  { label: 'Approvals', href: '/approvals', icon: CheckSquare },
  { label: 'Documents', href: '/documents', icon: FileText },
  { label: 'Settings', href: '/settings', icon: Settings },
];

/**
 * AppLayout — the shell for all authenticated app pages.
 * Renders the sidebar nav and main content area.
 * Redirects to /login if no JWT token is present.
 */
export default function AppLayout({ children }: { children: React.ReactNode }) {
  const path = usePathname();
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
    <div className="min-h-screen flex">
      <aside className="w-64 bg-gray-900 text-white flex flex-col shrink-0">
        <div className="p-6 border-b border-gray-700">
          <h1 className="text-xl font-bold">OrchestAI</h1>
          <p className="text-xs text-gray-400 mt-1">AI Workflow Platform</p>
        </div>
        <nav className="flex-1 p-4 space-y-1">
          {nav.map(({ label, href, icon: Icon }) => (
            <Link key={href} href={href}
              className={`flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                path.startsWith(href) ? 'bg-blue-600 text-white' : 'text-gray-300 hover:bg-gray-800 hover:text-white'
              }`}>
              <Icon size={18} />
              {label}
            </Link>
          ))}
        </nav>
      </aside>
      <main className="flex-1 overflow-auto bg-gray-50">
        {children}
      </main>
    </div>
  );
}
