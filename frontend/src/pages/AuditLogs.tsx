import React, { useEffect, useState } from 'react';
import { Activity, Download, Filter } from 'lucide-react';
import { apiClient } from '../shared/api/apiClient';
import styles from './AuditLogs.module.css';

interface AuditLog {
  id: string;
  timestamp: string;
  userId: string;
  action: string;
  resource: string;
  ipAddress: string;
  result: string;
}

export const AuditLogs: React.FC = () => {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchLogs();
  }, []);

  const fetchLogs = async () => {
    setIsLoading(true);
    try {
      const response = await apiClient.get<any>('/api/audit/logs');
      // The API returns { data: AuditLogEntity[], total: number }
      // Axios puts the body in response.data
      setLogs(response.data.data || []);
    } catch (err) {
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleExportCSV = () => {
    // Basic CSV export logic
    const header = ['Timestamp', 'User', 'Action', 'Resource', 'IP', 'Result'];
    const rows = logs.map(log => [
      log.timestamp, log.userId, log.action, log.resource, log.ipAddress, log.result
    ]);
    const csvContent = "data:text/csv;charset=utf-8," 
      + [header.join(','), ...rows.map(e => e.join(','))].join("\n");
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", "audit_logs_export.csv");
    document.body.appendChild(link); // Required for FF
    link.click();
    link.remove();
  };

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <div className={styles.titleSection}>
          <Activity className={styles.icon} size={32} />
          <div>
            <h1>Audit Logs</h1>
            <p>Track all user and system activities</p>
          </div>
        </div>
        <div className={styles.actions}>
          <button className={styles.secondaryBtn}>
            <Filter size={16} /> Filter
          </button>
          <button className={styles.primaryBtn} onClick={handleExportCSV}>
            <Download size={16} /> Export CSV
          </button>
        </div>
      </header>

      <div className={styles.tableContainer}>
        {isLoading ? (
          <div className={styles.loader}>Loading audit logs...</div>
        ) : (
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>User</th>
                <th>Action</th>
                <th>Resource</th>
                <th>IP Address</th>
                <th>Result</th>
              </tr>
            </thead>
            <tbody>
              {logs.length === 0 ? (
                <tr>
                  <td colSpan={6} className={styles.emptyState}>No activity found.</td>
                </tr>
              ) : (
                logs.map((log) => (
                  <tr key={log.id}>
                    <td className={styles.timestamp}>
                      {new Date(log.timestamp).toLocaleString()}
                    </td>
                    <td className={styles.fw500}>{log.userId}</td>
                    <td>
                      <span className={styles.badgeLine}>{log.action}</span>
                    </td>
                    <td className={styles.textMuted}>{log.resource}</td>
                    <td className={styles.code}>{log.ipAddress}</td>
                    <td>
                      <span className={`${styles.statusBadge} ${log.result.toLowerCase() === 'success' ? styles.success : styles.error}`}>
                        {log.result}
                      </span>
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
