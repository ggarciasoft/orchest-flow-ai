import Link from 'next/link';
import { PublicFooter } from '../components/PublicFooter';

export default function PrivacyPage() {
  return (
    <div className="min-h-screen bg-white flex flex-col">
      <header className="border-b border-slate-200">
        <div className="max-w-3xl mx-auto px-6 h-14 flex items-center">
          <Link href="/" className="text-sm font-semibold text-slate-900 hover:text-indigo-600 transition-colors">← OrchestFlowAI</Link>
        </div>
      </header>
      <main className="flex-1 max-w-3xl mx-auto px-6 py-12 w-full">
        <h1 className="text-2xl font-semibold text-slate-900 mb-2">Privacy Policy</h1>
        <p className="text-sm text-slate-500 mb-8">Last updated: May 2026</p>
        <div className="prose prose-slate prose-sm max-w-none space-y-6 text-slate-700">
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">1. Information We Collect</h2>
            <p>We collect account information (name, email), usage data (workflow executions, feature interactions), and cookies to operate and improve the service.</p>
          </section>
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">2. How We Use Information</h2>
            <p>We use your information for service delivery, analytics to improve the product, and communication about updates or support matters.</p>
          </section>
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">3. Data Sharing</h2>
            <p>We do not sell your data. We share information only with processors necessary to deliver the service, under strict data processing agreements.</p>
          </section>
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">4. Cookies</h2>
            <p>We use functional cookies required for the service to operate, plus optional analytics cookies. You can manage cookie preferences via the cookie banner shown on your first visit.</p>
          </section>
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">5. Your Rights</h2>
            <p>You have the right to access, correct, or request deletion of your personal data. Submit requests to <a href="mailto:ggarciasoft@gmail.com" className="text-indigo-600 hover:underline">ggarciasoft@gmail.com</a>.</p>
          </section>
          <section>
            <h2 className="text-base font-semibold text-slate-900 mb-2">6. Contact</h2>
            <p>For privacy-related inquiries, contact us at <a href="mailto:ggarciasoft@gmail.com" className="text-indigo-600 hover:underline">ggarciasoft@gmail.com</a>.</p>
          </section>
        </div>
      </main>
      <PublicFooter />
    </div>
  );
}
