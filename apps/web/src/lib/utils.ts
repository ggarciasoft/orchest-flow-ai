import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Merges Tailwind CSS class names, resolving conflicts using tailwind-merge.
 * Wraps clsx for conditional class support.
 *
 * @param inputs - Any number of class values (strings, objects, arrays)
 * @returns A single merged class name string
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Returns Tailwind CSS color classes for a given workflow/execution status string.
 * Used to render colored status badges consistently across the UI.
 *
 * @param status - The status string (e.g. "completed", "running", "failed")
 * @returns Tailwind background + text color class string
 */
export function statusColor(status: string): string {
  switch (status.toLowerCase()) {
    case 'completed': return 'bg-green-100 text-green-800';
    case 'running': return 'bg-blue-100 text-blue-800';
    case 'paused': return 'bg-yellow-100 text-yellow-800';
    case 'failed': return 'bg-red-100 text-red-800';
    case 'cancelled': return 'bg-gray-100 text-gray-800';
    case 'queued': return 'bg-purple-100 text-purple-800';
    default: return 'bg-gray-100 text-gray-600';
  }
}

/**
 * Formats an ISO 8601 date string for display in the UI.
 * Returns an em-dash (—) if the value is null, undefined, or empty.
 *
 * @param iso - Optional ISO date string (e.g. "2024-01-15T10:30:00Z")
 * @returns Human-readable date/time string or "—" if no value
 */
export function formatDate(iso?: string) {
  if (!iso) return '—';
  return new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(iso));
}
