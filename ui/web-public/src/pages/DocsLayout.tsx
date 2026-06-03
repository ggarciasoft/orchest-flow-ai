'use client';

import { useState } from 'react';
import Link from 'next/link';
import { docs, categories } from '../content/docs/index';
import { PublicFooter } from '../components/PublicFooter';
import { BookOpen, Menu, X } from 'lucide-react';

function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <nav className="space-y-5">
      {categories.map(cat => (
        <div key={cat}>
          <p className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">{cat}</p>
          <ul className="space-y-0.5">
            {docs.filter(d => d.category === cat).map(d => (
              <li key={d.slug}>
                <Link href={`/docs/${d.slug}`} onClick={onNavigate}
                  className="block text-sm text-slate-600 hover:text-indigo-600 px-2 py-1.5 rounded-lg hover:bg-indigo-50 transition-colors">
                  {d.title}
                </Link>
              </li>
            ))}
          </ul>
        </div>
      ))}
    </nav>
  );
}

export default function DocsLayout({ children }: { children: React.ReactNode }) {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div className="min-h-screen bg-[#f8fafc] flex flex-col">
      <header className="bg-white border-b border-slate-200 sticky top-0 z-30">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 h-14 flex items-center gap-4 sm:gap-6">
          <button
            onClick={() => setSidebarOpen(!sidebarOpen)}
            className="lg:hidden p-1.5 -ml-1.5 rounded-lg text-slate-500 hover:text-slate-900 hover:bg-slate-100 transition-colors"
            aria-label="Toggle navigation"
          >
            {sidebarOpen ? <X size={20} /> : <Menu size={20} />}
          </button>
          <Link href="/" className="flex items-center gap-2 shrink-0">
            <div className="w-6 h-6 bg-indigo-600 rounded flex items-center justify-center">
              <span className="text-white text-[10px] font-bold">O</span>
            </div>
            <span className="text-sm font-semibold text-slate-900">OrchestFlowAI</span>
          </Link>
          <span className="text-slate-300 hidden sm:inline">/</span>
          <span className="text-sm text-slate-600 items-center gap-1.5 hidden sm:flex"><BookOpen size={14} />Docs</span>
        </div>
      </header>

      {/* Mobile sidebar overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 z-20 lg:hidden" aria-hidden="true">
          <div className="absolute inset-0 bg-slate-900/20 backdrop-blur-sm" onClick={() => setSidebarOpen(false)} />
          <aside className="absolute top-14 left-0 bottom-0 w-64 bg-white border-r border-slate-200 overflow-y-auto p-4">
            <Sidebar onNavigate={() => setSidebarOpen(false)} />
          </aside>
        </div>
      )}

      <div className="flex flex-1 max-w-7xl mx-auto w-full px-4 sm:px-6 py-6 sm:py-8 gap-8">
        <aside className="hidden lg:block w-52 shrink-0">
          <div className="sticky top-24">
            <Sidebar />
          </div>
        </aside>
        <main className="flex-1 min-w-0">{children}</main>
      </div>

      <PublicFooter />
    </div>
  );
}
