import { PageHeader } from '@/components/ui';

export default function SettingsPage() {
  return (
    <div className="p-8">
      <PageHeader title="Settings" subtitle="Configure AI providers and platform settings" />
      <div className="bg-white border border-slate-200 rounded-xl text-slate-400 text-center">
        <div className="px-6 py-5 border-b border-slate-200 text-left">
          <p className="text-sm font-medium text-slate-900">Platform Configuration</p>
        </div>
        <div className="p-8">
          <p className="text-lg font-medium">Settings panel</p>
          <p className="text-sm mt-1">AI provider configuration, tenant settings, and user management coming in next phase.</p>
        </div>
      </div>
    </div>
  );
}
