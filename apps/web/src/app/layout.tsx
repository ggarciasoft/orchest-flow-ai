import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';
import { Providers } from '@/components/providers';
import { CookieBanner } from '@/components/CookieBanner';
const inter = Inter({ subsets: ['latin'] });
export const metadata: Metadata = { title: 'OrchestAI', description: 'Enterprise AI Workflow Platform' };
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className={inter.className}>
        <Providers>{children}</Providers>
        <CookieBanner />
      </body>
    </html>
  );
}
