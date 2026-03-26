import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface User {
  id: string;
  email: string;
  tenantId: string;
  roles: string[];
  isImpersonated: boolean;
  impersonatedBy?: string;
}

interface AuthState {
  token: string | null;
  refreshToken: string | null;
  tenantSlug: string | null;
  user: User | null;

  // Actions
  setTokens: (token: string, refreshToken: string) => void;
  setUser: (user: User) => void;
  setTenantSlug: (slug: string) => void;
  logout: () => void;
  isAuthenticated: () => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      token: null,
      refreshToken: null,
      tenantSlug: null,
      user: null,

      setTokens: (token, refreshToken) => set({ token, refreshToken }),
      setUser: (user) => set({ user }),
      setTenantSlug: (tenantSlug) => set({ tenantSlug }),
      logout: () =>
        set({ token: null, refreshToken: null, user: null, tenantSlug: null }),
      isAuthenticated: () => !!get().token && !!get().user,
    }),
    {
      name: 'portal-gravity-auth',
      partialize: (state) => ({
        token: state.token,
        refreshToken: state.refreshToken,
        tenantSlug: state.tenantSlug,
        user: state.user,
      }),
    }
  )
);
