'use client';
import { useState, useEffect } from 'react';
import Link from 'next/link';
import { Cookie } from 'lucide-react';

const STORAGE_KEY = 'OrchestFlowAI_cookie_consent';

export function CookieBanner() {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    if (!localStorage.getItem(STORAGE_KEY)) setVisible(true);
  }, []);

  const accept = () => {
    localStorage.setItem(STORAGE_KEY, 'accepted');
    setVisible(false);
  };

  const decline = () => {
    localStorage.setItem(STORAGE_KEY, 'declined');
    setVisible(false);
  };

  if (!visible) return null;

  return (
    <div className="fixed bottom-5 left-1/2 -translate-x-1/2 z-50 w-full max-w-lg px-4">
      <div className="bg-white border border-slate-200 rounded-xl shadow-lg px-5 py-4 flex flex-col sm:flex-row items-start sm:items-center gap-4">
        <div className="flex items-start gap-3 flex-1 min-w-0">
          <Cookie size={18} className="text-indigo-600 mt-0.5 shrink-0" />
          <p className="text-xs text-slate-600 leading-relaxed">
            We use cookies to improve your experience. By continuing, you agree to our{' '}
            <Link href="/privacy" className="text-indigo-600 hover:underline">Privacy Policy</Link>.
          </p>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <button onClick={decline} className="text-xs text-slate-500 hover:text-slate-700 px-3 py-1.5 rounded-lg hover:bg-slate-100 transition-colors">
            Decline
          </button>
          <button onClick={accept} className="bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-medium px-4 py-1.5 rounded-lg transition-colors">
            Accept
          </button>
        </div>
      </div>
    </div>
  );
}
