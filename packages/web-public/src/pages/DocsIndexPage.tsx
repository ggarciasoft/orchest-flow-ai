import Link from 'next/link';
import { docs, categories } from '../content/docs/index';
import { BookOpen, Rocket, Lightbulb, Code2, Shield, BookMarked, HelpCircle } from 'lucide-react';

const CATEGORY_META: Record<string, { icon: React.ElementType; color: string; description: string }> = {
  'Getting Started': { icon: Rocket,     color: 'text-emerald-600 bg-emerald-50 border-emerald-200', description: 'Everything you need to get up and running.' },
  'How To':          { icon: HelpCircle, color: 'text-sky-600    bg-sky-50    border-sky-200',    description: 'Step-by-step guides for common tasks.' },
  'Core Concepts':   { icon: Lightbulb,  color: 'text-amber-600  bg-amber-50  border-amber-200',  description: 'How OrchestFlowAI works under the hood.' },
  'Developers':      { icon: Code2,      color: 'text-violet-600 bg-violet-50 border-violet-200', description: 'API reference, SDKs, and integration docs.' },
  'Operations':      { icon: Shield,     color: 'text-rose-600   bg-rose-50   border-rose-200',   description: 'Security, observability, and deployment.' },
  'Reference':       { icon: BookMarked, color: 'text-slate-600  bg-slate-50  border-slate-200',  description: 'Glossary, roadmap, and quick-reference material.' },
};

export default function DocsIndexPage() {
  return (
    <div>
      <div className="mb-10">
        <h1 className="text-2xl font-bold text-slate-900">Documentation</h1>
        <p className="mt-1 text-sm text-slate-500">Everything you need to build, run, and operate OrchestFlowAI.</p>
      </div>

      <div className="space-y-10">
        {categories.map(cat => {
          const meta = CATEGORY_META[cat] ?? { icon: BookOpen, color: 'text-indigo-600 bg-indigo-50 border-indigo-200', description: '' };
          const Icon = meta.icon;
          const catDocs = docs.filter(d => d.category === cat);
          return (
            <section key={cat}>
              <div className="mb-4 flex items-center gap-3">
                <div className={`rounded-lg border p-1.5 ${meta.color}`}>
                  <Icon className="h-4 w-4" />
                </div>
                <div>
                  <h2 className="text-sm font-semibold text-slate-800">{cat}</h2>
                  {meta.description && <p className="text-xs text-slate-400">{meta.description}</p>}
                </div>
              </div>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                {catDocs.map(d => (
                  <Link key={d.slug} href={`/docs/${d.slug}`}
                    className="group rounded-xl border border-slate-200 bg-white p-4 hover:border-indigo-300 hover:shadow-sm transition-all">
                    <div className="flex items-start gap-3">
                      <BookOpen className="mt-0.5 h-4 w-4 shrink-0 text-indigo-300 group-hover:text-indigo-500 transition-colors" />
                      <div className="min-w-0">
                        <p className="text-sm font-medium text-slate-900 group-hover:text-indigo-700 transition-colors">{d.title}</p>
                        {d.description && <p className="mt-0.5 text-xs text-slate-400 leading-relaxed">{d.description}</p>}
                      </div>
                    </div>
                  </Link>
                ))}
              </div>
            </section>
          );
        })}
      </div>
    </div>
  );
}
