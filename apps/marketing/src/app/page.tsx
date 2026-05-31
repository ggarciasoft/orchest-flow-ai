import { HomePage } from '@orchest-flow-ai/web-public';

export default function Page() {
  return (
    <HomePage
      navAuthSlot={
        <a
          href="mailto:ggarciasoft@gmail.com?subject=OrchestFlowAI%20Demo%20Request"
          className="text-sm text-slate-600 hover:text-slate-900 transition-colors"
        >
          Request a demo
        </a>
      }
    />
  );
}
