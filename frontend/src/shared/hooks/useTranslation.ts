import { useQuery } from '@tanstack/react-query';
import { useLocaleStore } from '../../store/localeStore';
import { apiClient } from '../api/apiClient';

export function useTranslation(namespace: string) {
  const { locale } = useLocaleStore();

  const { data: translations, isLoading } = useQuery({
    queryKey: ['translations', locale, namespace],
    queryFn: async () => {
      const response = await apiClient.get<Record<string, string>>(`/api/i18n/${namespace}/${locale}`);
      return response.data;
    },
    staleTime: 60 * 60 * 1000, // 1 hour
  });

  const t = (key: string, defaultValue?: string): string => {
    if (isLoading) return '...';
    if (translations && translations[key]) {
      return translations[key];
    }
    return defaultValue ?? `[${key}]`;
  };

  return { t, isLoading };
}
