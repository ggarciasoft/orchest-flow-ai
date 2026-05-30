import Link from 'next/link';
import { ArrowRight, GitBranch, Zap, Shield, BarChart3, CheckCircle, ClipboardList, Database } from 'lucide-react';
import { PublicFooter } from '@/components/PublicFooter';

const features = [
  {
    icon: GitBranch,
    title: 'Visual Workflow Designer',
    desc: 'Drag-and-drop canvas with 20+ node types, icons, AI assistant, version history, and undo/redo.',
  },
  {
    icon: Zap,
    title: 'Multi-Provider AI',
    desc: 'Route tasks across OpenAI, Anthropic, Azure, and Ollama. Switch default provider without restart.',
  },
  {
    icon: ClipboardList,
    title: 'Custom Forms & Approvals',
    desc: 'Build dynamic forms with AI assistance. Pause workflows for human input or external data.',
  },
  {
    icon: Database,
    title: 'External Data Intake',
    desc: 'Pause workflows and wait for external systems to POST data. Validate fields and coerce types.',
  },
  {
    icon: Shield,
    title: 'Enterprise Security',
    desc: 'JWT auth, RBAC roles, tenant isolation, encrypted secrets, and HMAC audit chains.',
  },
  {
    icon: BarChart3,
    title: 'Real-Time Monitoring',
    desc: 'Live execution timeline, node-level logs, AI chat history, and token usage tracking.',
  },
];

export default function HomePage() {
  return (
    <div className="min-h-screen bg-white flex flex-col">
      {/* Nav */}
      <header className="border-b border-slate-200">
        <div className="max-w-6xl mx-auto px-6 h-14 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-7 h-7 bg-indigo-600 rounded-lg flex items-center justify-center">
              <span className="text-white text-xs font-bold">O</span>
            </div>
            <span className="text-sm font-semibold text-slate-900">OrchestFlowAI</span>
          </div>
          <div className="flex items-center gap-3">
            <Link href="/docs" className="text-sm text-slate-600 hover:text-slate-900 transition-colors">Docs</Link>
            <Link href="/login" className="text-sm text-slate-600 hover:text-slate-900 transition-colors">Sign in</Link>
            <Link href="/onboarding" className="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium px-4 py-1.5 rounded-lg transition-colors">
              Get started
            </Link>
          </div>
        </div>
      </header>

      {/* Hero */}
      <section className="flex-1 flex flex-col items-center justify-center px-6 py-24 text-center bg-[#f8fafc]">
        <div className="inline-flex items-center gap-2 bg-indigo-50 border border-indigo-100 text-indigo-700 text-xs font-medium px-3 py-1 rounded-full mb-6">
          <CheckCircle size={12} /> Visual designer · AI builder · External data intake
        </div>
        <h1 className="text-5xl font-bold text-slate-900 leading-tight max-w-3xl">
          Orchestrate AI workflows <span className="text-indigo-600">at enterprise scale</span>
        </h1>
        <p className="text-lg text-slate-500 mt-5 max-w-xl">
          Build, run, and monitor multi-LLM pipelines with a visual designer, approval workflows, and full audit trails.
        </p>
        <div className="flex items-center gap-3 mt-8">
          <Link href="/onboarding" className="bg-indigo-600 hover:bg-indigo-700 text-white font-medium px-6 py-3 rounded-xl text-sm flex items-center gap-2 transition-colors">
            Start for free <ArrowRight size={16} />
          </Link>
          <Link href="/login" className="border border-slate-200 hover:bg-slate-50 text-slate-700 font-medium px-6 py-3 rounded-xl text-sm transition-colors">
            Sign in
          </Link>
        </div>
      </section>

      {/* Features */}
      <section className="max-w-6xl mx-auto px-6 py-20 w-full">
        <h2 className="text-2xl font-semibold text-slate-900 text-center mb-12">Everything you need to ship AI faster</h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {features.map(({ icon: Icon, title, desc }) => (
            <div key={title} className="bg-white border border-slate-200 rounded-xl p-6">
              <div className="w-9 h-9 bg-indigo-50 rounded-lg flex items-center justify-center mb-4">
                <Icon size={18} className="text-indigo-600" />
              </div>
              <h3 className="text-sm font-semibold text-slate-900 mb-1">{title}</h3>
              <p className="text-xs text-slate-500 leading-relaxed">{desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className="bg-indigo-600 py-16 px-6 text-center">
        <h2 className="text-2xl font-semibold text-white mb-3">Ready to get started?</h2>
        <p className="text-indigo-200 text-sm mb-6">Deploy in minutes. No infrastructure required.</p>
        <Link href="/onboarding" className="bg-white text-indigo-600 hover:bg-indigo-50 font-medium px-6 py-2.5 rounded-lg text-sm transition-colors inline-block">
          Create your workspace
        </Link>
      </section>

      {/* Footer */}
      <PublicFooter />
    </div>
  );
}
