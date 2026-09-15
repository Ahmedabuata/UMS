import React, { useState, useEffect, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import usersApi from '../../api/users.api';
import axiosInstance from '../../api/axiosInstance';
import { handleApiError } from '../../utils/handleApiError';

export const UserRolesModal = ({ userId, onClose, onSuccess }) => {
  const { t, i18n } = useTranslation();
  const [allRoles, setAllRoles] = useState([]);
  const [assignedRoles, setAssignedRoles] = useState([]);
  const [selectedRoleToAdd, setSelectedRoleToAdd] = useState('');
  const [loading, setLoading] = useState(false);
  const [fetching, setFetching] = useState(true);
  const [errorMsg, setErrorMsg] = useState('');

  // Helper to safely extract role ID
  const getRoleId = (r) => {
    if (!r) return '';
    if (typeof r === 'object') return r.id || r._id || r.name || r.roleName || '';
    return r;
  };

  // Helper to resolve role name, prioritizing displayName first
  const getRoleName = (r) => {
    if (!r) return '';
    if (typeof r === 'object') {
      return r.displayName || r.display_name || r.name || r.roleName || r.title || r.id || '';
    }
    // If r is just a string ID, try to find the full role object from allRoles
    const found = allRoles.find(role => getRoleId(role) === r);
    if (found && typeof found === 'object') {
      return found.displayName || found.display_name || found.name || found.roleName || found.title || r;
    }
    return r;
  };

  useEffect(() => {
    const fetchModalData = async () => {
      try {
        setFetching(true);
        setErrorMsg('');
        const [rolesRes, userRolesRes] = await Promise.all([
          axiosInstance.get('/roles'),
          usersApi.getRoles(userId)
        ]);

        const rolesData = rolesRes.data?.data ?? rolesRes.data;
        const rolesArray = Array.isArray(rolesData) ? rolesData : rolesData?.roles || rolesData?.items || rolesData?.data || [];
        setAllRoles(rolesArray);

        const userRolesData = userRolesRes.data?.data ?? userRolesRes.data;
        const userRolesArray = Array.isArray(userRolesData) ? userRolesData : userRolesData?.roles || userRolesData?.data || [];
        setAssignedRoles(userRolesArray);
      } catch (err) {
        setErrorMsg(t('common.fetchError'));
      } finally {
        setFetching(false);
      }
    };
    if (userId) fetchModalData();
  }, [userId, t]);

  const availableRoles = useMemo(() => {
    const assignedIds = new Set(assignedRoles.map(getRoleId));
    return allRoles.filter(r => !assignedIds.has(getRoleId(r)));
  }, [allRoles, assignedRoles]);

  const handleAddRole = () => {
    if (!selectedRoleToAdd) return;
    const roleObj = allRoles.find(r => getRoleId(r) === selectedRoleToAdd);
    if (!roleObj) return;
    if (!assignedRoles.some(r => getRoleId(r) === getRoleId(roleObj))) {
      setAssignedRoles(prev => [...prev, roleObj]);
    }
    setSelectedRoleToAdd('');
  };

  const handleRemoveRole = (roleId) => {
    setAssignedRoles(prev => prev.filter(r => getRoleId(r) !== roleId));
  };

  const handleSave = async () => {
    setLoading(true);
    setErrorMsg('');
    try {
      const roleIds = assignedRoles.map(getRoleId);
      await usersApi.setRoles(userId, roleIds);
      onSuccess(t('users.rolesUpdateSuccess'));
      onClose();
    } catch (error) {
      const errRes = handleApiError(error, 'users.statusToggleError');
      const msg = typeof errRes === 'string' && i18n.exists(errRes) ? t(errRes) : errRes;
      setErrorMsg(msg || t('users.updateError'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
      <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl w-full max-w-lg p-6 space-y-6 shadow-xl text-[var(--text-main)]">
        <div className="flex justify-between items-center border-b border-[var(--border-color)] pb-4">
          <h2 className="text-xl font-bold">{t('users.manageRoles')}</h2>
          <button onClick={onClose} className="text-[var(--text-muted)] hover:text-[var(--text-main)] font-bold">✕</button>
        </div>

        {errorMsg && <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-sm">{errorMsg}</div>}

        {fetching ? (
          <div className="py-12 text-center text-[var(--text-muted)]">{t('common.loading')}</div>
        ) : (
          <div className="space-y-4">
            <div className="flex gap-2">
              <select
                value={selectedRoleToAdd}
                onChange={(e) => setSelectedRoleToAdd(e.target.value)}
                className="flex-1 px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]"
              >
                <option value="">{t('users.assignRole')}</option>
                {availableRoles.map(r => (
                  <option key={getRoleId(r)} value={getRoleId(r)}>{getRoleName(r)}</option>
                ))}
              </select>
              <button
                onClick={handleAddRole}
                disabled={!selectedRoleToAdd}
                type="button"
                className="px-4 py-2 bg-[var(--accent-color)] text-white text-sm font-medium rounded-xl hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {t('common.add')}
              </button>
            </div>

            <div className="border border-[var(--border-color)] rounded-xl p-4 max-h-60 overflow-y-auto space-y-2 bg-[var(--bg-primary)]">
              {assignedRoles.length === 0 ? (
                <p className="text-center text-sm text-[var(--text-muted)]">{t('users.noRolesAssigned')}</p>
              ) : (
                assignedRoles.map((r, idx) => (
                  <div key={getRoleId(r) || idx} className="flex justify-between items-center p-2 rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-color)] text-sm">
                    <span className="font-medium truncate">{getRoleName(r)}</span>
                    <button
                      onClick={() => handleRemoveRole(getRoleId(r))}
                      type="button"
                      className="text-xs text-red-400 hover:text-red-300 font-medium px-2 py-1 bg-red-500/10 rounded-lg hover:bg-red-500/20"
                    >
                      {t('users.removeRole')}
                    </button>
                  </div>
                ))
              )}
            </div>
          </div>
        )}

        <div className="flex justify-end gap-2 border-t border-[var(--border-color)] pt-4">
          <button onClick={onClose} type="button" className="px-4 py-2 rounded-xl border border-[var(--border-color)] text-sm font-medium hover:bg-[var(--bg-primary)]">
            {t('common.cancel')}
          </button>
          <button onClick={handleSave} disabled={loading || fetching} type="button" className="px-4 py-2 rounded-xl bg-green-600 text-white text-sm font-medium hover:bg-green-700 disabled:opacity-50">
            {loading ? t('common.saving') : t('common.save')}
          </button>
        </div>
      </div>
    </div>
  );
};

export default UserRolesModal;