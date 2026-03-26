import React, { useEffect, useState } from 'react';
import { useTranslation } from '../shared/hooks/useTranslation';
import { apiClient } from '../shared/api/apiClient';
import { Shield, Plus, Edit, Trash2 } from 'lucide-react';
import styles from './Roles.module.css';

interface Role {
  id: string;
  name: string;
  description: string;
}

export const Roles: React.FC = () => {
  const { t } = useTranslation('rbac');
  const [roles, setRoles] = useState<Role[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchRoles();
  }, []);

  const fetchRoles = async () => {
    try {
      setIsLoading(true);
      const { data } = await apiClient.get<Role[]>('/api/roles');
      setRoles(data);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <div className={styles.titleSection}>
          <Shield className={styles.icon} size={32} />
          <div>
            <h1>{t('roles.title', 'Role Management')}</h1>
            <p>{t('roles.subtitle', 'Manage roles and assign permissions')}</p>
          </div>
        </div>
        <button className={styles.primaryBtn}>
          <Plus size={18} />
          {t('roles.create', 'Create Role')}
        </button>
      </header>

      <div className={styles.content}>
        {isLoading ? (
          <div className={styles.loader}>Loading roles...</div>
        ) : (
          <div className={styles.tableContainer}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>{t('roles.name', 'Role Name')}</th>
                  <th>{t('roles.description', 'Description')}</th>
                  <th className={styles.alignRight}>{t('roles.actions', 'Actions')}</th>
                </tr>
              </thead>
              <tbody>
                {roles.length === 0 ? (
                  <tr>
                    <td colSpan={3} className={styles.emptyState}>
                      No roles found.
                    </td>
                  </tr>
                ) : (
                  roles.map((role) => (
                    <tr key={role.id}>
                      <td className={styles.fw600}>{role.name}</td>
                      <td className={styles.textMuted}>{role.description}</td>
                      <td className={styles.actions}>
                        <button className={styles.iconBtn} title="Edit Role">
                          <Edit size={16} />
                        </button>
                        <button className={`${styles.iconBtn} ${styles.danger}`} title="Delete Role">
                          <Trash2 size={16} />
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
