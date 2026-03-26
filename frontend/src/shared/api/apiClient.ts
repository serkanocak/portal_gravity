import axios from 'axios';
import { useAuthStore } from '../../store/authStore';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5146';

export const apiClient = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
});

// ── Request Interceptor — inject auth & tenant headers ──────
apiClient.interceptors.request.use((config) => {
  const { token, tenantSlug } = useAuthStore.getState();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  if (tenantSlug) {
    config.headers['X-Tenant-Id'] = tenantSlug;
  }

  return config;
});

// ── Response Interceptor — handle 401 & auto-refresh ────────
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const { refreshToken, setTokens, logout } = useAuthStore.getState();

        if (!refreshToken) {
          logout();
          return Promise.reject(error);
        }

        const { data } = await axios.post(`${API_BASE}/api/auth/refresh`, {
          refreshToken,
        });

        setTokens(data.accessToken, data.refreshToken);
        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
        return apiClient(originalRequest);
      } catch {
        useAuthStore.getState().logout();
      }
    }

    return Promise.reject(error);
  }
);
