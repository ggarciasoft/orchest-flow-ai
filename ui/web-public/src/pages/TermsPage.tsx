import Link from 'next/link';
import { PublicFooter } from '../components/PublicFooter';

export default function TermsPage() {
  return (
    <div className="min-h-screen bg-white flex flex-col">
      <header className="border-b border-slate-200">
        <div className="max-w-3xl mx-auto px-6 h-14 flex items-center">
          <Link href="/" className="text-sm font-semibold text-slate-900 hover:text-indigo-600 transition-colors">← OrchestFlowAI</Link>
        </div>
      </header>
      <main className="flex-1 max-w-3xl mx-auto px-6 py-12 w-full">
        <h1 className="text-2xl font-semibold text-slate-900 mb-2">Terms of Service</h1>
        <p className="text-sm text-slate-500 mb-8">Last updated: May 2026</p>
        <div className="prose prose-slate prose-sm max-w-none space-y-6 text-slate-700">
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">1. Acceptance of Terms</h2>
            <p>By accessing or using OrchestFlowAI, you agree to be bound by these Terms of Service. If you do not agree, please do not use the service.</p>
          </section>
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">2. Use of Service</h2>
            <p>You may use OrchestFlowAI for lawful purposes only. You are responsible for all activity that occurs under your account.</p>
          </section>
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">3. Data &amp; Privacy</h2>
            <p>We handle your data in accordance with our <Link href="/privacy" className="text-indigo-600 hover:underline">Privacy Policy</Link>.</p>
          </section>
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">4. Limitation of Liability</h2>
            <p>OrchestFlowAI is provided &quot;as is&quot; without warranties of any kind. We are not liable for any indirect or consequential damages.</p>
          </section>
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">5. Changes to Terms</h2>
            <p>We reserve the right to update these terms at any time. Continued use of the service constitutes acceptance of updated terms.</p>
          </section>
        </div>
      </main>
      <PublicFooter />
    </div>
  );
}
