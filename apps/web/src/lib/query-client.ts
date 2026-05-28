import { QueryClient } from '@tanstack/react-query';
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 0,           // always consider data stale — refetch on mount and focus
      refetchOnWindowFocus: true,  // refresh when user switches back to the tab
      refetchOnReconnect: true,    // refresh after reconnect
      retry: 1,
    },
  },
});
