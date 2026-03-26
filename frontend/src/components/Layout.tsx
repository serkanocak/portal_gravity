import React from 'react';
import { Outlet, NavLink } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { Shield, Users, Activity, LogOut, Hexagon } from 'lucide-react';
import { ImpersonationBanner } from './ImpersonationBanner';
import { LanguageSelector } from './LanguageSelector';
import { HelpPopup } from './HelpPopup';
import styles from './Layout.module.css';

export const Layout: React.FC = () => {
  const { user, logout } = useAuthStore();

  const handleLogout = () => {
    logout();
    window.location.href = '/';
  };

  const hasRole = (role: string) => user?.roles?.includes(role) || user?.roles?.includes('Admin');

  return (
    <div className={styles.layout}>
      <ImpersonationBanner />
      <div className={styles.mainContainer}>
        {/* Sidebar */}
        <aside className={styles.sidebar}>
          <div className={styles.brand}>
            <Hexagon className={styles.brandIcon} />
            <span>Portal Gravity</span>
          </div>

          <nav className={styles.nav}>
            <div className={styles.navGroup}>
              <div className={styles.navTitle}>Menu</div>
              <NavLink to="/dashboard" className={({isActive}) => isActive ? `${styles.navLink} ${styles.active}` : styles.navLink}>
                <Activity size={18} /> Dashboard
              </NavLink>

              {/* RBAC Role-Based Menu Hiding */}
              {hasRole('HR') || hasRole('Admin') ? (
                <NavLink to="/users" className={({isActive}) => isActive ? `${styles.navLink} ${styles.active}` : styles.navLink}>
                  <Users size={18} /> Staff Directory
                </NavLink>
              ) : null}

              {hasRole('Admin') && (
                <>
                  <NavLink to="/roles" className={({isActive}) => isActive ? `${styles.navLink} ${styles.active}` : styles.navLink}>
                    <Shield size={18} /> Role Management
                  </NavLink>
                  <NavLink to="/audit-logs" className={({isActive}) => isActive ? `${styles.navLink} ${styles.active}` : styles.navLink}>
                    <Activity size={18} /> Audit Logs
                  </NavLink>
                </>
              )}
            </div>
          </nav>
        </aside>

        {/* Content Area */}
        <div className={styles.contentArea}>
          {/* Header */}
          <header className={styles.header}>
            <div className={styles.headerLeft}>
              <h2 className={styles.pageTitle}>Workspace: {user?.tenantId}</h2>
              <HelpPopup slug="global-dashboard" />
            </div>
            
            <div className={styles.headerRight}>
              <LanguageSelector />
              
              <div className={styles.userMenu}>
                <div className={styles.avatar}>
                  {user?.email?.[0].toUpperCase() || 'U'}
                </div>
                <div className={styles.userInfo}>
                  <div className={styles.userEmail}>{user?.email}</div>
                  <div className={styles.userRoles}>{user?.roles?.join(', ')}</div>
                </div>
                <button className={styles.iconBtn} onClick={handleLogout} title="Logout">
                  <LogOut size={16} />
                </button>
              </div>
            </div>
          </header>

          {/* Main Outlet for nested routes */}
          <main className={styles.mainContent}>
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  );
};
