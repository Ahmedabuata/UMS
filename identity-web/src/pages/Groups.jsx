import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import Gate from '../components/Gate';
import axiosInstance from '../api/axiosInstance';
import { useDebounce } from '../hooks/useDebounce';
import { Toast } from '../components/Toast';

export const Groups = () => {
  const { t } = useTranslation();
  const [groups, setGroups] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchQuery, setSearchQuery] = useState('');
  const debouncedSearch = useDebounce(searchQuery, 300);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalRecords, setTotalRecords] = useState(0);
  const pageSize = 10;

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [currentGroup, setCurrentGroup] = useState(null);
  const [groupToDelete, setGroupToDelete] = useState(null);
  const [formData, setFormData] = useState({
    displayName: '',
    name: '',
    description: '',
    branchCode: 'MAIN',
  });
  const [formErrors, setFormErrors] = useState({});
  const [submitError, setSubmitError] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [toast, setToast] = useState(null);

  // ============================================================
  // Extract existing branch codes from current groups
  // ============================================================
  const existingBranchCodes = useMemo(() => {
    const codes = groups
      .map(g => g.branch_code || g.branchCode || g.BranchCode)
      .filter(Boolean)
      .map(c => String(c).trim().toUpperCase())
      .filter(Boolean);
    return [...new Set(codes)].sort();
  }, [groups]);

  // ============================================================
  // ✅ Auto-generate Group Name from Display Name
  // "Student IT" → "STUDENT_IT"
  // ============================================================
  const generateGroupName = (displayName) => {
    if (!displayName) return '';
    return displayName
      .trim()
      .toUpperCase()
      .replace(/\s+/g, '_')
      .replace(/-+/g, '_')
      .replace(/__+/g, '_')
      .replace(/[^A-Z0-9_]/g, '');
  };

  const handleDisplayNameChange = (e) => {
    const displayName = e.target.value;
    setFormData(prev => ({
      ...prev,
      displayName,
      name: generateGroupName(displayName),
    }));
  };

  const getGroupDisplayName = (group) =>
    group.displayName ||
    group.display_name ||
    group.DisplayName ||
    group.name ||
    '-';

  // ============================================================
  // fetchGroups
  // ============================================================
  const fetchGroups = useCallback(async () => {
    try {
      setLoading(true);
      const res = await axiosInstance.get('/groups', {
        params: { search: debouncedSearch, page, limit: pageSize },
      });
      const data = res.data;
      const list = Array.isArray(data)
        ? data
        : data.groups || data.data || data.items || [];
      setGroups(list);
      setTotalPages(data.totalPages || 1);
      setTotalRecords(data.total || list.length || 0);
      setError(null);
    } catch (err) {
      setError(t('groups.errorLoading'));
    } finally {
      setLoading(false);
    }
  }, [debouncedSearch, page, t]);

  useEffect(() => { fetchGroups(); }, [fetchGroups]);
  useEffect(() => { setPage(1); }, [debouncedSearch]);

  // ============================================================
  // openModal
  // ============================================================
  const openModal = (group = null) => {
    setCurrentGroup(group);
    if (group) {
      const display =
        group.displayName ||
        group.display_name ||
        group.DisplayName ||
        '';
      setFormData({
        displayName: display,
        name: (group.name || generateGroupName(display)).toUpperCase(),
        description: group.description || '',
        branchCode: (
          group.branch_code ||
          group.branchCode ||
          group.BranchCode ||
          'MAIN'
        ).toUpperCase(),
      });
    } else {
      setFormData({
        displayName: '',
        name: '',
        description: '',
        branchCode: 'MAIN',
      });
    }
    setFormErrors({});
    setSubmitError(null);
    setIsModalOpen(true);
  };

  // ============================================================
  // handleSave
  // ============================================================
  const handleSave = async (e) => {
    e.preventDefault();

    // Validate
    const errors = {};
    if (!formData.displayName.trim()) errors.displayName = t('validation.required');
    if (!formData.name.trim()) errors.name = t('validation.required');
    if (!formData.branchCode.trim()) errors.branchCode = t('validation.required');

    setFormErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const payload = {
        name: formData.name.trim().toUpperCase(),
        displayName: formData.displayName.trim(),
        description: formData.description?.trim() || null,
        branchCode: formData.branchCode.trim().toUpperCase(),
      };

      if (currentGroup) {
        await axiosInstance.put(`/groups/${currentGroup.id}`, payload);
      } else {
        await axiosInstance.post('/groups', payload);
      }

      setIsModalOpen(false);
      fetchGroups();
      setToast({
        type: 'success',
        message: currentGroup
          ? t('groups.updateSuccess')
          : t('groups.addSuccess'),
      });
    } catch (err) {
      const msg =
        err.response?.data?.message ||
        err.response?.data?.general ||
        t('common.error');
      const lowerMsg = msg.toLowerCase();

      if (lowerMsg.includes('already exists') || lowerMsg.includes('exists')) {
        setFormErrors(prev => ({ ...prev, displayName: t('groups.alreadyExists') || msg }));
      } else if (lowerMsg.includes('name')) {
        setFormErrors(prev => ({ ...prev, name: msg }));
      } else {
        setSubmitError(msg);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  // ============================================================
  // handleDeleteConfirm
  // ============================================================
  const handleDeleteConfirm = async () => {
    setIsSubmitting(true);
    try {
      await axiosInstance.delete(`/groups/${groupToDelete.id}`);
      setIsDeleteModalOpen(false);
      setGroupToDelete(null);
      fetchGroups();
      setToast({ type: 'success', message: t('groups.deleteSuccess') });
    } catch (err) {
      const msg = err.response?.data?.message || t('common.error');
      setToast({ type: 'error', message: msg });
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-6 text-[var(--text-main)] p-6 bg-[var(--bg-primary)] min-h-screen">
      {toast && (
        <Toast
          type={toast.type}
          message={toast.message}
          onClose={() => setToast(null)}
        />
      )}

      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 p-6 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl">
        <div>
          <h1 className="text-2xl font-bold">{t('groups.title')}</h1>
          <p className="text-[var(--text-muted)] text-sm">{t('groups.subtitle')}</p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={fetchGroups}
            className="px-4 py-2 text-sm border rounded-xl bg-[var(--bg-primary)]"
          >
            {t('common.refresh')}
          </button>
          <Gate permission="GROUP_CREATE">
            <button
              onClick={() => openModal()}
              className="px-4 py-2 text-sm bg-[var(--accent-color)] text-white rounded-xl"
            >
              {t('groups.addNew')}
            </button>
          </Gate>
        </div>
      </div>

      {/* Search */}
      <div className="flex gap-3">
        <input
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          placeholder={t('groups.searchPlaceholder')}
          className="flex-1 max-w-sm px-4 py-2 rounded-xl bg-[var(--bg-secondary)] border border-[var(--border-color)] text-sm"
        />
      </div>

      {/* Table */}
      <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-sm">
            <thead>
              <tr className="border-b bg-[var(--bg-primary)]/50 text-[var(--text-muted)] text-xs uppercase">
                <th className="py-3 px-6">{t('groups.name')}</th>
                <th className="py-3 px-6">{t('groups.displayName')}</th>
                <th className="py-3 px-6">{t('groups.branch')}</th>
                <th className="py-3 px-6">{t('groups.description')}</th>
                <th className="py-3 px-6 text-right">{t('common.actions')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--border-color)]">
              {loading ? (
                <tr>
                  <td colSpan="5" className="p-6 text-center">
                    {t('common.loading')}
                  </td>
                </tr>
              ) : groups.length === 0 ? (
                <tr>
                  <td colSpan="5" className="p-6 text-center">
                    {t('groups.noData')}
                  </td>
                </tr>
              ) : (
                groups.map((g) => (
                  <tr
                    key={g.id}
                    className="hover:bg-[var(--bg-primary)]/40"
                  >
                    <td className="py-4 px-6 font-mono font-medium">
                      {(g.name || '').toUpperCase()}
                    </td>
                    <td className="py-4 px-6">{getGroupDisplayName(g)}</td>
                    <td className="py-4 px-6">
                      <span className="px-2.5 py-1 bg-[var(--accent-color)]/10 text-[var(--accent-color)] border border-[var(--accent-color)]/20 rounded-full text-xs font-bold">
                        {(
                          g.branch_code ||
                          g.branchCode ||
                          '---'
                        )
                          .toString()
                          .toUpperCase()}
                      </span>
                    </td>
                    <td className="py-4 px-6 text-[var(--text-muted)]">
                      {g.description || '---'}
                    </td>
                    <td className="py-4 px-6 text-right space-x-2">
                      <Gate permission="GROUP_UPDATE">
                        <button
                          onClick={() => openModal(g)}
                          className="text-xs px-2.5 py-1 rounded-full bg-[var(--bg-primary)] border"
                        >
                          {t('common.edit')}
                        </button>
                      </Gate>
                      <Gate permission="GROUP_DELETE">
                        <button
                          onClick={() => {
                            setGroupToDelete(g);
                            setIsDeleteModalOpen(true);
                          }}
                          className="text-xs px-2.5 py-1 rounded-full border hover:text-red-500"
                        >
                          {t('common.delete')}
                        </button>
                      </Gate>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        {totalPages > 1 && (
          <div className="flex justify-between items-center p-4 border-t text-xs">
            <span>
              {t('common.showing')} {page} {t('common.of')} {totalPages} -{' '}
              {totalRecords} {t('common.records')}
            </span>
            <div className="flex gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
                className="px-3 py-1.5 rounded-xl border bg-[var(--bg-primary)] disabled:opacity-50"
              >
                {t('common.previous')}
              </button>
              <button
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
                className="px-3 py-1.5 rounded-xl border bg-[var(--bg-primary)] disabled:opacity-50"
              >
                {t('common.next')}
              </button>
            </div>
          </div>
        )}
      </div>

      {/* ============================================================
          Create/Edit Modal
          ============================================================ */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
          <div className="bg-[var(--bg-secondary)] border rounded-xl w-full max-w-md p-6 shadow-xl">
            <h2 className="text-lg font-bold mb-4">
              {currentGroup ? t('common.edit') : t('groups.addNew')}
            </h2>

            {/* Submit Error */}
            {submitError && (
              <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-sm text-red-500 flex items-start gap-2 mb-4">
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

            <form onSubmit={handleSave} className="space-y-4">
              {/* Display Name */}
              <div>
                <label className="block text-xs font-medium mb-1">
                  {t('groups.displayName')} *
                </label>
                <input
                  type="text"
                  value={formData.displayName}
                  onChange={handleDisplayNameChange}
                  placeholder={t('groups.displayNamePlaceholder') || 'Student IT'}
                  className={`w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border text-sm focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]/20 ${
                    formErrors.displayName ? 'border-red-500' : ''
                  }`}
                />
                <p className="text-xs text-[var(--text-muted)] mt-1">
                  {t('groups.displayNameHint')}
                </p>
                {formErrors.displayName && (
                  <span className="text-red-500 text-xs">{formErrors.displayName}</span>
                )}
              </div>

              {/* Group Name (Auto) */}
              <div>
                <label className="block text-xs font-medium mb-1">
                  {t('groups.groupName')} ({t('groups.autoGenerated')})
                </label>
                <input
                  type="text"
                  value={formData.name}
                  readOnly
                  placeholder={t('groups.autoGenerated')}
                  className={`w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)]/60 border text-sm font-mono uppercase cursor-not-allowed opacity-70 ${
                    formErrors.name ? 'border-red-500' : ''
                  }`}
                />
                <p className="text-xs text-[var(--text-muted)] mt-1">
                  {t('groups.groupNameHint')}
                </p>
                {formErrors.name && (
                  <span className="text-red-500 text-xs">{formErrors.name}</span>
                )}
              </div>

              {/* Branch Code */}
              <div>
                <label className="block text-xs font-medium mb-1">
                  {t('groups.branch')} *
                </label>
                <div className="flex gap-2">
                  <input
                    type="text"
                    list="branchCodesList"
                    value={formData.branchCode}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        branchCode: e.target.value.toUpperCase(),
                      })
                    }
                    className="flex-1 px-3 py-2 rounded-xl bg-[var(--bg-primary)] border text-sm uppercase"
                  />
                  <select
                    value={formData.branchCode}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        branchCode: e.target.value.toUpperCase(),
                      })
                    }
                    className="px-3 py-2 rounded-xl bg-[var(--bg-primary)] border text-sm w-32"
                  >
                    <option value="">{t('groups.selectBranch')}</option>
                    {existingBranchCodes.map((code) => (
                      <option key={code} value={code}>
                        {code}
                      </option>
                    ))}
                  </select>
                </div>
                <datalist id="branchCodesList">
                  {existingBranchCodes.map((code) => (
                    <option key={code} value={code} />
                  ))}
                </datalist>
                {formErrors.branchCode && (
                  <span className="text-red-500 text-xs">{formErrors.branchCode}</span>
                )}
              </div>

              {/* Description */}
              <div>
                <label className="block text-xs font-medium mb-1">
                  {t('groups.description')}
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) =>
                    setFormData({ ...formData, description: e.target.value })
                  }
                  className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border text-sm h-20"
                />
              </div>

              {/* Footer */}
              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-4 py-2 text-xs border rounded-xl bg-[var(--bg-primary)]"
                >
                  {t('common.cancel')}
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-4 py-2 text-xs bg-[var(--accent-color)] text-white rounded-xl disabled:opacity-50"
                >
                  {isSubmitting ? t('common.saving') : t('common.save')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete Modal */}
      {isDeleteModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="bg-[var(--bg-secondary)] border rounded-xl w-full max-w-sm p-6 text-center">
            <h3 className="font-bold mb-2">
              {t('common.confirmDelete')} {groupToDelete?.name}?
            </h3>
            <div className="flex justify-center gap-2 mt-4">
              <button
                onClick={() => setIsDeleteModalOpen(false)}
                className="px-4 py-2 text-xs border rounded-xl bg-[var(--bg-primary)]"
              >
                {t('common.cancel')}
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={isSubmitting}
                className="px-4 py-2 text-xs bg-red-600 text-white rounded-xl"
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

export default Groups;