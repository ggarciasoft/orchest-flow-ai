import Link from 'next/link';

export function PublicFooter() {
  return (
    <footer className="border-t border-slate-200 bg-white">
      <div className="max-w-6xl mx-auto px-6 py-8 flex flex-col sm:flex-row items-center justify-between gap-4">
        <div className="flex items-center gap-2">
          <div className="w-5 h-5 bg-indigo-600 rounded flex items-center justify-center">
            <span className="text-white text-[10px] font-bold">O</span>
          </div>
          <span className="text-xs text-slate-500">© {new Date().getFullYear()} OrchestFlowAI. All rights reserved.</span>
        </div>
        <nav className="flex items-center gap-5">
          {[
            { href: '/docs', label: 'Docs' },
            { href: '/terms', label: 'Terms' },
            { href: '/privacy', label: 'Privacy' },
            { href: '/feedback', label: 'Feedback' },
          ].map(({ href, label }) => (
            <Link key={href} href={href} className="text-xs text-slate-500 hover:text-slate-900 transition-colors">
              {label}
            </Link>
          ))}
        </nav>
      </div>
    </footer>
  );
}
