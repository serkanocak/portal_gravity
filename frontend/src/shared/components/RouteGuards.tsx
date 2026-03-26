import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';

/** Redirects to /login if not authenticated */
export function PrivateRoute() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated());
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <Outlet />;
}

/** Redirects to /select-tenant if no tenant slug is set */
export function TenantRoute() {
  const tenantSlug = useAuthStore((s) => s.tenantSlug);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated());
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (!tenantSlug) {
    return <Navigate to="/select-tenant" replace />;
  }

  return <Outlet />;
}
