import Link from 'next/link';
import { docs, categories } from '@/content/docs/index';
import { PublicFooter } from '@/components/PublicFooter';
import { BookOpen } from 'lucide-react';

export default function DocsLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen bg-[#f8fafc] flex flex-col">
      {/* Top nav */}
      <header className="bg-white border-b border-slate-200 sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-6 h-14 flex items-center gap-6">
          <Link href="/" className="flex items-center gap-2 shrink-0">
            <div className="w-6 h-6 bg-indigo-600 rounded flex items-center justify-center">
              <span className="text-white text-[10px] font-bold">O</span>
            </div>
            <span className="text-sm font-semibold text-slate-900">OrchestFlowAI</span>
          </Link>
          <span className="text-slate-300">/</span>
          <span className="text-sm text-slate-600 flex items-center gap-1.5"><BookOpen size={14} />Docs</span>
        </div>
      </header>

      <div className="flex flex-1 max-w-7xl mx-auto w-full px-6 py-8 gap-8">
        {/* Sidebar */}
        <aside className="w-52 shrink-0">
          <nav className="space-y-5 sticky top-24">
            {categories.map(cat => (
              <div key={cat}>
                <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">{cat}</p>
                <ul className="space-y-0.5">
                  {docs.filter(d => d.category === cat).map(d => (
                    <li key={d.slug}>
                      <Link
                        href={`/docs/${d.slug}`}
                        className="block text-sm text-slate-600 hover:text-indigo-600 px-2 py-1.5 rounded-lg hover:bg-indigo-50 transition-colors"
                      >
                        {d.title}
                      </Link>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </nav>
        </aside>

        {/* Content */}
        <main className="flex-1 min-w-0">
          {children}
        </main>
      </div>

      <PublicFooter />
    </div>
  );
}
