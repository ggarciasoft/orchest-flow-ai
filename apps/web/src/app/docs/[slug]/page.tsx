import { notFound } from 'next/navigation';
import { docs } from '@/content/docs/index';
import { readFileSync } from 'fs';
import { join } from 'path';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

export function generateStaticParams() {
  return docs.map(d => ({ slug: d.slug }));
}

export default async function DocPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const doc = docs.find(d => d.slug === slug);
  if (!doc) notFound();

  // Read from the repo docs directory (works in dev + build)
  const docsDir = join(process.cwd(), '..', '..', 'docs');
  let content = '';
  try {
    content = readFileSync(join(docsDir, doc.filename), 'utf-8');
  } catch {
    content = `# ${doc.title}\n\nDocumentation coming soon.`;
  }

  return (
    <article className="bg-white border border-slate-200 rounded-xl px-8 py-8 prose prose-slate prose-sm max-w-none
      prose-headings:font-semibold prose-headings:text-slate-900
      prose-h1:text-xl prose-h2:text-base prose-h3:text-sm
      prose-p:text-slate-600 prose-p:leading-relaxed
      prose-code:bg-slate-100 prose-code:text-slate-800 prose-code:rounded prose-code:px-1 prose-code:text-xs
      prose-pre:bg-slate-50 prose-pre:text-slate-800 prose-pre:border prose-pre:border-slate-200 prose-pre:rounded-xl
      prose-a:text-indigo-600 prose-a:no-underline hover:prose-a:underline
      prose-strong:text-slate-900
      prose-li:text-slate-600
      prose-table:text-sm prose-th:text-slate-900 prose-td:text-slate-600
    ">
      <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
    </article>
  );
}
