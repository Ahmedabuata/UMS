import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import axiosInstance from '../api/axiosInstance';
import { handleApiError } from '../utils/handleApiError';
import { isRequired } from '../utils/validation';
import { useDebounce } from '../hooks/useDebounce';
import Gate from '../components/Gate';
import { Toast } from '../components/Toast';

// ============================================================
// Helper: Extract error message from any error shape
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
// Helper: Get role display name (multi-schema support)
// ============================================================
const getRoleDisplayName = (role) => {
  if (!role) return '';
  return (
    role.displayName ||
    role.display_name ||
    role.roleName ||
    role.name ||
    role.title ||
    '-'
  );
};

// ============================================================
// Helper: Auto-generate RoleName from DisplayName
// "HR MANAGER" → "HR_MANAGER"
// ============================================================
const generateRoleName = (displayName) => {
  if (!displayName) return '';
  return displayName
    .trim()
    .toUpperCase()
    .replace(/\s+/g, '_')
    .replace(/[^A-Z0-9_]/g, '');
};

const Roles = () => {
  const { t } = useTranslation();
  const [roles, setRoles] = useState([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebounce(search, 300);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalRecords, setTotalRecords] = useState(0);

  // Branches (from existing roles + manual input)
  const [branches, setBranches] = useState([]);
  const [showNewBranchInput, setShowNewBranchInput] = useState(false);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [currentRole, setCurrentRole] = useState(null);
  const [roleToDelete, setRoleToDelete] = useState(null);

  // Toggle status
  const [togglingId, setTogglingId] = useState(null);

  // ✅ Full formData with all fields
  const [formData, setFormData] = useState({
    displayName: '',
    roleName: '',
    description: '',
    branchCode: '',
    isSystemRole: false,
    isActive: true,
  });

  const [formErrors, setFormErrors] = useState({});
  const [submitError, setSubmitError] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState(null);

  // ============================================================
  // fetchRoles
  // ============================================================
  const fetchRoles = async () => {
    setLoading(true);
    try {
      const response = await axiosInstance.get('/roles', {
        params: { search: debouncedSearch, page, limit: 10 },
      });

      const body = response.data;
      let rolesArray = [];

      if (Array.isArray(body)) {
        rolesArray = body;
      } else if (Array.isArray(body.data)) {
        rolesArray = body.data;
      } else if (Array.isArray(body.roles)) {
        rolesArray = body.roles;
      } else if (Array.isArray(body.items)) {
        rolesArray = body.items;
      }

      setRoles(rolesArray);
      setTotalPages(
        body.totalPages ||
        Math.ceil((body.total || rolesArray.length) / 10) ||
        1
      );
      setTotalRecords(body.total || rolesArray.length || 0);

      // ✅ Extract unique branches from roles
      const uniqueBranches = [
        ...new Set(
          rolesArray
            .map((r) => r.branchCode || r.branch_code)
            .filter(Boolean)
        ),
      ];
      setBranches(uniqueBranches);
    } catch (error) {
      const errRes = handleApiError(error, 'roles.errorLoading');
      setToast({
        type: 'error',
        message: typeof errRes === 'string' ? t(errRes) : t('roles.errorLoading'),
      });
      setRoles([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchRoles(); }, [debouncedSearch, page]);
  useEffect(() => { setPage(1); }, [debouncedSearch]);

  // ============================================================
  // validateForm
  // ============================================================
  const validateForm = () => {
    const errors = {};
    if (!isRequired(formData.displayName)) errors.displayName = t('validation.required');
    if (!isRequired(formData.roleName)) errors.roleName = t('validation.required');
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  // ============================================================
  // handleDisplayNameChange - Auto-generate roleName
  // ============================================================
  const handleDisplayNameChange = (e) => {
    const displayName = e.target.value;
    setFormData((prev) => ({
      ...prev,
      displayName,
      roleName: generateRoleName(displayName),
    }));
  };

  // ============================================================
  // handleSave
  // ============================================================
  const handleSave = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const payload = {
        roleName: formData.roleName?.trim().toUpperCase(),
        displayName: formData.displayName?.trim(),
        description: formData.description?.trim() || null,
        branchCode: formData.branchCode?.trim() || null,
        isSystemRole: formData.isSystemRole,
        isActive: formData.isActive,
      };

      if (currentRole) {
        await axiosInstance.put(`/roles/${currentRole.id}`, payload);
        setToast({ type: 'success', message: t('roles.updateSuccess') });
      } else {
        await axiosInstance.post('/roles', payload);
        setToast({ type: 'success', message: t('roles.addSuccess') });
      }

      setIsModalOpen(false);
      fetchRoles();
    } catch (error) {
      const errRes = handleApiError(error, 'roles.errorLoading');
      const errorMessage = extractErrorMessage(
        errRes,
        t('roles.saveError') || 'Failed to save role'
      );

      const lowerMsg = errorMessage.toLowerCase();
      if (lowerMsg.includes('display')) {
        setFormErrors((prev) => ({ ...prev, displayName: errorMessage }));
      } else if (lowerMsg.includes('rolename') || lowerMsg.includes('role name')) {
        setFormErrors((prev) => ({ ...prev, roleName: errorMessage }));
      } else if (lowerMsg.includes('branch')) {
        setFormErrors((prev) => ({ ...prev, branchCode: errorMessage }));
      } else if (lowerMsg.includes('exists') || lowerMsg.includes('already')) {
        setFormErrors((prev) => ({ ...prev, displayName: errorMessage }));
      } else {
        setSubmitError(errorMessage);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  // ============================================================
  // ✅ handleToggleStatus - Activate/Deactivate role
  // ============================================================
  const handleToggleStatus = async (role) => {
    const roleId = role.id;
    const currentActive = role.isActive ?? role.is_active ?? true;
    const newStatus = !currentActive;

    // ⚠️ Prevent deactivating SUPER_ADMIN
    if (role.roleName === 'SUPER_ADMIN' && !newStatus) {
      setToast({
        type: 'error',
        message: t('roles.cannotDeactivateSuperAdmin', 'Cannot deactivate SUPER_ADMIN'),
      });
      return;
    }

    setTogglingId(roleId);
    try {
      await axiosInstance.patch(`/roles/${roleId}/status`, { isActive: newStatus });

      // ✅ Optimistic update
      setRoles((prev) =>
        prev.map((r) => {
          if (r.id === roleId) {
            return { ...r, isActive: newStatus, is_active: newStatus };
          }
          return r;
        })
      );

      setToast({
        type: 'success',
        message: newStatus
          ? t('roles.activateSuccess', 'Role activated successfully')
          : t('roles.deactivateSuccess', 'Role deactivated successfully'),
      });
    } catch (error) {
      const errRes = handleApiError(error, 'roles.errorLoading');
      setToast({
        type: 'error',
        message: extractErrorMessage(
          errRes,
          t('roles.statusToggleError', 'Failed to toggle role status')
        ),
      });
      fetchRoles();
    } finally {
      setTogglingId(null);
    }
  };

  // ============================================================
  // handleDeleteConfirm
  // ============================================================
  const handleDeleteConfirm = async () => {
    if (!roleToDelete) return;
    setIsSubmitting(true);
    try {
      await axiosInstance.delete(`/roles/${roleToDelete.id}`);
      setToast({ type: 'success', message: t('roles.deleteSuccess') });
      setIsDeleteModalOpen(false);
      setRoleToDelete(null);
      fetchRoles();
    } catch (error) {
      const errRes = handleApiError(error, 'roles.errorLoading');
      setToast({
        type: 'error',
        message: typeof errRes === 'string' ? t(errRes) : t('roles.errorLoading'),
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  // ============================================================
  // openModal
  // ============================================================
  const openModal = (role = null) => {
    setCurrentRole(role);
    setShowNewBranchInput(false);

    if (role) {
      setFormData({
        displayName: getRoleDisplayName(role) === '-' ? '' : getRoleDisplayName(role),
        roleName: role.roleName || role.role_name || '',
        description: role.description || '',
        branchCode: role.branchCode || role.branch_code || '',
        isSystemRole: role.isSystemRole ?? role.is_system_role ?? false,
        isActive: role.isActive ?? role.is_active ?? true,
      });
    } else {
      setFormData({
        displayName: '',
        roleName: '',
        description: '',
        branchCode: '',
        isSystemRole: false,
        isActive: true,
      });
    }

    setFormErrors({});
    setSubmitError(null);
    setIsModalOpen(true);
  };

  // ============================================================
  // openDeleteModal
  // ============================================================
  const openDeleteModal = (role) => {
    setRoleToDelete(role);
    setIsDeleteModalOpen(true);
  };

  // ============================================================
  // closeModal
  // ============================================================
  const closeModal = () => {
    setIsModalOpen(false);
    setSubmitError(null);
  };

  return (
    <div className="p-6 text-[var(--text-main)]">
      {toast && <Toast type={toast.type} message={toast.message} onClose={() => setToast(null)} />}

      {/* Header */}
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-2xl font-bold">{t('roles.title')}</h1>
          <p className="text-[var(--text-muted)] text-sm">{t('roles.subtitle')}</p>
        </div>
        <Gate permission="Roles.Create">
          <button
            onClick={() => openModal()}
            className="px-4 py-2 bg-[var(--accent-color)] text-white rounded-xl hover:opacity-90 transition-all font-medium text-sm"
          >
            {t('roles.addNew')}
          </button>
        </Gate>
      </div>

      {/* Search */}
      <div className="mb-4">
        <input
          type="text"
          placeholder={t('roles.searchPlaceholder')}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full md:w-80 px-4 py-2.5 rounded-xl bg-[var(--bg-secondary)] border border-[var(--border-color)] text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]"
        />
      </div>

      {/* Table */}
      <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl overflow-hidden shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-[var(--bg-primary)]/50 border-b border-[var(--border-color)] text-[var(--text-muted)] text-xs uppercase tracking-wider">
                <th className="p-4 font-semibold">{t('roles.displayName')}</th>
                <th className="p-4 font-semibold">{t('roles.roleName')}</th>
                <th className="p-4 font-semibold">{t('roles.description')}</th>
                <th className="p-4 font-semibold">{t('roles.branch')}</th>
                <th className="p-4 font-semibold">{t('roles.isActive')}</th>
                <th className="p-4 font-semibold text-right">{t('common.actions')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--border-color)] text-sm">
              {loading ? (
                <tr>
                  <td colSpan="6" className="p-8 text-center text-[var(--text-muted)]">
                    {t('common.loading')}
                  </td>
                </tr>
              ) : roles.length === 0 ? (
                <tr>
                  <td colSpan="6" className="p-8 text-center text-[var(--text-muted)]">
                    {t('roles.noData')}
                  </td>
                </tr>
              ) : (
                roles.map((role) => {
                  const isActive = role.isActive ?? role.is_active ?? true;
                  const isSystem = role.isSystemRole ?? role.is_system_role ?? false;
                  const isSuperAdmin = (role.roleName || role.role_name) === 'SUPER_ADMIN';
                  const isToggling = togglingId === role.id;

                  return (
                    <tr
                      key={role.id}
                      className={`transition-colors ${
                        isActive
                          ? 'hover:bg-[var(--bg-primary)]/30'
                          : 'bg-red-500/5 hover:bg-red-500/10'
                      }`}
                    >
                      <td className="p-4 font-medium">
                        <div className="flex items-center gap-2">
                          {getRoleDisplayName(role)}
                          {!isActive && (
                            <span className="text-[10px] px-2 py-0.5 rounded-full bg-red-500/20 text-red-400 font-semibold uppercase">
                              {t('common.inactive')}
                            </span>
                          )}
                        </div>
                      </td>
                      <td className="p-4 text-[var(--text-muted)] font-mono text-xs">
                        {role.roleName || role.role_name || '-'}
                      </td>
                      <td className="p-4 text-[var(--text-muted)]">{role.description || '-'}</td>
                      <td className="p-4 text-[var(--text-muted)]">
                        {role.branchCode || role.branch_code || '-'}
                      </td>
                      <td className="p-4">
                        <span
                          className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium border ${
                            isActive
                              ? 'bg-green-500/10 border-green-500/20 text-green-500'
                              : 'bg-red-500/10 border-red-500/20 text-red-400'
                          }`}
                        >
                          {isActive ? t('common.active') : t('common.inactive')}
                        </span>
                      </td>
                      <td className="p-4">
                        <div className="flex justify-end gap-2 flex-wrap">
                          {/* ✅ NEW: Activate/Deactivate Button */}
                          <Gate permission="Roles.Update">
                            <button
                              onClick={() => handleToggleStatus(role)}
                              disabled={isSuperAdmin || isToggling}
                              title={
                                isSuperAdmin
                                  ? t('roles.cannotDeactivateSuperAdmin', 'Cannot deactivate SUPER_ADMIN')
                                  : isActive
                                  ? t('roles.deactivate', 'Deactivate')
                                  : t('roles.activate', 'Activate')
                              }
                              className={`px-3 py-1.5 text-xs font-medium rounded-lg border transition-all whitespace-nowrap disabled:opacity-30 disabled:cursor-not-allowed ${
                                isActive
                                  ? 'bg-amber-500/10 border-amber-500/20 text-amber-500 hover:bg-amber-500/20'
                                  : 'bg-green-500/10 border-green-500/20 text-green-500 hover:bg-green-500/20'
                              }`}
                            >
                              {isToggling
                                ? '...'
                                : isActive
                                ? t('roles.deactivate', 'Deactivate')
                                : t('roles.activate', 'Activate')}
                            </button>
                          </Gate>

                          {/* Manage Permissions */}
                          <Gate permission="Roles.Update">
                            <Link
                              to={`/roles/${role.id}/permissions`}
                              className="px-3 py-1.5 text-xs font-medium rounded-lg bg-[var(--bg-accent)] text-[var(--accent-color)] border border-[var(--accent-color)]/30 hover:bg-[var(--accent-color)]/20 transition-all whitespace-nowrap"
                            >
                              {t('roles.managePermissions')}
                            </Link>
                          </Gate>

                          {/* Edit */}
                          <Gate permission="Roles.Update">
                            <button
                              onClick={() => openModal(role)}
                              className="px-3 py-1.5 text-xs font-medium rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-color)] hover:bg-[var(--bg-primary)] transition-all whitespace-nowrap"
                            >
                              {t('common.edit')}
                            </button>
                          </Gate>

                          {/* Delete */}
                          <Gate permission="Roles.Delete">
                            <button
                              onClick={() => openDeleteModal(role)}
                              disabled={isSystem}
                              title={isSystem ? t('roles.cannotDeleteSystemRole') : ''}
                              className="px-3 py-1.5 text-xs font-medium rounded-lg bg-red-500/10 text-red-400 border border-red-500/20 hover:bg-red-500/20 transition-all whitespace-nowrap disabled:opacity-30 disabled:cursor-not-allowed"
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

        {/* Pagination */}
        <div className="p-4 border-t border-[var(--border-color)] flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-[var(--text-muted)]">
          <div>
            {t('common.showing')} {roles.length > 0 ? (page - 1) * 10 + 1 : 0} {t('common.of')} {totalRecords} {t('common.records')}
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage((p) => Math.max(p - 1, 1))}
              disabled={page === 1}
              className="px-3 py-1.5 rounded-lg border border-[var(--border-color)] bg-[var(--bg-primary)] disabled:opacity-50"
            >
              {t('common.previous')}
            </button>
            <span>{t('common.page')} {page} / {totalPages || 1}</span>
            <button
              onClick={() => setPage((p) => Math.min(p + 1, totalPages))}
              disabled={page >= totalPages}
              className="px-3 py-1.5 rounded-lg border border-[var(--border-color)] bg-[var(--bg-primary)] disabled:opacity-50"
            >
              {t('common.next')}
            </button>
          </div>
        </div>
      </div>

      {/* ============================================================
          Create/Edit Modal
          ============================================================ */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl w-full max-w-lg p-6 shadow-xl max-h-[90vh] overflow-y-auto">
            <h2 className="text-lg font-bold mb-4">
              {currentRole ? t('common.edit') : t('roles.addNew')}
            </h2>

            {submitError && (
              <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-sm text-red-500 flex items-start gap-2 mb-4">
                <span className="text-lg leading-none">⚠️</span>
                <span className="flex-1">{submitError}</span>
                <button
                  type="button"
                  onClick={() => setSubmitError(null)}
                  className="text-red-500 hover:text-red-700 font-bold"
                  aria-label="Close"
                >
                  ×
                </button>
              </div>
            )}

            <form onSubmit={handleSave} className="space-y-4">
              {/* Display Name */}
              <div>
                <label className="block text-xs font-medium mb-1">
                  {t('roles.displayName')} *
                </label>
                <input
                  type="text"
                  value={formData.displayName}
                  onChange={handleDisplayNameChange}
                  placeholder="HR MANAGER"
                  className={`w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)] text-sm ${
                    formErrors.displayName
                      ? 'border-red-500 ring-2 ring-red-500/20'
                      : 'border-[var(--border-color)]'
                  }`}
                />
                {formErrors.displayName && (
                  <span className="text-red-400 text-xs mt-1 block">{formErrors.displayName}</span>
                )}
                <p className="text-[10px] text-[var(--text-muted)] mt-1">
                  ℹ️ {t('roles.displayNameHint')}
                </p>
              </div>

              {/* Role Name (auto) */}
              <div>
                <label className="block text-xs font-medium mb-1">
                  {t('roles.roleName')} ({t('roles.autoGenerated')})
                </label>
                <input
                  type="text"
                  value={formData.roleName}
                  readOnly
                  className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)]/50 border border-[var(--border-color)] text-[var(--text-muted)] font-mono text-sm cursor-not-allowed"
                />
                <p className="text-[10px] text-[var(--text-muted)] mt-1">
                  ℹ️ {t('roles.roleNameHint')}
                </p>
              </div>

              {/* Description */}
              <div>
                <label className="block text-xs font-medium mb-1">{t('roles.description')}</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)] text-sm resize-none h-20"
                />
              </div>

              {/* Branch */}
              <div>
                <label className="block text-xs font-medium mb-1">{t('roles.branch')}</label>
                {!showNewBranchInput ? (
                  <select
                    value={formData.branchCode}
                    onChange={(e) => {
                      if (e.target.value === '__new__') {
                        setShowNewBranchInput(true);
                        setFormData({ ...formData, branchCode: '' });
                      } else {
                        setFormData({ ...formData, branchCode: e.target.value });
                      }
                    }}
                    className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-main)] text-sm"
                  >
                    <option value="">{t('roles.selectBranch')}</option>
                    {branches.map((b) => (
                      <option key={b} value={b}>
                        {b}
                      </option>
                    ))}
                    <option value="__new__">+ {t('roles.addNewBranch')}</option>
                  </select>
                ) : (
                  <div className="flex gap-2">
                    <input
                      type="text"
                      value={formData.branchCode}
                      onChange={(e) => setFormData({ ...formData, branchCode: e.target.value })}
                      placeholder="BRANCH_CODE"
                      className="flex-1 px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-main)] text-sm"
                    />
                    <button
                      type="button"
                      onClick={() => {
                        setShowNewBranchInput(false);
                        setFormData({ ...formData, branchCode: '' });
                      }}
                      className="px-3 py-2 text-xs rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]"
                    >
                      {t('common.cancel')}
                    </button>
                  </div>
                )}
              </div>

              {/* Checkboxes */}
              <div className="space-y-2 pt-2">
                <label className="flex items-center gap-3 p-3 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]/50 cursor-pointer hover:bg-[var(--bg-primary)] transition-colors">
                  <input
                    type="checkbox"
                    checked={formData.isSystemRole}
                    onChange={(e) => setFormData({ ...formData, isSystemRole: e.target.checked })}
                    className="w-4 h-4 accent-[var(--accent-color)]"
                  />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-[var(--text-main)]">
                      {t('roles.isSystemRole')}
                    </p>
                    <p className="text-[10px] text-[var(--text-muted)] mt-0.5">
                      ℹ️ {t('roles.isSystemRoleHint')}
                    </p>
                  </div>
                </label>

                <label className="flex items-center gap-3 p-3 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]/50 cursor-pointer hover:bg-[var(--bg-primary)] transition-colors">
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    className="w-4 h-4 accent-[var(--accent-color)]"
                  />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-[var(--text-main)]">
                      {t('roles.isActive')}
                    </p>
                    <p className="text-[10px] text-[var(--text-muted)] mt-0.5">
                      ℹ️ {t('roles.isActiveHint')}
                    </p>
                  </div>
                </label>
              </div>

              {/* Footer */}
              <div className="flex justify-end gap-2 pt-3">
                <button
                  type="button"
                  onClick={closeModal}
                  disabled={isSubmitting}
                  className="px-4 py-2 text-xs font-medium rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] disabled:opacity-50"
                >
                  {t('common.cancel')}
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-4 py-2 text-xs font-medium rounded-xl bg-[var(--accent-color)] text-white disabled:opacity-50"
                >
                  {isSubmitting ? t('common.saving') : t('common.save')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {isDeleteModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl w-full max-w-sm p-6 shadow-xl text-center">
            <h3 className="text-base font-bold mb-2">{t('common.confirmDelete')}</h3>
            <p className="text-xs text-[var(--text-muted)] mb-6">{getRoleDisplayName(roleToDelete)}</p>
            <div className="flex justify-center gap-2">
              <button
                onClick={() => setIsDeleteModalOpen(false)}
                className="px-4 py-2 text-xs font-medium rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]"
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={isSubmitting}
                className="px-4 py-2 text-xs font-medium rounded-xl bg-red-600 text-white disabled:opacity-50"
              >
                {isSubmitting ? t('common.deleting') : t('common.delete')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Roles;