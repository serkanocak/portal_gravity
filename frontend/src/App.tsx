import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from './store/authStore';
import { Login } from './pages/Login';
import { Layout } from './components/Layout';
import { Roles } from './pages/Roles';
import { Users } from './pages/Users';
import { AuditLogs } from './pages/AuditLogs';

// Simple Dashboard placeholder
const Dashboard: React.FC = () => (
  <div style={{ padding: '48px', color: '#f8fafc', maxWidth: '1400px', margin: '0 auto' }}>
    <h1 style={{ fontSize: '2rem', fontWeight: 700, marginBottom: '16px' }}>Welcome to Portal Gravity</h1>
    <p style={{ color: '#94a3b8', fontSize: '1.125rem' }}>Select an option from the sidebar to get started.</p>
  </div>
);

const PrivateRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated());
  return isAuthenticated ? <>{children}</> : <Navigate to="/" />;
};

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Login />} />

        {/* Protected Routes */}
        <Route
          path="/"
          element={
            <PrivateRoute>
              <Layout />
            </PrivateRoute>
          }
        >
          <Route path="dashboard" element={<Dashboard />} />
          <Route path="roles" element={<Roles />} />
          <Route path="users" element={<Users />} />
          <Route path="audit-logs" element={<AuditLogs />} />
        </Route>
        
        {/* Fallback */}
        <Route path="*" element={<Navigate to="/dashboard" />} />
      </Routes>
    </BrowserRouter>
  );
};

export default App;
