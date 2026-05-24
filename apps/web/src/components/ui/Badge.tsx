import { cn } from '@/lib/utils';

type BadgeVariant = 'default' | 'success' | 'warning' | 'danger' | 'info';

const variantStyles: Record<BadgeVariant, string> = {
  default:  'bg-slate-100 text-slate-600',
  success:  'bg-emerald-50 text-emerald-700',
  warning:  'bg-amber-50 text-amber-700',
  danger:   'bg-red-50 text-red-700',
  info:     'bg-indigo-50 text-indigo-700',
};

export function Badge({ variant = 'default', className, children }: {
  variant?: BadgeVariant;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <span className={cn('inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium', variantStyles[variant], className)}>
      {children}
    </span>
  );
}

/** Map execution/approval status string to Badge variant */
export function statusVariant(status: string): BadgeVariant {
  switch (status?.toLowerCase()) {
    case 'completed': case 'approved': return 'success';
    case 'failed': case 'rejected': return 'danger';
    case 'running': case 'processing': return 'info';
    case 'pending': return 'warning';
    default: return 'default';
  }
}
