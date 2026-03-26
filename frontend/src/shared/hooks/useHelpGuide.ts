import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../api/apiClient';

export interface HelpGuide {
  id: string;
  slug: string;
  title: string;
  contentHtml: string;
  updatedAt: string;
}

export function useHelpGuide(slug: string) {
  return useQuery({
    queryKey: ['helpGuide', slug],
    queryFn: async () => {
      const { data } = await apiClient.get<HelpGuide>(`/api/help-guides/${slug}`);
      return data;
    },
    staleTime: 5 * 60 * 1000, // 5 mins
  });
}
