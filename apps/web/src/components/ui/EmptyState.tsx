import { cn } from '@/lib/utils';
import { LucideIcon } from 'lucide-react';

export function EmptyState({ icon: Icon, title, subtitle, action, className }: {
  icon: LucideIcon;
  title: string;
  subtitle?: string;
  action?: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={cn('flex flex-col items-center justify-center py-16 text-center', className)}>
      <Icon size={40} className="text-slate-300 mb-4" />
      <p className="text-sm font-medium text-slate-600">{title}</p>
      {subtitle && <p className="text-xs text-slate-400 mt-1">{subtitle}</p>}
      {action && <div className="mt-6">{action}</div>}
    </div>
  );
}
