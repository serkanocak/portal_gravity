import React, { useEffect, useState } from 'react';
import { Users as UsersIcon, MoreHorizontal, CheckCircle, XCircle } from 'lucide-react';
import { apiClient } from '../shared/api/apiClient';
import styles from './Users.module.css';

interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  isActive: boolean;
  departmentId?: string;
  departmentName?: string;
  roles?: string[];
}

export const Users: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    setIsLoading(true);
    try {
      // Mocked endpoint per Phase 1 specs
      const { data } = await apiClient.get<User[]>('/api/org/users');
      setUsers(data);
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
          <UsersIcon className={styles.icon} size={32} />
          <div>
            <h1>Staff & Assignments</h1>
            <p>Manage users, departments, and active statuses</p>
          </div>
        </div>
        <div className={styles.actions}>
          <div className={styles.quickInvite}>
            <input 
              id="user-email-input"
              type="email" 
              placeholder="Invite by Email..." 
              className={styles.input} 
              onKeyDown={async (e) => {
                if (e.key === 'Enter') {
                  const email = (e.target as HTMLInputElement).value;
                  if (!email) return;
                  try {
                    await apiClient.post('/api/org/users/invite', { email });
                    (e.target as HTMLInputElement).value = '';
                    fetchUsers();
                  } catch (err: any) {
                    alert(err.response?.data || 'Error inviting user');
                  }
                }
              }}
            />
          </div>
        </div>
      </header>

      <div className={styles.tableContainer}>
        {isLoading ? (
          <div className={styles.loader}>Loading users...</div>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>User</th>
                <th>Status</th>
                <th>Department</th>
                <th>Roles</th>
                <th className={styles.alignRight}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.length === 0 ? (
                <tr>
                  <td colSpan={5} className={styles.emptyState}>No users found.</td>
                </tr>
              ) : (
                users.map((user) => (
                  <tr key={user.id}>
                    <td>
                      <div className={styles.userInfo}>
                        <div className={styles.avatar}>
                          {user.email[0].toUpperCase()}
                        </div>
                        <div>
                          <div className={styles.fw600}>{user.firstName} {user.lastName}</div>
                          <div className={styles.textMuted}>{user.email}</div>
                        </div>
                      </div>
                    </td>
                    <td>
                      {user.isActive ? (
                        <span className={`${styles.statusBadge} ${styles.active}`}>
                          <CheckCircle size={12} /> Active
                        </span>
                      ) : (
                        <span className={`${styles.statusBadge} ${styles.inactive}`}>
                          <XCircle size={12} /> Inactive
                        </span>
                      )}
                    </td>
                    <td>
                      <span className={styles.departmentBadge}>
                        {user.departmentName || 'Unassigned'}
                      </span>
                    </td>
                    <td>
                      <div className={styles.roleList}>
                        {user.roles?.map(r => (
                          <span key={r} className={styles.roleBadge}>{r}</span>
                        ))}
                      </div>
                    </td>
                    <td className={styles.actionsCell}>
                      <button className={styles.iconBtn}>
                        <MoreHorizontal size={18} />
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};
