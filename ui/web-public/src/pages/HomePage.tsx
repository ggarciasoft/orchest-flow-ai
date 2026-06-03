import Link from 'next/link';
import {
  ArrowRight, GitBranch, Zap, Shield, BarChart3, ClipboardList, Database,
  Sparkles, Workflow, PlayCircle, Activity, Brain, UserCheck, Code2,
  PlayCircle as StartIcon, StopCircle,
} from 'lucide-react';
import { PublicFooter } from '../components/PublicFooter';

// ─── data ────────────────────────────────────────────────────────────────────

const stats = [
  { value: '31',   label: 'Built-in nodes' },
  { value: '4',    label: 'AI providers' },
  { value: '7',    label: 'Node categories' },
  { value: '100%', label: 'Open source' },
];

const providers = [
  { name: 'OpenAI',    color: 'bg-emerald-50 text-emerald-700 border-emerald-200' },
  { name: 'Anthropic', color: 'bg-amber-50   text-amber-700   border-amber-200'  },
  { name: 'Azure',     color: 'bg-sky-50     text-sky-700     border-sky-200'    },
  { name: 'Ollama',    color: 'bg-violet-50  text-violet-700  border-violet-200' },
];

const steps = [
  {
    icon: Workflow,
    title: 'Design',
    desc: 'Drag-and-drop nodes onto the canvas, or let the AI assistant generate a workflow from a prompt.',
  },
  {
    icon: PlayCircle,
    title: 'Run',
    desc: 'Execute pipelines with persistent state, automatic retries, and pause-for-human-approval steps.',
  },
  {
    icon: Activity,
    title: 'Monitor',
    desc: 'Follow a live execution timeline with node-level logs, AI token usage, and full audit trails.',
  },
];

const features = [
  { icon: GitBranch,    title: 'Visual Workflow Designer', desc: 'Drag-and-drop canvas with 30+ node types, version history, and undo/redo.' },
  { icon: Zap,          title: 'Multi-Provider AI',        desc: 'Route tasks across OpenAI, Anthropic, Azure, and Ollama — switch defaults without a restart.' },
  { icon: ClipboardList, title: 'Forms & Approvals',       desc: 'Build dynamic forms with AI assistance and pause workflows for human input.' },
  { icon: Database,     title: 'External Data Intake',     desc: 'Pause and wait for external systems to POST data, with field validation and type coercion.' },
  { icon: Shield,       title: 'Enterprise Security',      desc: 'JWT auth, RBAC roles, tenant isolation, and encrypted secrets out of the box.' },
  { icon: BarChart3,    title: 'Real-Time Monitoring',     desc: 'Live execution timeline, node logs, AI chat history, and token usage tracking.' },
];

// ─── canvas mockup ────────────────────────────────────────────────────────────

const mockNodes = [
  { id: 'start',    x:  40, y:  80, label: 'Start',       color: '#4f46e5', icon: StartIcon, w: 110 },
  { id: 'ai',       x: 200, y:  30, label: 'AI Classify', color: '#818cf8', icon: Brain,      w: 120 },
  { id: 'cond',     x: 200, y: 130, label: 'Condition',   color: '#f59e0b', icon: GitBranch,  w: 120 },
  { id: 'approval', x: 370, y:  30, label: 'Approval',    color: '#ef4444', icon: UserCheck,  w: 120 },
  { id: 'code',     x: 370, y: 130, label: 'Transform',   color: '#8b5cf6', icon: Code2,      w: 120 },
  { id: 'endNode',  x: 530, y:  80, label: 'End',         color: '#0f172a', icon: StopCircle, w: 100 },
];

const mockEdges: [string, string][] = [
  ['start', 'ai'], ['start', 'cond'],
  ['ai', 'approval'], ['cond', 'code'],
  ['approval', 'endNode'], ['code', 'endNode'],
];

const NODE_H = 34;

function nodeCenter(id: string) {
  const n = mockNodes.find(n => n.id === id)!;
  return { cx: n.x + n.w / 2, cy: n.y + NODE_H / 2 };
}

function CanvasMockup() {
  return (
    <div className="relative w-full max-w-[680px] mx-auto select-none" style={{ height: 200 }}>
      <svg className="absolute inset-0 w-full h-full" xmlns="http://www.w3.org/2000/svg">
        <defs>
          <pattern id="grid" width="20" height="20" patternUnits="userSpaceOnUse">
            <path d="M 20 0 L 0 0 0 20" fill="none" stroke="#e2e8f0" strokeWidth="0.5" />
          </pattern>
          <marker id="arrow" markerWidth="6" markerHeight="6" refX="5" refY="3" orient="auto">
            <path d="M0,0 L0,6 L6,3 z" fill="#94a3b8" />
          </marker>
        </defs>
        <rect width="100%" height="100%" fill="url(#grid)" />
        {mockEdges.map(([src, tgt]) => {
          const s = nodeCenter(src);
          const t = nodeCenter(tgt);
          const mx = (s.cx + t.cx) / 2;
          return (
            <path
              key={`${src}-${tgt}`}
              d={`M ${s.cx} ${s.cy} C ${mx} ${s.cy}, ${mx} ${t.cy}, ${t.cx} ${t.cy}`}
              fill="none" stroke="#94a3b8" strokeWidth="1.5" markerEnd="url(#arrow)"
            />
          );
        })}
      </svg>
      {mockNodes.map(({ id, x, y, label, color, icon: Icon, w }) => (
        <div key={id}
          className="absolute flex items-center gap-1.5 rounded-lg px-2.5 text-white text-[11px] font-semibold shadow-sm"
          style={{ left: x, top: y, width: w, height: NODE_H, background: color }}
        >
          <Icon size={13} className="shrink-0 opacity-90" />
          <span className="truncate">{label}</span>
        </div>
      ))}
      <div className="absolute top-2 right-2 flex items-center gap-1.5 bg-white/90 border border-slate-200 rounded-lg px-2.5 py-1.5 shadow-sm">
        <span className="text-[10px] font-medium text-slate-500">v3</span>
        <div className="w-px h-3 bg-slate-200" />
        <span className="text-[10px] text-indigo-600 font-semibold flex items-center gap-0.5"><Sparkles size={9} /> AI</span>
        <div className="w-px h-3 bg-slate-200" />
        <span className="text-[10px] font-medium text-slate-500">Save</span>
        <div className="w-px h-3 bg-slate-200" />
        <span className="text-[10px] font-semibold text-emerald-600">▶ Run</span>
      </div>
    </div>
  );
}

// ─── page ─────────────────────────────────────────────────────────────────────

interface HomePageProps {
  /** Slot for the auth/CTA link in the nav (defaults to "Sign in" → /login). */
  navAuthSlot?: React.ReactNode;
}

export default function HomePage({ navAuthSlot }: HomePageProps = {}) {
  return (
    <div className="min-h-screen bg-white flex flex-col">

      {/* Nav */}
      <header className="sticky top-0 z-30 border-b border-slate-200 bg-white/80 backdrop-blur-md">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 h-14 flex items-center justify-between">
          <Link href="/" className="flex items-center gap-2">
            <div className="w-7 h-7 bg-indigo-600 rounded-lg flex items-center justify-center">
              <span className="text-white text-xs font-bold">O</span>
            </div>
            <span className="text-sm font-semibold text-slate-900">OrchestFlowAI</span>
          </Link>
          <div className="flex items-center gap-2 sm:gap-4">
            <Link href="/docs" className="hidden sm:inline text-sm text-slate-600 hover:text-slate-900 transition-colors">Docs</Link>
            {navAuthSlot ?? (
              <Link href="/login" className="text-sm text-slate-600 hover:text-slate-900 transition-colors">Sign in</Link>
            )}
            <a href="https://github.com/ggarciasoft/orchest-flow-ai" target="_blank" rel="noopener noreferrer"
              className="bg-slate-900 hover:bg-slate-700 text-white text-sm font-medium px-3.5 py-1.5 rounded-lg transition-colors flex items-center gap-1.5">
              GitHub
            </a>
          </div>
        </div>
      </header>

      {/* Hero */}
      <section className="relative overflow-hidden bg-gradient-to-b from-indigo-50/70 via-white to-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 pt-16 sm:pt-24 pb-10 text-center">
          <div className="inline-flex items-center gap-2 bg-white border border-indigo-100 text-indigo-700 text-xs font-medium px-3 py-1 rounded-full mb-6 shadow-sm">
            <Sparkles size={12} /> Self-hosted · Open source · Apache 2.0
          </div>
          <h1 className="text-4xl sm:text-5xl lg:text-6xl font-bold text-slate-900 leading-[1.1] tracking-tight">
            Orchestrate AI workflows{' '}
            <span className="text-indigo-600">on your own infrastructure</span>
          </h1>
          <p className="text-base sm:text-lg text-slate-500 mt-5 max-w-xl mx-auto leading-relaxed">
            Build, run, and monitor multi-LLM pipelines with a visual designer, approval workflows, and full audit trails — fully self-hosted, no vendor lock-in.
          </p>
          <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-center gap-3 mt-8 mb-10">
            <a href="https://github.com/ggarciasoft/orchest-flow-ai" target="_blank" rel="noopener noreferrer"
              className="bg-indigo-600 hover:bg-indigo-700 text-white font-medium px-6 py-3 rounded-xl text-sm flex items-center justify-center gap-2 transition-colors shadow-sm">
              Star on GitHub <ArrowRight size={16} />
            </a>
            <Link href="/docs/setup" className="border border-slate-200 hover:bg-slate-50 text-slate-700 font-medium px-6 py-3 rounded-xl text-sm transition-colors">
              Self-host in 5 min
            </Link>
          </div>
        </div>

        {/* Canvas mockup */}
        <div className="max-w-4xl mx-auto px-4 sm:px-6 pb-12">
          <div className="relative rounded-2xl border border-slate-200 bg-white overflow-hidden shadow-xl shadow-slate-200/60">
            <div className="flex items-center gap-1.5 px-4 py-3 border-b border-slate-100 bg-slate-50">
              <div className="w-3 h-3 rounded-full bg-red-400" />
              <div className="w-3 h-3 rounded-full bg-amber-400" />
              <div className="w-3 h-3 rounded-full bg-emerald-400" />
              <span className="ml-3 text-xs text-slate-400 font-mono">workflow-designer</span>
            </div>
            <div className="p-4"><CanvasMockup /></div>
          </div>
        </div>
      </section>

      {/* Provider strip */}
      <section className="border-y border-slate-200 bg-white">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 py-5 flex flex-col sm:flex-row items-center justify-center gap-3 sm:gap-6">
          <span className="text-xs font-medium text-slate-400 uppercase tracking-wider shrink-0">Works with</span>
          <div className="flex flex-wrap items-center justify-center gap-2">
            {providers.map(({ name, color }) => (
              <span key={name} className={`text-xs font-semibold px-3 py-1.5 rounded-lg border ${color}`}>{name}</span>
            ))}
          </div>
        </div>
      </section>

      {/* Stats */}
      <section className="border-b border-slate-200 bg-white">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 py-8 grid grid-cols-2 sm:grid-cols-4 gap-6">
          {stats.map(({ value, label }) => (
            <div key={label} className="text-center">
              <p className="text-2xl sm:text-3xl font-bold text-slate-900">{value}</p>
              <p className="text-xs text-slate-500 mt-1">{label}</p>
            </div>
          ))}
        </div>
      </section>

      {/* How it works */}
      <section className="max-w-5xl mx-auto px-4 sm:px-6 py-16 sm:py-20 w-full">
        <div className="text-center mb-12">
          <h2 className="text-2xl sm:text-3xl font-semibold text-slate-900">From idea to production in three steps</h2>
          <p className="text-sm text-slate-500 mt-2 max-w-lg mx-auto">No infrastructure to manage — design, run, and observe everything from one place.</p>
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-6">
          {steps.map(({ icon: Icon, title, desc }, i) => (
            <div key={title} className="relative bg-white border border-slate-200 rounded-2xl p-6">
              <span className="absolute top-5 right-5 text-xs font-semibold text-slate-300">0{i + 1}</span>
              <div className="w-10 h-10 bg-indigo-50 rounded-xl flex items-center justify-center mb-4">
                <Icon size={20} className="text-indigo-600" />
              </div>
              <h3 className="text-base font-semibold text-slate-900 mb-1.5">{title}</h3>
              <p className="text-sm text-slate-500 leading-relaxed">{desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Features */}
      <section className="bg-[#f8fafc] border-t border-slate-200">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 py-16 sm:py-20 w-full">
          <h2 className="text-2xl sm:text-3xl font-semibold text-slate-900 text-center mb-12">Everything you need to ship AI faster</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
            {features.map(({ icon: Icon, title, desc }) => (
              <div key={title} className="bg-white border border-slate-200 rounded-xl p-6 hover:border-indigo-300 hover:shadow-sm transition-all">
                <div className="w-9 h-9 bg-indigo-50 rounded-lg flex items-center justify-center mb-4">
                  <Icon size={18} className="text-indigo-600" />
                </div>
                <h3 className="text-sm font-semibold text-slate-900 mb-1.5">{title}</h3>
                <p className="text-xs text-slate-500 leading-relaxed">{desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="bg-indigo-600">
        <div className="max-w-3xl mx-auto px-4 sm:px-6 py-16 sm:py-20 text-center">
          <h2 className="text-2xl sm:text-3xl font-semibold text-white mb-3">Ready to self-host?</h2>
          <p className="text-indigo-200 text-sm mb-7 max-w-md mx-auto">Clone the repo, set two env vars, and run. Apache 2.0 licensed — use it however you need.</p>
          <a href="https://github.com/ggarciasoft/orchest-flow-ai" target="_blank" rel="noopener noreferrer"
            className="bg-white text-indigo-600 hover:bg-indigo-50 font-medium px-6 py-3 rounded-xl text-sm transition-colors inline-flex items-center gap-2">
            View on GitHub <ArrowRight size={16} />
          </a>
        </div>
      </section>

      <PublicFooter />
    </div>
  );
}
