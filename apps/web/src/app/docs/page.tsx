import Link from 'next/link';
import { docs, categories } from '@/content/docs/index';
import { BookOpen } from 'lucide-react';

export default function DocsIndexPage() {
  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-semibold text-slate-900">Documentation</h1>
        <p className="text-sm text-slate-500 mt-1">Everything you need to build, run, and operate OrchestAI.</p>
      </div>

      <div className="space-y-8">
        {categories.map(cat => (
          <div key={cat}>
            <h2 className="text-xs font-semibold text-slate-400 uppercase tracking-wider mb-3">{cat}</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {docs.filter(d => d.category === cat).map(d => (
                <Link key={d.slug} href={`/docs/${d.slug}`}
                  className="bg-white border border-slate-200 rounded-xl p-4 hover:border-indigo-300 hover:shadow-sm transition-all group">
                  <div className="flex items-center gap-2 mb-1">
                    <BookOpen size={14} className="text-indigo-400 group-hover:text-indigo-600 transition-colors" />
                    <span className="text-sm font-medium text-slate-900">{d.title}</span>
                  </div>
                  <p className="text-xs text-slate-400">{d.filename}</p>
                </Link>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
