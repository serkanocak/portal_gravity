import React from 'react';
import { useAuthStore } from '../store/authStore';
import { UserX, ShieldAlert } from 'lucide-react';
import { apiClient } from '../shared/api/apiClient';
import styles from './ImpersonationBanner.module.css';

export const ImpersonationBanner: React.FC = () => {
  const { user, setTokens } = useAuthStore();

  if (!user?.isImpersonated) return null;

  const handleStopImpersonation = async () => {
    try {
      const { data } = await apiClient.post('/api/auth/stop-impersonate');
      // Update with the original admin token
      setTokens(data.accessToken, data.refreshToken);
      // We'd typically decode JWT via a helper, or let app fetch `me()`. 
      // For now, reload window to wipe state and re-auth correctly.
      window.location.href = '/';
    } catch (err) {
      console.error('Failed to stop impersonation', err);
    }
  };

  return (
    <div className={styles.bannerContainer}>
      <div className={styles.bannerContent}>
        <div className={styles.info}>
          <ShieldAlert size={20} className={styles.icon} />
          <span>
            You are currently impersonating tenant: <strong>{user?.tenantId}</strong>
            {' '}by admin <strong>{user?.impersonatedBy}</strong>
          </span>
        </div>
        <button className={styles.stopButton} onClick={handleStopImpersonation}>
          <UserX size={16} />
          <span>Stop</span>
        </button>
      </div>
    </div>
  );
};
