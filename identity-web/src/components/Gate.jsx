import React from 'react';
import { usePermissions } from '../hooks/usePermissions';

export const Gate = ({
  permission,      // صلاحية واحدة: 'USER_CREATE'
  permissions,     // عدة صلاحيات: ['USER_READ','USER_CREATE']
  requireAll = false, // true = يجب كل الصلاحيات, false = واحدة تكفي
  role,            // رول واحد: 'MANAGER' أو 'STUDENT' أو 'EMPLOYEE'
  roles,           // عدة رولات: ['MANAGER','ACCOUNTANT']
  fallback = null, // ماذا يظهر لو لا يملك صلاحية
  children,
}) => {
  const { hasPermission, hasPermissions, hasRole, hasRoles, isSuperAdmin } = usePermissions();

  // السوبر أدمن يفتح كل شيء - من جدول permissions
  if (isSuperAdmin) {
    return <>{children}</>;
  }

  let isAllowed = true;

  if (role && !hasRole(role)) {
    isAllowed = false;
  }

  if (roles && roles.length > 0 && !hasRoles(roles, requireAll)) {
    isAllowed = false;
  }

  if (permission && !hasPermission(permission)) {
    isAllowed = false;
  }

  if (permissions && permissions.length > 0 && !hasPermissions(permissions, requireAll)) {
    isAllowed = false;
  }

  if (!isAllowed) {
    return fallback;
  }

  return <>{children}</>;
};

export default Gate;