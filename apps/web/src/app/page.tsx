import { HomePage } from '@orchest-flow-ai/web-public';
import Link from 'next/link';

export default function Page() {
  return (
    <HomePage
      navAuthSlot={
        <Link href="/login" className="text-sm text-slate-600 hover:text-slate-900 transition-colors">
          Sign in
        </Link>
      }
    />
  );
}
