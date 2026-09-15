import { useCallback, useMemo } from 'react';
import { useAuth } from '../context/AuthContext';

/**
 * usePermissions Hook (OPTIMIZED)
 * 
 * Performance Optimizations:
 * 1. permissionSet (Set) → O(1) lookups instead of O(n) .some()
 * 2. roleSet (Set) → O(1) lookups
 * 3. useCallback on all functions → Stable references
 * 4. useMemo on computed values → No recalculation
 * 5. loading flag → Prevent flash during user data load
 * 
 * @returns {Object} {
 *   user, permissions, roles, isSuperAdmin, loading,
 *   hasRole, hasRoles, hasPermission, hasPermissions
 * }
 */
export const usePermissions = () => {
  const { user, loading: authLoading } = useAuth();

  // ============================================================
  // Helper: Extract role/permission name from various shapes
  // ============================================================
  const getRoleName = useCallback((r) => {
    if (!r) return '';
    if (typeof r === 'string') return r;
    return r.role_name || r.roleName || r.name || r.display_name || '';
  }, []);

  const getPermName = useCallback((p) => {
    if (!p) return '';
    if (typeof p === 'string') return p;
    return p.permission_name || p.permissionName || p.name || '';
  }, []);

  // ============================================================
  // Extract raw arrays (memoized)
  // ============================================================
  const userPermissions = useMemo(
    () => user?.permissions || user?.permission_names || user?.perms || [],
    [user]
  );

  const userRoles = useMemo(
    () => user?.roles || user?.role_names || [],
    [user]
  );

  // ============================================================
  // ✅ OPTIMIZATION: Convert to Sets for O(1) lookups
  // ============================================================
  const permissionSet = useMemo(() => {
    return new Set(
      userPermissions.map(p => getPermName(p).toUpperCase()).filter(Boolean)
    );
  }, [userPermissions, getPermName]);

  const roleSet = useMemo(() => {
    return new Set(
      userRoles.map(r => getRoleName(r).toUpperCase()).filter(Boolean)
    );
  }, [userRoles, getRoleName]);

  // ============================================================
  // isSuperAdmin (memoized)
  // ============================================================
  const isSuperAdmin = useMemo(() => {
    return (
      permissionSet.has('SUPER_ADMIN') ||
      roleSet.has('SUPER_ADMIN') ||
      user?.is_super_admin === true
    );
  }, [permissionSet, roleSet, user]);

  // ============================================================
  // hasRole: O(1) check
  // ============================================================
  const hasRole = useCallback((role) => {
    if (!role) return true;
    if (isSuperAdmin) return true;
    return roleSet.has(role.toUpperCase());
  }, [isSuperAdmin, roleSet]);

  const hasRoles = useCallback((roles = [], requireAll = false) => {
    if (!roles?.length) return true;
    if (isSuperAdmin) return true;
    if (requireAll) return roles.every(r => roleSet.has(r.toUpperCase()));
    return roles.some(r => roleSet.has(r.toUpperCase()));
  }, [isSuperAdmin, roleSet]);

  // ============================================================
  // hasPermission: O(1) check
  // ============================================================
  const hasPermission = useCallback((permission) => {
    if (!permission) return true;
    if (isSuperAdmin) return true;
    return permissionSet.has(permission.toUpperCase());
  }, [isSuperAdmin, permissionSet]);

  const hasPermissions = useCallback((permissions = [], requireAll = false) => {
    if (!permissions?.length) return true;
    if (isSuperAdmin) return true;
    if (requireAll) return permissions.every(p => permissionSet.has(p.toUpperCase()));
    return permissions.some(p => permissionSet.has(p.toUpperCase()));
  }, [isSuperAdmin, permissionSet]);

  // ============================================================
  // ✅ NEW: loading flag (prevents flash)
  // ============================================================
  const loading = authLoading || !user;

  // ============================================================
  // Return Value (memoized)
  // ============================================================
  return useMemo(() => ({
    user,
    permissions: userPermissions,
    roles: userRoles,
    isSuperAdmin,
    loading,
    hasRole,
    hasRoles,
    hasPermission,
    hasPermissions,
  }), [
    user,
    userPermissions,
    userRoles,
    isSuperAdmin,
    loading,
    hasRole,
    hasRoles,
    hasPermission,
    hasPermissions,
  ]);
};

export default usePermissions;