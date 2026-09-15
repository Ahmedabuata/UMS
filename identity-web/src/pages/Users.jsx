import React, { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import usersApi from '../api/users.api';
import { handleApiError } from '../utils/handleApiError';
import { useDebounce } from '../hooks/useDebounce';
import { Toast } from '../components/Toast';
import Gate from '../components/Gate';
import UserRolesModal from '../components/modals/UserRolesModal';
import UserGroupsModal from '../components/modals/UserGroupsModal';

const Users = () => {
  const { t, i18n } = useTranslation();
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [togglingId, setTogglingId] = useState(null);
  const [toast, setToast] = useState(null);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [limit] = useState(10);
  const [total, setTotal] = useState(0);
  const debouncedSearch = useDebounce(search, 500);
  const [selectedUserId, setSelectedUserId] = useState(null);
  const [showRolesModal, setShowRolesModal] = useState(false);
  const [showGroupsModal, setShowGroupsModal] = useState(false);
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedUser, setSelectedUser] = useState(null);

  const [formData, setFormData] = useState({
    username: '',
    email: '',
    phone_number: '',
    firstName: '',
    lastName: '',
    password: '',
    is_active: true,
    sendPasswordByEmail: false,
  });

  const [formErrors, setFormErrors] = useState({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState(null);

  // ============================================================
  // getUserStatus
  // ============================================================
  const getUserStatus = (u) => {
    if (!u) return false;
    const raw = u.IsActive ?? u.is_active ?? u.isActive ?? u.active ?? u.status ?? true;
    if (typeof raw === 'boolean') return raw;
    if (typeof raw === 'string') {
      const s = raw.toLowerCase();
      return s === 'active' || s === 'true' || s === '1';
    }
    if (typeof raw === 'number') return raw === 1;
    return true;
  };

  // ============================================================
  // extractErrorMessage
  // ============================================================
  const extractErrorMessage = (errRes, defaultMsg) => {
    if (!errRes) return defaultMsg;
    if (typeof errRes === 'string') return i18n.exists(errRes) ? t(errRes) : errRes;
    if (typeof errRes === 'object') {
      for (const v of Object.values(errRes)) {
        if (typeof v === 'string') return i18n.exists(v) ? t(v) : v;
        if (Array.isArray(v) && v[0]) return v[0];
      }
    }
    return defaultMsg;
  };

  // ============================================================
  // fetchUsers
  // ============================================================
  const fetchUsers = useCallback(async () => {
    try {
      setLoading(true);
      const params = { page, limit };
      if (debouncedSearch) params.search = debouncedSearch;
      const res = await usersApi.getAll(params);
      const body = res?.data?.data ?? res?.data;
      const list = Array.isArray(body) ? body : body.users || body.items || body.data || [];
      setUsers(list.filter(Boolean));
      setTotal(body.total || body.totalCount || list.length);
    } catch (e) {
      const errRes = handleApiError(e, 'users.errorLoading');
      setToast({ type: 'error', message: extractErrorMessage(errRes, t('users.errorLoading')) });
    } finally {
      setLoading(false);
    }
  }, [page, limit, debouncedSearch, t, i18n]);

  useEffect(() => { fetchUsers(); }, [fetchUsers]);
  useEffect(() => { setPage(1); }, [debouncedSearch]);

  // ============================================================
  // handleToggleStatus
  // ============================================================
  const handleToggleStatus = async (user) => {
    const userId = user.id || user._id;
    const isActive = getUserStatus(user);
    setTogglingId(userId);
    try {
      await usersApi.toggleStatus(userId, !isActive);
      setUsers(prevUsers =>
        prevUsers.map(u => {
          const uId = u.id || u._id;
          if (uId === userId) {
            return { ...u, IsActive: !isActive };
          }
          return u;
        })
      );
      setToast({
        type: 'success',
        message: !isActive ? t('users.activateSuccess') : t('users.deactivateSuccess'),
      });
    } catch (e) {
      const errRes = handleApiError(e, 'users.statusToggleError');
      setToast({ type: 'error', message: extractErrorMessage(errRes, t('users.statusToggleError')) });
      fetchUsers();
    } finally {
      setTogglingId(null);
    }
  };

  // ============================================================
  // closeModals
  // ============================================================
  const closeModals = () => {
    setIsAddModalOpen(false);
    setIsEditModalOpen(false);
    setIsDeleteModalOpen(false);
    setSelectedUser(null);
    setFormData({
      username: '',
      email: '',
      phone_number: '',
      firstName: '',
      lastName: '',
      password: '',
      is_active: true,
      sendPasswordByEmail: false,
    });
    setFormErrors({});
    setIsSubmitting(false);
    setSubmitError(null);
  };

  // ============================================================
  // validateForm
  // ============================================================
  const validateForm = (isEdit = false) => {
    const errors = {};
    if (!formData.username?.trim()) errors.username = t('validation.required');
    if (!formData.email?.trim()) errors.email = t('validation.required');
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = t('validation.invalidEmail');
    }
    if (!isEdit && !formData.sendPasswordByEmail && !formData.password?.trim()) {
      errors.password = t('validation.required');
    }
    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  // ============================================================
  // handleCreateUser - FIXED
  // ============================================================
  const handleCreateUser = async (e) => {
    e.preventDefault();
    if (!validateForm(false)) return;
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const payload = {
        username: formData.username,
        email: formData.email,
        phoneNumber: formData.phone_number?.trim() || null,
        firstName: formData.firstName?.trim() || null,
        lastName: formData.lastName?.trim() || null,
        password: formData.sendPasswordByEmail ? null : formData.password,
        sendPasswordByEmail: formData.sendPasswordByEmail,
      };

      const res = await usersApi.create(payload);
      const emailSent = res?.data?.emailSent;

      setToast({
        type: 'success',
        message: emailSent ? t('users.addSuccessEmailSent') : t('users.addSuccess'),
      });
      closeModals();
      setPage(1);
      fetchUsers();
    } catch (e) {
      const errRes = handleApiError(e, 'users.createError');
      const errorMessage = extractErrorMessage(errRes, t('users.createError'));

      // ✅ Field-specific error (optional enhancement)
      const lowerMsg = errorMessage.toLowerCase();
      if (lowerMsg.includes('username')) {
        setFormErrors(prev => ({ ...prev, username: errorMessage }));
      } else if (lowerMsg.includes('email')) {
        setFormErrors(prev => ({ ...prev, email: errorMessage }));
      } else if (lowerMsg.includes('phone')) {
        setFormErrors(prev => ({ ...prev, phone_number: errorMessage }));
      } else {
        setSubmitError(errorMessage);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  // ============================================================
  // handleUpdateUser - FIXED
  // ============================================================
  const handleUpdateUser = async (e) => {
    e.preventDefault();
    if (!validateForm(true)) return;
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const payload = {
        username: formData.username,
        email: formData.email,
        phoneNumber: formData.phone_number?.trim() || null,
        firstName: formData.firstName?.trim() || null,
        lastName: formData.lastName?.trim() || null,
      };
      if (formData.password?.trim()) {
        payload.password = formData.password;
      }

      await usersApi.update(selectedUser.id || selectedUser._id, payload);
      setToast({ type: 'success', message: t('users.updateSuccess') });
      closeModals();
      fetchUsers();
    } catch (e) {
      const errRes = handleApiError(e, 'users.updateError');
      const errorMessage = extractErrorMessage(errRes, t('users.updateError'));

      const lowerMsg = errorMessage.toLowerCase();
      if (lowerMsg.includes('username')) {
        setFormErrors(prev => ({ ...prev, username: errorMessage }));
      } else if (lowerMsg.includes('email')) {
        setFormErrors(prev => ({ ...prev, email: errorMessage }));
      } else {
        setSubmitError(errorMessage);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  // ============================================================
  // handleDeleteUser
  // ============================================================
  const handleDeleteUser = async () => {
    setIsSubmitting(true);
    try {
      await usersApi.delete(selectedUser.id || selectedUser._id);
      setToast({ type: 'success', message: t('users.deleteSuccess') });
      closeModals();
      if (users.length === 1 && page > 1) setPage(p => p - 1);
      else fetchUsers();
    } catch (e) {
      const errRes = handleApiError(e, 'users.deleteError');
      setToast({ type: 'error', message: extractErrorMessage(errRes, t('users.deleteError')) });
    } finally {
      setIsSubmitting(false);
    }
  };

  const totalPages = Math.ceil(total / limit) || 1;

  return (
    <div className="space-y-6 text-[var(--text-main)] p-6 bg-[var(--bg-primary)] min-h-screen">
      {toast && <Toast type={toast.type} message={toast.message} onClose={() => setToast(null)} />}

      {showRolesModal && selectedUserId && (
        <UserRolesModal
          userId={selectedUserId}
          onClose={() => { setShowRolesModal(false); setSelectedUserId(null); }}
          onSuccess={(msg) => {
            setToast({ type: 'success', message: msg });
            fetchUsers();
          }}
        />
      )}

      {showGroupsModal && selectedUserId && (
        <UserGroupsModal
          userId={selectedUserId}
          onClose={() => { setShowGroupsModal(false); setSelectedUserId(null); }}
          onSuccess={(msg) => {
            setToast({ type: 'success', message: msg });
            fetchUsers();
          }}
        />
      )}

      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <h1 className="text-2xl font-bold text-[var(--text-main)]">{t('users.title')}</h1>
        <div className="flex gap-2 w-full md:w-auto">
          <Gate permission="USER_SEARCH">
            <input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder={t('users.searchPlaceholder')}
              className="px-3 py-2 rounded-xl bg-[var(--bg-secondary)] border border-[var(--border-color)] text-sm w-full md:w-64 text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]/20"
            />
          </Gate>
          <Gate permission="USER_CREATE">
            <button
              onClick={() => {
                setFormData({
                  username: '',
                  email: '',
                  phone_number: '',
                  firstName: '',
                  lastName: '',
                  password: '',
                  is_active: true,
                  sendPasswordByEmail: false,
                });
                setFormErrors({});
                setSubmitError(null);
                setIsAddModalOpen(true);
              }}
              className="px-4 py-2 bg-[var(--accent-color)] text-white rounded-xl text-sm font-medium hover:opacity-90"
            >
              {t('users.addNew')}
            </button>
          </Gate>
        </div>
      </div>

      {/* Table */}
      <Gate permission="USER_READ" fallback={<div className="p-10 text-center text-sm text-[var(--text-muted)]">{t('common.noPermission')}</div>}>
        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-sm">
              <thead>
                <tr className="border-b border-[var(--border-color)] bg-[var(--bg-primary)]/50 text-[var(--text-muted)] text-xs uppercase tracking-wider">
                  <th className="p-4">{t('users.username')}</th>
                  <th className="p-4">{t('users.email')}</th>
                  <th className="p-4">{t('users.phone')}</th>
                  <th className="p-4">{t('users.status')}</th>
                  <th className="p-4 text-right">{t('users.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--border-color)]">
                {loading ? (
                  <tr><td colSpan="5" className="p-6 text-center text-[var(--text-muted)]">{t('common.loading')}</td></tr>
                ) : users.length === 0 ? (
                  <tr><td colSpan="5" className="p-6 text-center text-[var(--text-muted)]">{t('users.noData')}</td></tr>
                ) : (
                  users.map(u => {
                    const userId = u.id || u._id;
                    const isActive = getUserStatus(u);
                    return (
                      <tr key={userId} className="hover:bg-[var(--bg-primary)]/40 transition-colors">
                        <td className="p-4 font-medium text-[var(--text-main)]">{u.username || '-'}</td>
                        <td className="p-4 text-[var(--text-muted)]">{u.email}</td>
                        <td className="p-4 text-[var(--text-muted)]">
                          {u.phoneNumber || u.phone_number || u.phone || '-'}
                        </td>
                        <td className="p-4">
                          <span className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-medium border ${
                            isActive
                              ? 'bg-[var(--accent-color)]/10 border-[var(--accent-color)]/20 text-[var(--accent-color)]'
                              : 'bg-[var(--bg-primary)] border-[var(--border-color)] text-[var(--text-muted)]'
                          }`}>
                            {isActive ? t('users.active') : t('users.inactive')}
                          </span>
                        </td>
                        <td className="p-4 text-right">
                          <div className="flex justify-end gap-1.5 flex-wrap">
                            <Gate permission="USER_MANAGE_ROLES">
                              <button
                                onClick={() => { setSelectedUserId(userId); setShowRolesModal(true); }}
                                className="text-xs px-2.5 py-1 rounded-full bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-muted)] hover:bg-[var(--accent-color)]/10 hover:text-[var(--accent-color)]"
                              >
                                {t('users.roles')}
                              </button>
                            </Gate>
                            <Gate permission="USER_MANAGE_GROUPS">
                              <button
                                onClick={() => { setSelectedUserId(userId); setShowGroupsModal(true); }}
                                className="text-xs px-2.5 py-1 rounded-full bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-muted)] hover:bg-[var(--accent-color)]/10 hover:text-[var(--accent-color)]"
                              >
                                {t('users.groups')}
                              </button>
                            </Gate>
                            <Gate permission="USER_UPDATE">
                              <button
                                onClick={() => {
                                  setSelectedUser(u);
                                  setFormData({
                                    username: u.username || '',
                                    email: u.email || '',
                                    phone_number: u.phoneNumber || u.phone_number || u.phone || '',
                                    firstName: u.firstName || u.first_name || '',
                                    lastName: u.lastName || u.last_name || '',
                                    password: '',
                                    is_active: getUserStatus(u),
                                    sendPasswordByEmail: false,
                                  });
                                  setFormErrors({});
                                  setSubmitError(null);
                                  setIsEditModalOpen(true);
                                }}
                                className="text-xs px-2.5 py-1 rounded-full bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-muted)] hover:bg-[var(--accent-color)]/10 hover:text-[var(--accent-color)]"
                              >
                                {t('common.edit')}
                              </button>
                            </Gate>
                            <Gate permission="USER_TOGGLE_STATUS">
                              <button
                                onClick={() => handleToggleStatus(u)}
                                disabled={togglingId === userId}
                                className="text-xs px-2.5 py-1 rounded-full bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-muted)] disabled:opacity-50"
                              >
                                {isActive ? t('users.deactivate') : t('users.activate')}
                              </button>
                            </Gate>
                            <Gate permission="USER_DELETE">
                              <button
                                onClick={() => { setSelectedUser(u); setIsDeleteModalOpen(true); }}
                                className="text-xs px-2.5 py-1 rounded-full bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-muted)] hover:text-red-500 hover:border-red-500/20"
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
              <span>{t('common.page')} {page} / {totalPages} - {total}</span>
              <div className="flex gap-2">
                <button disabled={page <= 1} onClick={() => setPage(p => p - 1)} className="px-3 py-1.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] disabled:opacity-50">
                  {t('common.previous')}
                </button>
                <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)} className="px-3 py-1.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] disabled:opacity-50">
                  {t('common.next')}
                </button>
              </div>
            </div>
          )}
        </div>
      </Gate>

      {/* ============================================================
          Add User Modal
          ============================================================ */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl w-full max-w-md p-6 space-y-4 shadow-xl">
            <h2 className="text-xl font-bold text-[var(--text-main)]">
              {t('users.addNew')}
            </h2>

            {/* ✅ Submit Error Alert */}
            {submitError && (
              <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-sm text-red-500 flex items-start gap-2">
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

            <form onSubmit={handleCreateUser} className="space-y-3">
              {/* Username */}
              <div>
                <input
                  type="text"
                  value={formData.username}
                  onChange={e => setFormData({ ...formData, username: e.target.value })}
                  placeholder={t('users.username')}
                  className={`w-full px-3 py-2 rounded-xl border bg-[var(--bg-primary)] text-sm text-[var(--text-main)] ${
                    formErrors.username
                      ? 'border-red-500 ring-2 ring-red-500/20'
                      : 'border-[var(--border-color)]'
                  }`}
                />
                {formErrors.username && <p className="text-xs text-red-500 mt-1">{formErrors.username}</p>}
              </div>

              {/* Email */}
              <div>
                <input
                  type="email"
                  value={formData.email}
                  onChange={e => setFormData({ ...formData, email: e.target.value })}
                  placeholder={t('users.email')}
                  className={`w-full px-3 py-2 rounded-xl border bg-[var(--bg-primary)] text-sm text-[var(--text-main)] ${
                    formErrors.email
                      ? 'border-red-500 ring-2 ring-red-500/20'
                      : 'border-[var(--border-color)]'
                  }`}
                />
                {formErrors.email && <p className="text-xs text-red-500 mt-1">{formErrors.email}</p>}
              </div>

              {/* First Name + Last Name */}
              <div className="grid grid-cols-2 gap-3">
                <input
                  type="text"
                  value={formData.firstName}
                  onChange={e => setFormData({ ...formData, firstName: e.target.value })}
                  placeholder={t('users.firstName')}
                  className="w-full px-3 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)]"
                />
                <input
                  type="text"
                  value={formData.lastName}
                  onChange={e => setFormData({ ...formData, lastName: e.target.value })}
                  placeholder={t('users.lastName')}
                  className="w-full px-3 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)]"
                />
              </div>

              {/* Phone */}
              <div>
                <input
                  type="text"
                  value={formData.phone_number}
                  onChange={e => setFormData({ ...formData, phone_number: e.target.value })}
                  placeholder={t('users.phone')}
                  className={`w-full px-3 py-2 rounded-xl border bg-[var(--bg-primary)] text-sm text-[var(--text-main)] ${
                    formErrors.phone_number
                      ? 'border-red-500 ring-2 ring-red-500/20'
                      : 'border-[var(--border-color)]'
                  }`}
                />
                {formErrors.phone_number && <p className="text-xs text-red-500 mt-1">{formErrors.phone_number}</p>}
              </div>

              {/* Send Password By Email Checkbox */}
              <label className="flex items-start gap-3 p-3 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]/50 cursor-pointer hover:bg-[var(--bg-primary)] transition-colors">
                <input
                  type="checkbox"
                  checked={formData.sendPasswordByEmail}
                  onChange={e => setFormData({
                    ...formData,
                    sendPasswordByEmail: e.target.checked,
                    password: e.target.checked ? '' : formData.password,
                  })}
                  className="mt-0.5 w-4 h-4 accent-[var(--accent-color)]"
                />
                <div className="flex-1">
                  <p className="text-sm font-medium text-[var(--text-main)]">
                    {t('users.sendPasswordByEmail')}
                  </p>
                  <p className="text-xs text-[var(--text-muted)] mt-0.5">
                    {t('users.sendPasswordByEmailHint')}
                  </p>
                </div>
              </label>

              {/* Password */}
              {!formData.sendPasswordByEmail && (
                <div>
                  <input
                    type="password"
                    value={formData.password}
                    onChange={e => setFormData({ ...formData, password: e.target.value })}
                    placeholder={t('login.password')}
                    className={`w-full px-3 py-2 rounded-xl border bg-[var(--bg-primary)] text-sm text-[var(--text-main)] ${
                      formErrors.password
                        ? 'border-red-500 ring-2 ring-red-500/20'
                        : 'border-[var(--border-color)]'
                    }`}
                  />
                  {formErrors.password && <p className="text-xs text-red-500 mt-1">{formErrors.password}</p>}
                </div>
              )}

              {/* Footer */}
              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={closeModals}
                  disabled={isSubmitting}
                  className="px-4 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)] disabled:opacity-50"
                >
                  {t('common.cancel')}
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-4 py-2 rounded-xl bg-[var(--accent-color)] text-white text-sm font-medium disabled:opacity-50"
                >
                  {isSubmitting ? t('common.saving') : t('common.save')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ============================================================
          Edit User Modal
          ============================================================ */}
      {isEditModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl w-full max-w-md p-6 space-y-4 shadow-xl">
            <h2 className="text-xl font-bold text-[var(--text-main)]">{t('common.edit')}</h2>

            {/* ✅ Submit Error Alert */}
            {submitError && (
              <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-sm text-red-500 flex items-start gap-2">
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

            <form onSubmit={handleUpdateUser} className="space-y-3">
              <div>
                <input
                  type="text"
                  value={formData.username}
                  onChange={e => setFormData({ ...formData, username: e.target.value })}
                  placeholder={t('users.username')}
                  className={`w-full px-3 py-2 rounded-xl border bg-[var(--bg-primary)] text-sm text-[var(--text-main)] ${
                    formErrors.username
                      ? 'border-red-500 ring-2 ring-red-500/20'
                      : 'border-[var(--border-color)]'
                  }`}
                />
                {formErrors.username && <p className="text-xs text-red-500 mt-1">{formErrors.username}</p>}
              </div>

              <div>
                <input
                  type="email"
                  value={formData.email}
                  onChange={e => setFormData({ ...formData, email: e.target.value })}
                  placeholder={t('users.email')}
                  className={`w-full px-3 py-2 rounded-xl border bg-[var(--bg-primary)] text-sm text-[var(--text-main)] ${
                    formErrors.email
                      ? 'border-red-500 ring-2 ring-red-500/20'
                      : 'border-[var(--border-color)]'
                  }`}
                />
                {formErrors.email && <p className="text-xs text-red-500 mt-1">{formErrors.email}</p>}
              </div>

              <div className="grid grid-cols-2 gap-3">
                <input
                  type="text"
                  value={formData.firstName}
                  onChange={e => setFormData({ ...formData, firstName: e.target.value })}
                  placeholder={t('users.firstName')}
                  className="w-full px-3 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)]"
                />
                <input
                  type="text"
                  value={formData.lastName}
                  onChange={e => setFormData({ ...formData, lastName: e.target.value })}
                  placeholder={t('users.lastName')}
                  className="w-full px-3 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)]"
                />
              </div>

              <input
                type="text"
                value={formData.phone_number}
                onChange={e => setFormData({ ...formData, phone_number: e.target.value })}
                placeholder={t('users.phone')}
                className="w-full px-3 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)]"
              />

              <input
                type="password"
                value={formData.password}
                onChange={e => setFormData({ ...formData, password: e.target.value })}
                placeholder="Leave empty"
                className="w-full px-3 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)]"
              />

              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={closeModals}
                  className="px-4 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)]"
                >
                  {t('common.cancel')}
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-4 py-2 rounded-xl bg-[var(--accent-color)] text-white text-sm"
                >
                  {t('common.save')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete User Modal */}
      {isDeleteModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
          <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl w-full max-w-sm p-6 text-center space-y-4 shadow-xl">
            <h2 className="text-xl font-bold text-red-500">{t('common.delete')}</h2>
            <p className="text-sm text-[var(--text-muted)]">{t('common.confirmDelete')}</p>
            <div className="flex justify-center gap-2 pt-2">
              <button
                onClick={closeModals}
                className="px-4 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm text-[var(--text-main)] hover:bg-[var(--bg-primary)]/80"
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={handleDeleteUser}
                disabled={isSubmitting}
                className="px-4 py-2 rounded-xl bg-red-500 text-white text-sm hover:bg-red-600 disabled:opacity-50"
              >
                {t('common.delete')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Users;