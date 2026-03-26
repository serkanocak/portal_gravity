import React, { useState } from 'react';
import { useAuthStore } from '../store/authStore';
import { useTranslation } from '../shared/hooks/useTranslation';
import { LanguageSelector } from '../components/LanguageSelector';
import { apiClient } from '../shared/api/apiClient';
import { Rocket, Loader2 } from 'lucide-react';
import { Navigate } from 'react-router-dom';
import styles from './Login.module.css';

function parseJwtPayload(token: string) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(base64));
  } catch {
    return null;
  }
}

export const Login: React.FC = () => {
  const { t } = useTranslation('auth');
  const setTokens = useAuthStore((s) => s.setTokens);
  const setUser = useAuthStore((s) => s.setUser);
  const setTenantSlug = useAuthStore((s) => s.setTenantSlug);
  
  const [tenant, setTenant] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');
    
    try {
      setTenantSlug(tenant);
      const { data } = await apiClient.post('/api/auth/login', {
        tenantSlug: tenant,
        email,
        password,
      });

      setTokens(data.accessToken, data.refreshToken);

      // Decode JWT to extract user info for auth store
      const payload = parseJwtPayload(data.accessToken);
      if (payload) {
        // Handle variations in JWT claim names (.NET often uses long URIs)
        const rolesKey = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
        const rawRoles = payload.role || payload[rolesKey] || [];
        const roles = Array.isArray(rawRoles) ? rawRoles : [rawRoles];
        
        setUser({
          id: payload.sub || payload.nameid || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || '',
          email: payload.email || email,
          tenantId: payload.tenantId || '',
          roles: roles,
          isImpersonated: !!payload.impersonatedBy,
          impersonatedBy: payload.impersonatedBy || undefined,
        });
      }

      window.location.href = '/dashboard';
    } catch (err: any) {
      setError(err.response?.data?.message || t('login.error', 'Login failed'));
    } finally {
      setIsLoading(false);
    }
  };

  // If already authenticated, redirect
  if (useAuthStore.getState().isAuthenticated()) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <div className={styles.loginPage}>
      <div className={styles.navbar}>
        <div className={styles.logo}>
          <Rocket className={styles.icon} />
          <span>Portal Gravity</span>
        </div>
        <LanguageSelector />
      </div>

      <div className={styles.cardContainer}>
        <div className={styles.glassCard}>
          <div className={styles.header}>
            <h2>{t('login.title', 'Welcome Back')}</h2>
            <p>{t('login.subtitle', 'Sign in to your workspace')}</p>
          </div>
          
          <form className={styles.form} onSubmit={handleLogin}>
            {error && <div className={styles.errorAlert}>{error}</div>}
            
            <div className={styles.formGroup}>
              <label>{t('login.tenantId', 'Workspace URL')}</label>
              <div className={styles.inputWrapper}>
                <input 
                  type="text" 
                  placeholder="acme" 
                  value={tenant}
                  onChange={(e) => setTenant(e.target.value)}
                  required 
                />
                <span className={styles.suffix}>.portalgravity.com</span>
              </div>
            </div>

            <div className={styles.formGroup}>
              <label>{t('login.email', 'Email Address')}</label>
              <input 
                type="email" 
                placeholder="john@example.com" 
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>

            <div className={styles.formGroup}>
              <div className={styles.labelRow}>
                <label>{t('login.password', 'Password')}</label>
                <a href="#" className={styles.forgotPass}>
                  {t('login.forgot_password', 'Forgot password?')}
                </a>
              </div>
              <input 
                type="password" 
                placeholder="••••••••" 
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required 
              />
            </div>

            <button type="submit" className={styles.submitBtn} disabled={isLoading}>
              {isLoading ? <Loader2 className={styles.spinner} /> : t('login.submit', 'Sign In')}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};
