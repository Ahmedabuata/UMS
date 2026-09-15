import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import permissionsApi from '../api/permissions.api';
import axiosInstance from '../api/axiosInstance';
import { handleApiError } from '../utils/handleApiError';
import { useDebounce } from '../hooks/useDebounce';
import { Toast } from '../components/Toast';
import Gate from '../components/Gate';

// ============================================================
// Available Actions
// ============================================================
const AVAILABLE_ACTIONS = [
  'CREATE', 'READ', 'UPDATE', 'DELETE', 'WRITE',
  'SEARCH', 'EXPORT', 'IMPORT',
  'MANAGE_ROLES', 'MANAGE_GROUPS', 'MANAGE_PERMISSIONS',
  'STATUS', 'TOGGLE_STATUS',
  'VIEW', 'ADMIN',
];

// ============================================================
// ✅ Protected Permissions (cannot be deleted)
// ============================================================
const PROTECTED_PERMISSIONS = ['SUPER_ADMIN', 'SUPER_USER'];

// ============================================================
// Helper: extract error message
// ============================================================
const extractErrorMessage = (errRes, defaultMsg = 'An unexpected error occurred') => {
  if (!errRes) return defaultMsg;
  if (typeof errRes === 'string') return errRes;
  if (typeof errRes === 'object') {
    for (const v of Object.values(errRes)) {
      if (typeof v === 'string') return v;
      if (Array.isArray(v) && v[0]) return v[0];
    }
  }
  return defaultMsg;
};

// ============================================================
// ✅ NEW Helper: Format permission name for display
//    "AUDIT_ARCHIVE_MANAGE" → "AUDIT ARCHIVE MANAGE"
// ============================================================
const formatPermissionName = (permCode) => {
  if (!permCode) return '-';
  return String(permCode).replace(/_/g, ' ');
};

// ============================================================
// Helper: Generate Permission Name
// ============================================================
const generatePermissionName = (displayName, action) => {
  if (!displayName || !action) return '';
  const cleaned = displayName.trim().replace(/\s+/g, '_').replace(/[^a-zA-Z0-9_]/g, '');
  return `${cleaned}_${action.trim().toUpperCase()}`;
};

// ============================================================
// Helper: Generate Final Display Name
// ============================================================
const generateDisplayName = (userDisplayName, action) => {
  if (!userDisplayName || !action) return userDisplayName || '';
  return `${userDisplayName.trim()} ${action.trim().toUpperCase()}`;
};

// ============================================================
// Helper: Generate Module Code
// ============================================================
const generateModuleCode = (displayName) => {
  if (!displayName) return '';
  const firstWord = displayName.trim().split(/\s+/)[0] || '';
  return firstWord.toUpperCase().replace(/[^A-Z0-9]/g, '');
};

const Permissions = () => {
  const { t } = useTranslation();
  const [permissions, setPermissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [limit] = useState(10);
  const [total, setTotal] = useState(0);
  const [toast, setToast] = useState(null);

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedPermission, setSelectedPermission] = useState(null);
  const [permissionToDelete, setPermissionToDelete] = useState(null);
  const [togglingId, setTogglingId] = useState(null);
  const [togglingSensitiveId, setTogglingSensitiveId] = useState(null);
  const [deleting, setDeleting] = useState(false);

  const [formData, setFormData] = useState({
    userDisplayName: '',
    action: '',
    permission_name: '',
    display_name: '',
    module_code: '',
    branch_code: '',
    description: '',
    is_active: true,
    is_sensitive: false,
    useCustomAction: false,
    customAction: '',
  });

  const [formErrors, setFormErrors] = useState({});
  const [submitError, setSubmitError] = useState(null);
  const [creating, setCreating] = useState(false);
  const [showNewBranchInput, setShowNewBranchInput] = useState(false);

  const debouncedSearch = useDebounce(search, 500);

  const existingBranchCodes = useMemo(() => {
    const codes = permissions
      .map((p) => p.branch_code || p.branchCode || p.BranchCode)
      .filter(Boolean)
      .map((c) => String(c).trim().toUpperCase())
      .filter(Boolean);
    return [...new Set(codes)].sort();
  }, [permissions]);

  // ============================================================
  // Helpers
  // ============================================================
  const getPermCode = (p) => {
    if (!p) return '';
    const raw = p.permission_name ?? p.PermissionName ?? p.permissionName ?? p.name ?? p.code ?? '';
    return String(raw).trim().toUpperCase();
  };

  const getModule = (p) => {
    if (!p) return '-';
    const mod = p.module ?? p.Module ?? p.module_code ?? getPermCode(p).split('_')[0] ?? '-';
    return String(mod).trim().toUpperCase();
  };

  const getDisplayName = (p) => {
    if (!p) return '-';
    return p.display_name || p.displayName || p.DisplayName || '-';
  };

  // ✅ Get display name with fallback to formatted permission_name
  const getDisplayNameWithFallback = (p) => {
    if (!p) return '-';
    const displayName = p.display_name || p.displayName || p.DisplayName || '';
    
    // ✅ If display_name is empty → format from permission_name
    if (!displayName || displayName.trim() === '' || displayName === '-') {
      return formatPermissionName(getPermCode(p));
    }
    
    return displayName;
  };

  const getBranchCode = (p) => {
    if (!p) return '-';
    return p.branch_code || p.branchCode || p.BranchCode || '-';
  };

  const isActive = (p) => p?.is_active ?? p?.isActive ?? true;
  const isSensitive = (p) => p?.is_sensitive ?? p?.isSensitive ?? false;

  // ✅ Helper: Check if permission is protected
  const isProtected = (p) => {
    const code = getPermCode(p);
    return PROTECTED_PERMISSIONS.includes(code);
  };

  // ============================================================
  // fetchPermissions
  // ============================================================
  const fetchPermissions = useCallback(async () => {
    try {
      setLoading(true);
      const params = { page, limit };
      if (debouncedSearch) params.search = debouncedSearch;

      const res = await permissionsApi.getAll(params);
      const body = res?.data?.data ?? res?.data ?? res;
      let list = Array.isArray(body)
        ? body
        : body?.permissions || body?.items || body?.data || [];

      list = list.filter(Boolean).filter((p) => getPermCode(p));

      setPermissions(list);
      setTotal(Number(body?.total || body?.totalCount || list.length));
    } catch (err) {
      setPermissions([]);
      const errRes = handleApiError(err, 'permissions.fetchError');
      setToast({ type: 'error', message: extractErrorMessage(errRes, t('permissions.fetchError')) });
    } finally {
      setLoading(false);
    }
  }, [page, limit, debouncedSearch, t]);

  useEffect(() => { fetchPermissions(); }, [fetchPermissions]);
  useEffect(() => { setPage(1); }, [debouncedSearch]);

  // ============================================================
  // handleToggleActive
  // ✅ UPDATED: Different Toast for deactivate (info) vs activate (success)
  // ============================================================
  const handleToggleActive = async (permission) => {
    const permissionId = permission.id;
    const currentActive = isActive(permission);
    const newStatus = !currentActive;

    setTogglingId(permissionId);
    try {
      await axiosInstance.patch(`/permissions/${permissionId}/status`, {
        isActive: newStatus,
      });

      setPermissions((prev) =>
        prev.map((p) => {
          if (p.id === permissionId) {
            return { ...p, is_active: newStatus, isActive: newStatus };
          }
          return p;
        })
      );

      // ✅ Show different message based on action
      if (!newStatus) {
        // Deactivated → warn user about potential re-login
        setToast({
          type: 'info',
          message: t(
            'permissions.deactivateSuccessWithLogout',
            'Permission deactivated. You may need to re-login.'
          ),
        });
      } else {
        // Activated → normal success
        setToast({
          type: 'success',
          message: t('permissions.activateSuccess', 'Permission activated'),
        });
      }
    } catch (err) {
      const errRes = handleApiError(err, 'permissions.toggleError');
      setToast({
        type: 'error',
        message: extractErrorMessage(errRes, t('permissions.toggleError', 'Failed to toggle status')),
      });
      fetchPermissions();
    } finally {
      setTogglingId(null);
    }
  };

  // ============================================================
  // handleToggleSensitive
  // ============================================================
  const handleToggleSensitive = async (permission) => {
    const permissionId = permission.id;
    const currentSensitive = isSensitive(permission);
    const newStatus = !currentSensitive;

    setTogglingSensitiveId(permissionId);
    try {
      await axiosInstance.patch(`/permissions/${permissionId}/sensitivity`, {
        isSensitive: newStatus,
      });

      setPermissions((prev) =>
        prev.map((p) => {
          if (p.id === permissionId) {
            return { ...p, is_sensitive: newStatus, isSensitive: newStatus };
          }
          return p;
        })
      );

      setToast({
        type: 'success',
        message: newStatus
          ? t('permissions.markSensitiveSuccess', 'Marked as sensitive')
          : t('permissions.unmarkSensitiveSuccess', 'Unmarked as sensitive'),
      });
    } catch (err) {
      const errRes = handleApiError(err, 'permissions.toggleSensitiveError');
      setToast({
        type: 'error',
        message: extractErrorMessage(errRes, t('permissions.toggleSensitiveError', 'Failed to toggle sensitivity')),
      });
      fetchPermissions();
    } finally {
      setTogglingSensitiveId(null);
    }
  };

  // ============================================================
  // handleDeleteConfirm
  // ============================================================
  const handleDeleteConfirm = async () => {
    if (!permissionToDelete) return;

    // ✅ Frontend protection check
    if (isProtected(permissionToDelete)) {
      setToast({
        type: 'error',
        message: t('permissions.cannotDeleteProtected', `Cannot delete ${getPermCode(permissionToDelete)}`),
      });
      setIsDeleteModalOpen(false);
      setPermissionToDelete(null);
      return;
    }

    setDeleting(true);
    try {
      await axiosInstance.delete(`/permissions/${permissionToDelete.id}`);

      setToast({
        type: 'success',
        message: t('permissions.deleteSuccess', 'Permission deleted successfully'),
      });
      setIsDeleteModalOpen(false);
      setPermissionToDelete(null);
      fetchPermissions();
    } catch (err) {
      const errRes = handleApiError(err, 'permissions.deleteError');
      setToast({
        type: 'error',
        message: extractErrorMessage(errRes, t('permissions.deleteError', 'Failed to delete permission')),
      });
    } finally {
      setDeleting(false);
    }
  };

  // ============================================================
  // openDeleteModal
  // ============================================================
  const openDeleteModal = (permission) => {
    if (isProtected(permission)) {
      setToast({
        type: 'error',
        message: t('permissions.cannotDeleteProtected', `Cannot delete ${getPermCode(permission)}`),
      });
      return;
    }
    setPermissionToDelete(permission);
    setIsDeleteModalOpen(true);
  };

  // ============================================================
  // Handlers
  // ============================================================
  const handleUserDisplayNameChange = (e) => {
    const userDisplayName = e.target.value;
    setFormData((prev) => {
      const action = prev.action;
      return {
        ...prev,
        userDisplayName,
        permission_name: action ? generatePermissionName(userDisplayName, action) : '',
        display_name: action ? generateDisplayName(userDisplayName, action) : userDisplayName,
        module_code: generateModuleCode(userDisplayName),
      };
    });
  };

  const handleActionSelect = (e) => {
    const value = e.target.value;

    if (value === '__new__') {
      setFormData((prev) => ({
        ...prev,
        useCustomAction: true,
        action: '',
        permission_name: '',
        display_name: prev.userDisplayName,
      }));
    } else {
      setFormData((prev) => ({
        ...prev,
        useCustomAction: false,
        action: value,
        customAction: '',
        permission_name: generatePermissionName(prev.userDisplayName, value),
        display_name: generateDisplayName(prev.userDisplayName, value),
      }));
    }
  };

  const handleCustomActionChange = (e) => {
    const customAction = e.target.value;
    const action = customAction
      .trim()
      .toUpperCase()
      .replace(/\s+/g, '_')
      .replace(/[^A-Z0-9_]/g, '');
    setFormData((prev) => ({
      ...prev,
      customAction,
      action,
      permission_name: generatePermissionName(prev.userDisplayName, action),
      display_name: generateDisplayName(prev.userDisplayName, action),
    }));
  };

  const openCreateModal = () => {
    setFormData({
      userDisplayName: '',
      action: '',
      permission_name: '',
      display_name: '',
      module_code: '',
      branch_code: '',
      description: '',
      is_active: true,
      is_sensitive: false,
      useCustomAction: false,
      customAction: '',
    });
    setFormErrors({});
    setSubmitError(null);
    setShowNewBranchInput(false);
    setIsCreateModalOpen(true);
  };

  const closeCreateModal = () => {
    setIsCreateModalOpen(false);
    setSubmitError(null);
  };

  const handleCreate = async () => {
    const errors = {};
    if (!formData.userDisplayName?.trim()) errors.userDisplayName = t('validation.required');
    if (!formData.action?.trim()) errors.action = t('validation.required');

    setFormErrors(errors);
    if (Object.keys(errors).length > 0) return;

    const finalName = formData.permission_name.trim();
    const finalDisplayName = formData.display_name.trim();

    if (!/^[A-Za-z][A-Za-z0-9_]*_[A-Z][A-Z0-9_]*$/.test(finalName)) {
      setFormErrors({ permission_name: t('permissions.invalidFormat') });
      return;
    }

    if (permissions.some((p) => getPermCode(p) === finalName.toUpperCase())) {
      setFormErrors({ permission_name: t('permissions.alreadyExists') });
      return;
    }

    try {
      setCreating(true);
      setSubmitError(null);

      await permissionsApi.create({
        permission_name: finalName,
        PermissionName: finalName,
        display_name: finalDisplayName,
        displayName: finalDisplayName,
        module: formData.userDisplayName.trim(),
        module_code: formData.module_code.trim().toUpperCase(),
        branch_code: formData.branch_code?.trim().toUpperCase() || null,
        description: formData.description?.trim() || null,
        is_active: formData.is_active,
        is_sensitive: formData.is_sensitive,
      });

      setToast({ type: 'success', message: t('permissions.createSuccess') });
      closeCreateModal();
      fetchPermissions();
    } catch (err) {
      const errRes = handleApiError(err, 'permissions.createError');
      const errorMessage = extractErrorMessage(errRes, t('permissions.createError'));

      const lowerMsg = errorMessage.toLowerCase();
      if (lowerMsg.includes('permission') || lowerMsg.includes('name')) {
        setFormErrors((prev) => ({ ...prev, permission_name: errorMessage }));
      } else if (lowerMsg.includes('branch')) {
        setFormErrors((prev) => ({ ...prev, branch_code: errorMessage }));
      } else {
        setSubmitError(errorMessage);
      }
    } finally {
      setCreating(false);
    }
  };

  const totalPages = Math.ceil(total / limit) || 1;

  // ============================================================
  // Render
  // ============================================================
  return (
    <div className="space-y-6 text-[var(--text-main)] p-6 bg-[var(--bg-primary)] min-h-screen">
      {toast && <Toast type={toast.type} message={toast.message} onClose={() => setToast(null)} />}

      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <h1 className="text-2xl font-bold text-[var(--text-main)]">{t('permissions.title')}</h1>
        <Gate permission="PERMISSION_WRITE">
          <button
            onClick={openCreateModal}
            className="px-4 py-2 rounded-xl bg-[var(--accent-color)] text-[var(--bg-primary)] text-sm font-medium hover:opacity-90 transition-opacity"
          >
            {t('permissions.addNew')}
          </button>
        </Gate>
      </div>

      {/* Search */}
      <Gate permission="PERMISSION_SEARCH">
        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl p-4 flex">
          <input
            placeholder={t('common.search')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="px-4 py-2 w-full md:max-w-md rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm text-[var(--text-main)] placeholder:text-[var(--text-muted)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]/20 focus:border-[var(--accent-color)]"
          />
        </div>
      </Gate>

      {/* Table */}
      <Gate
        permission="PERMISSION_READ"
        fallback={
          <div className="p-10 text-center text-sm text-[var(--text-muted)]">
            {t('common.noPermission')}
          </div>
        }
      >
        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-sm">
              <thead>
                <tr className="border-b border-[var(--border-color)] bg-[var(--bg-primary)]/50 text-[var(--text-muted)] text-xs uppercase tracking-wider">
                  <th className="p-4 font-semibold">{t('permissions.name')}</th>
                  <th className="p-4 font-semibold">{t('permissions.displayName')}</th>
                  <th className="p-4 font-semibold">{t('permissions.module')}</th>
                  <th className="p-4 font-semibold">{t('permissions.branch')}</th>
                  <th className="p-4 font-semibold text-center">{t('permissions.isActive')}</th>
                  <th className="p-4 font-semibold text-center">{t('permissions.isSensitive')}</th>
                  <th className="p-4 hidden sm:table-cell font-semibold">
                    {t('permissions.description')}
                  </th>
                  <th className="p-4 text-center font-semibold">{t('common.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--border-color)]">
                {loading ? (
                  <tr>
                    <td colSpan="8" className="p-6 text-center text-[var(--text-muted)]">
                      {t('common.loading')}
                    </td>
                  </tr>
                ) : permissions.length === 0 ? (
                  <tr>
                    <td colSpan="8" className="p-6 text-center text-[var(--text-muted)]">
                      {t('permissions.noData')}
                    </td>
                  </tr>
                ) : (
                  permissions.map((p) => {
                    const active = isActive(p);
                    const sensitive = isSensitive(p);
                    const isToggling = togglingId === p.id;
                    const isTogglingSensitive = togglingSensitiveId === p.id;
                    const protectedPerm = isProtected(p);

                    return (
                      <tr
                        key={p.id || p._id || getPermCode(p)}
                        className={`transition-colors ${
                          active
                            ? 'hover:bg-[var(--bg-primary)]/40'
                            : 'bg-red-500/5 hover:bg-red-500/10 opacity-70'
                        }`}
                      >
                        <td className="p-4">
                          <div className="flex items-center gap-2">
                            <span className="inline-flex items-center px-3 py-1 rounded-full bg-[var(--accent-color)]/10 border border-[var(--accent-color)]/20 text-[var(--accent-color)] font-mono font-bold text-xs tracking-wide uppercase">
                              {getPermCode(p)}
                            </span>
                            {protectedPerm && (
                              <span className="text-[10px] px-2 py-0.5 rounded-full bg-amber-500/20 text-amber-500 font-semibold uppercase">
                                🔒
                              </span>
                            )}
                          </div>
                        </td>
                        <td className="p-4 text-[var(--text-main)] font-medium">
                          {getDisplayNameWithFallback(p)}
                        </td>
                        <td className="p-4">
                          <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-muted)] text-xs font-medium uppercase">
                            {getModule(p)}
                          </span>
                        </td>
                        <td className="p-4">
                          <span className="inline-flex items-center px-2.5 py-1 rounded-full bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-muted)] text-xs font-medium uppercase">
                            {getBranchCode(p)}
                          </span>
                        </td>

                        {/* Is Active */}
                        <td className="p-4 text-center">
                          <span
                            className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border ${
                              active
                                ? 'bg-green-500/10 border-green-500/20 text-green-500'
                                : 'bg-red-500/10 border-red-500/20 text-red-400'
                            }`}
                          >
                            {active ? t('common.active') : t('common.inactive')}
                          </span>
                        </td>

                        {/* Is Sensitive (Interactive) */}
                        <td className="p-4 text-center">
                          <Gate permission="PERMISSION_UPDATE">
                            <button
                              onClick={() => handleToggleSensitive(p)}
                              disabled={isTogglingSensitive}
                              title={
                                sensitive
                                  ? t('permissions.unmarkSensitive', 'Unmark as sensitive')
                                  : t('permissions.markSensitive', 'Mark as sensitive')
                              }
                              className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border transition-all whitespace-nowrap disabled:opacity-30 ${
                                sensitive
                                  ? 'bg-red-500/10 border-red-500/20 text-red-400 hover:bg-red-500/20'
                                  : 'bg-[var(--bg-primary)] border-[var(--border-color)] text-[var(--text-muted)] hover:bg-[var(--accent-color)]/10'
                              }`}
                            >
                              {isTogglingSensitive
                                ? '...'
                                : sensitive
                                ? '🔒 YES'
                                : 'NO'}
                            </button>
                          </Gate>
                        </td>

                        <td className="p-4 text-[var(--text-muted)] truncate max-w-xs hidden sm:table-cell">
                          {p.description || p.Description || '-'}
                        </td>

                        <td className="p-4 text-center">
                          <div className="flex items-center justify-center gap-2 flex-wrap">
                            <Gate permission="PERMISSION_UPDATE">
                              <button
                                onClick={() => handleToggleActive(p)}
                                disabled={isToggling}
                                className={`text-xs px-3 py-1.5 rounded-full border transition-all whitespace-nowrap disabled:opacity-30 ${
                                  active
                                    ? 'bg-amber-500/10 border-amber-500/20 text-amber-500 hover:bg-amber-500/20'
                                    : 'bg-green-500/10 border-green-500/20 text-green-500 hover:bg-green-500/20'
                                }`}
                              >
                                {isToggling
                                  ? '...'
                                  : active
                                  ? t('permissions.deactivate')
                                  : t('permissions.activate')}
                              </button>
                            </Gate>

                            <button
                              onClick={() => {
                                setSelectedPermission(p);
                                setIsDetailsModalOpen(true);
                              }}
                              className="text-xs px-3 py-1.5 rounded-full bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-muted)] hover:bg-[var(--accent-color)]/10 hover:text-[var(--accent-color)] hover:border-[var(--accent-color)]/20 transition-colors"
                            >
                              {t('permissions.viewDetails')}
                            </button>

                            {/* Delete Button */}
                            <Gate permission="PERMISSION_DELETE">
                              <button
                                onClick={() => openDeleteModal(p)}
                                disabled={protectedPerm}
                                title={
                                  protectedPerm
                                    ? t('permissions.cannotDeleteProtected', 'Cannot delete protected permission')
                                    : ''
                                }
                                className="text-xs px-3 py-1.5 rounded-full bg-red-500/10 text-red-400 border border-red-500/20 hover:bg-red-500/20 transition-all whitespace-nowrap disabled:opacity-30 disabled:cursor-not-allowed"
                              >
                                {t('common.delete')}
                              </button>
                            </Gate>
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
          {totalPages > 1 && (
            <div className="flex justify-between items-center p-4 border-t border-[var(--border-color)] text-xs text-[var(--text-muted)]">
              <span>
                {t('common.page')} {page} / {totalPages} - {total}
              </span>
              <div className="flex gap-2">
                <button
                  disabled={page <= 1}
                  onClick={() => setPage((pr) => pr - 1)}
                  className="px-3 py-1.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] disabled:opacity-50 hover:bg-[var(--accent-color)]/10"
                >
                  {t('common.prev')}
                </button>
                <button
                  disabled={page >= totalPages}
                  onClick={() => setPage((pr) => pr + 1)}
                  className="px-3 py-1.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] disabled:opacity-50 hover:bg-[var(--accent-color)]/10"
                >
                  {t('common.next')}
                </button>
              </div>
            </div>
          )}
        </div>
      </Gate>

      {/* Create Modal */}
      {isCreateModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm">
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl w-full max-w-md p-6 space-y-4 shadow-xl max-h-[90vh] overflow-y-auto">
            <h2 className="text-xl font-bold border-b border-[var(--border-color)] pb-3 text-[var(--text-main)]">
              {t('permissions.addNew')}
            </h2>

            {submitError && (
              <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-sm text-red-500 flex items-start gap-2">
                <span className="text-lg leading-none">⚠️</span>
                <span className="flex-1">{submitError}</span>
                <button
                  type="button"
                  onClick={() => setSubmitError(null)}
                  className="text-red-500 hover:text-red-700 font-bold"
                >
                  ×
                </button>
              </div>
            )}

            <div className="space-y-4">
              {/* Display Name */}
              <div>
                <label className="block text-xs text-[var(--text-muted)] mb-1">
                  {t('permissions.displayName')} *
                </label>
                <input
                  type="text"
                  placeholder="Student Records"
                  value={formData.userDisplayName}
                  onChange={handleUserDisplayNameChange}
                  className={`w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border text-sm text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]/20 ${
                    formErrors.userDisplayName ? 'border-red-500' : 'border-[var(--border-color)]'
                  }`}
                />
                {formErrors.userDisplayName && (
                  <span className="text-red-400 text-xs mt-1 block">{formErrors.userDisplayName}</span>
                )}
              </div>

              {/* Module Code */}
              {formData.module_code && (
                <div className="p-3 rounded-xl bg-[var(--bg-primary)]/50 border border-[var(--border-color)]">
                  <div className="flex items-center justify-between mb-1">
                    <label className="block text-[10px] text-[var(--text-muted)] uppercase tracking-wide">
                      {t('permissions.moduleCode')}
                    </label>
                    <span className="text-[9px] px-2 py-0.5 rounded-full bg-[var(--border-color)] text-[var(--text-muted)] font-semibold">
                      🔒 {t('permissions.readOnly')}
                    </span>
                  </div>
                  <code className="text-sm font-mono font-bold text-[var(--text-main)]">
                    {formData.module_code}
                  </code>
                </div>
              )}

              {/* Action Dropdown */}
              <div>
                <label className="block text-xs text-[var(--text-muted)] mb-1">
                  {t('permissions.action')} *
                </label>

                {!formData.useCustomAction ? (
                  <select
                    value={formData.action}
                    onChange={handleActionSelect}
                    className={`w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border text-sm text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]/20 ${
                      formErrors.action ? 'border-red-500' : 'border-[var(--border-color)]'
                    }`}
                  >
                    <option value="">{t('permissions.selectAction')}</option>
                    {AVAILABLE_ACTIONS.map((action) => (
                      <option key={action} value={action}>
                        {action}
                      </option>
                    ))}
                    <option value="__new__">+ {t('permissions.addNewAction')}</option>
                  </select>
                ) : (
                  <div className="flex gap-2">
                    <input
                      type="text"
                      value={formData.customAction}
                      onChange={handleCustomActionChange}
                      placeholder="STATUS"
                      className={`flex-1 px-3 py-2 rounded-xl bg-[var(--bg-primary)] border text-sm font-mono uppercase text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]/20 ${
                        formErrors.action ? 'border-red-500' : 'border-[var(--border-color)]'
                      }`}
                    />
                    <button
                      type="button"
                      onClick={() =>
                        setFormData((prev) => ({
                          ...prev,
                          useCustomAction: false,
                          customAction: '',
                          action: '',
                          permission_name: '',
                          display_name: prev.userDisplayName,
                        }))
                      }
                      className="px-3 py-2 text-xs rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]"
                    >
                      {t('common.cancel')}
                    </button>
                  </div>
                )}
                {formErrors.action && (
                  <span className="text-red-400 text-xs mt-1 block">{formErrors.action}</span>
                )}
              </div>

              {/* Permission Name */}
              {formData.permission_name && (
                <div className="p-3 rounded-xl bg-[var(--accent-color)]/5 border border-[var(--accent-color)]/20">
                  <div className="flex items-center justify-between mb-1">
                    <label className="block text-[10px] text-[var(--text-muted)] uppercase tracking-wide">
                      {t('permissions.name')}
                    </label>
                    <span className="text-[9px] px-2 py-0.5 rounded-full bg-[var(--accent-color)]/20 text-[var(--accent-color)] font-semibold">
                      🔒 {t('permissions.readOnly')}
                    </span>
                  </div>
                  <code className="text-sm font-mono font-bold text-[var(--accent-color)] break-all">
                    {formData.permission_name}
                  </code>
                  {formErrors.permission_name && (
                    <span className="text-red-400 text-xs mt-1 block">
                      {formErrors.permission_name}
                    </span>
                  )}
                </div>
              )}

              {/* Final Display Name */}
              {formData.display_name && (
                <div className="p-3 rounded-xl bg-[var(--bg-primary)]/50 border border-[var(--border-color)]">
                  <div className="flex items-center justify-between mb-1">
                    <label className="block text-[10px] text-[var(--text-muted)] uppercase tracking-wide">
                      {t('permissions.displayNameFinal')}
                    </label>
                    <span className="text-[9px] px-2 py-0.5 rounded-full bg-[var(--border-color)] text-[var(--text-muted)] font-semibold">
                      🔒 {t('permissions.readOnly')}
                    </span>
                  </div>
                  <span className="text-sm font-medium text-[var(--text-main)]">
                    {formData.display_name}
                  </span>
                </div>
              )}

              {/* Branch Code */}
              <div>
                <label className="block text-xs text-[var(--text-muted)] mb-1">
                  {t('permissions.branch')}
                </label>
                {!showNewBranchInput ? (
                  <select
                    value={formData.branch_code}
                    onChange={(e) => {
                      if (e.target.value === '__new__') {
                        setShowNewBranchInput(true);
                        setFormData({ ...formData, branch_code: '' });
                      } else {
                        setFormData({ ...formData, branch_code: e.target.value });
                      }
                    }}
                    className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm text-[var(--text-main)]"
                  >
                    <option value="">{t('permissions.selectBranch')}</option>
                    {existingBranchCodes.map((code) => (
                      <option key={code} value={code}>
                        {code}
                      </option>
                    ))}
                    <option value="__new__">+ {t('permissions.addNewBranch')}</option>
                  </select>
                ) : (
                  <div className="flex gap-2">
                    <input
                      type="text"
                      value={formData.branch_code}
                      onChange={(e) =>
                        setFormData({ ...formData, branch_code: e.target.value.toUpperCase() })
                      }
                      placeholder="MAIN"
                      className="flex-1 px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm font-mono uppercase text-[var(--text-main)]"
                    />
                    <button
                      type="button"
                      onClick={() => {
                        setShowNewBranchInput(false);
                        setFormData({ ...formData, branch_code: '' });
                      }}
                      className="px-3 py-2 text-xs rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]"
                    >
                      {t('common.cancel')}
                    </button>
                  </div>
                )}
                {formErrors.branch_code && (
                  <span className="text-red-400 text-xs mt-1 block">{formErrors.branch_code}</span>
                )}
              </div>

              {/* Description */}
              <div>
                <label className="block text-xs text-[var(--text-muted)] mb-1">
                  {t('permissions.description')}
                </label>
                <textarea
                  placeholder={t('permissions.description')}
                  value={formData.description}
                  onChange={(e) =>
                    setFormData({ ...formData, description: e.target.value })
                  }
                  className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm text-[var(--text-main)] min-h-20 focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]/20 resize-none"
                />
              </div>

              {/* Checkboxes */}
              <div className="space-y-2 pt-2">
                <label className="flex items-center gap-3 p-3 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]/50 cursor-pointer hover:bg-[var(--bg-primary)] transition-colors">
                  <input
                    type="checkbox"
                    checked={formData.is_active}
                    onChange={(e) =>
                      setFormData({ ...formData, is_active: e.target.checked })
                    }
                    className="w-4 h-4 accent-[var(--accent-color)]"
                  />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-[var(--text-main)]">
                      {t('permissions.isActive')}
                    </p>
                  </div>
                </label>

                <label className="flex items-center gap-3 p-3 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]/50 cursor-pointer hover:bg-[var(--bg-primary)] transition-colors">
                  <input
                    type="checkbox"
                    checked={formData.is_sensitive}
                    onChange={(e) =>
                      setFormData({ ...formData, is_sensitive: e.target.checked })
                    }
                    className="w-4 h-4 accent-[var(--accent-color)]"
                  />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-[var(--text-main)]">
                      {t('permissions.isSensitive')}
                    </p>
                    <p className="text-[10px] text-[var(--text-muted)] mt-0.5">
                      ℹ️ {t('permissions.isSensitiveHint')}
                    </p>
                  </div>
                </label>
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-4">
              <button
                onClick={closeCreateModal}
                className="px-4 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)] hover:bg-[var(--bg-primary)]/80"
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={handleCreate}
                disabled={creating}
                className="px-4 py-2 rounded-xl bg-[var(--accent-color)] text-[var(--bg-primary)] disabled:opacity-50 text-sm font-medium hover:opacity-90"
              >
                {creating ? t('common.saving') : t('common.save')}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Details Modal */}
      {isDetailsModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm">
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl w-full max-w-md p-6 space-y-4 shadow-xl">
            <h2 className="text-xl font-bold border-b border-[var(--border-color)] pb-3 text-[var(--text-main)]">
              {t('permissions.details')}
            </h2>
            <div className="space-y-4 text-sm bg-[var(--bg-primary)] p-4 rounded-xl border border-[var(--border-color)]">
              <p className="flex flex-col gap-1">
                <span className="text-xs text-[var(--text-muted)] uppercase tracking-wide">
                  {t('permissions.name')}
                </span>
                <span className="font-mono font-bold text-[var(--accent-color)]">
                  {getPermCode(selectedPermission || {})}
                </span>
              </p>
              <p className="flex flex-col gap-1">
                <span className="text-xs text-[var(--text-muted)] uppercase tracking-wide">
                  {t('permissions.displayName')}
                </span>
                <span className="text-[var(--text-main)] font-medium">
                  {getDisplayNameWithFallback(selectedPermission)}
                </span>
              </p>
              <p className="flex flex-col gap-1">
                <span className="text-xs text-[var(--text-muted)] uppercase tracking-wide">
                  {t('permissions.module')}
                </span>
                <span className="inline-flex w-fit px-2.5 py-1 rounded-full bg-[var(--bg-secondary)] border border-[var(--border-color)] text-xs text-[var(--text-muted)] uppercase">
                  {getModule(selectedPermission || {})}
                </span>
              </p>
              <p className="flex flex-col gap-1">
                <span className="text-xs text-[var(--text-muted)] uppercase tracking-wide">
                  {t('permissions.branch')}
                </span>
                <span className="inline-flex w-fit px-2.5 py-1 rounded-full bg-[var(--bg-secondary)] border border-[var(--border-color)] text-xs text-[var(--text-muted)] uppercase">
                  {getBranchCode(selectedPermission || {})}
                </span>
              </p>
              <p className="flex flex-col gap-1">
                <span className="text-xs text-[var(--text-muted)] uppercase tracking-wide">
                  {t('permissions.description')}
                </span>
                <span className="text-[var(--text-main)]">
                  {selectedPermission?.description || selectedPermission?.Description || '-'}
                </span>
              </p>
              <p className="flex flex-col gap-1">
                <span className="text-xs text-[var(--text-muted)] uppercase tracking-wide">
                  {t('permissions.isActive')}
                </span>
                <span
                  className={`inline-flex w-fit px-2.5 py-1 rounded-full text-xs font-medium ${
                    isActive(selectedPermission)
                      ? 'bg-green-500/10 text-green-500 border border-green-500/20'
                      : 'bg-red-500/10 text-red-400 border border-red-500/20'
                  }`}
                >
                  {isActive(selectedPermission) ? t('common.active') : t('common.inactive')}
                </span>
              </p>
              <p className="flex flex-col gap-1">
                <span className="text-xs text-[var(--text-muted)] uppercase tracking-wide">
                  {t('permissions.isSensitive')}
                </span>
                <span
                  className={`inline-flex w-fit px-2.5 py-1 rounded-full text-xs font-medium ${
                    isSensitive(selectedPermission)
                      ? 'bg-red-500/10 text-red-400 border border-red-500/20'
                      : 'bg-[var(--bg-secondary)] text-[var(--text-muted)] border border-[var(--border-color)]'
                  }`}
                >
                  {isSensitive(selectedPermission) ? 'YES' : 'NO'}
                </span>
              </p>
              <p className="flex flex-col gap-1">
                <span className="text-xs text-[var(--text-muted)] uppercase tracking-wide">ID</span>
                <span className="font-mono text-xs text-[var(--text-muted)] break-all">
                  {selectedPermission?.id || '-'}
                </span>
              </p>
            </div>
            <div className="flex justify-end">
              <button
                onClick={() => setIsDetailsModalOpen(false)}
                className="px-5 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)] hover:bg-[var(--bg-primary)]/80"
              >
                {t('common.close')}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {isDeleteModalOpen && permissionToDelete && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm">
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl w-full max-w-sm p-6 space-y-4 shadow-xl text-center">
            <div className="flex justify-center">
              <div className="w-12 h-12 rounded-full bg-red-500/10 flex items-center justify-center">
                <span className="text-2xl">⚠️</span>
              </div>
            </div>
            <h3 className="text-lg font-bold text-red-400">
              {t('common.confirmDelete')}
            </h3>
            <p className="text-sm text-[var(--text-muted)]">
              {getPermCode(permissionToDelete)}
            </p>
            <p className="text-xs text-[var(--text-muted)]">
              {t('permissions.confirmDeleteHint', 'This action cannot be undone.')}
            </p>
            <div className="flex justify-center gap-2 pt-2">
              <button
                onClick={() => {
                  setIsDeleteModalOpen(false);
                  setPermissionToDelete(null);
                }}
                disabled={deleting}
                className="px-4 py-2 text-xs font-medium rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] disabled:opacity-50"
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleting}
                className="px-4 py-2 text-xs font-medium rounded-xl bg-red-600 text-white hover:bg-red-700 disabled:opacity-50"
              >
                {deleting ? t('common.deleting') : t('common.delete')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Permissions;