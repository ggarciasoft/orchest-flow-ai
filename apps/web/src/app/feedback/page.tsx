'use client';
import { useState } from 'react';
import Link from 'next/link';
import { PublicFooter } from '@/components/PublicFooter';
import { CheckCircle } from 'lucide-react';

export default function FeedbackPage() {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [submitted, setSubmitted] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    // TODO: wire to backend endpoint POST /api/feedback
    setSubmitted(true);
  };

  return (
    <div className="min-h-screen bg-[#f8fafc] flex flex-col">
      <header className="border-b border-slate-200 bg-white">
        <div className="max-w-3xl mx-auto px-6 h-14 flex items-center">
          <Link href="/" className="text-sm font-semibold text-slate-900 hover:text-indigo-600 transition-colors">← OrchestFlowAI</Link>
        </div>
      </header>
      <main className="flex-1 flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-md">
          {submitted ? (
            <div className="bg-white border border-slate-200 rounded-xl p-8 text-center">
              <CheckCircle size={40} className="text-emerald-500 mx-auto mb-4" />
              <h2 className="text-lg font-semibold text-slate-900 mb-1">Thanks for your feedback!</h2>
              <p className="text-sm text-slate-500 mb-6">We read every submission and will follow up if needed.</p>
              <Link href="/" className="text-sm text-indigo-600 hover:underline">Back to home</Link>
            </div>
          ) : (
            <div className="bg-white border border-slate-200 rounded-xl p-8">
              <h1 className="text-xl font-semibold text-slate-900 mb-1">Send us feedback</h1>
              <p className="text-sm text-slate-500 mb-6">We&apos;d love to hear what you think about OrchestFlowAI.</p>
              <form onSubmit={handleSubmit} className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1.5">Name</label>
                  <input value={name} onChange={e => setName(e.target.value)} required
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent placeholder:text-slate-400" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1.5">Email</label>
                  <input type="email" value={email} onChange={e => setEmail(e.target.value)} required
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent placeholder:text-slate-400" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1.5">Message</label>
                  <textarea value={message} onChange={e => setMessage(e.target.value)} required rows={4}
                    className="w-full border border-slate-200 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent placeholder:text-slate-400 resize-none" />
                </div>
                <button type="submit" className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-2.5 rounded-lg text-sm transition-colors">
                  Send feedback
                </button>
              </form>
            </div>
          )}
        </div>
      </main>
      <PublicFooter />
    </div>
  );
}
