import React, { useState, useEffect, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import usersApi from '../../api/users.api';
import axiosInstance from '../../api/axiosInstance';
import { handleApiError } from '../../utils/handleApiError';

const getGroupId = (g) => typeof g === 'object' ? (g.id || g._id || g.name) : g;
const getGroupName = (g) => typeof g === 'object' ? (g.name || g.display_name || getGroupId(g)) : g;

export const UserGroupsModal = ({ userId, onClose, onSuccess }) => {
  const { t, i18n } = useTranslation();
  const [allGroups, setAllGroups] = useState([]);
  const [assignedGroups, setAssignedGroups] = useState([]);
  const [selectedGroupToAdd, setSelectedGroupToAdd] = useState('');
  const [loading, setLoading] = useState(false);
  const [fetching, setFetching] = useState(true);
  const [errorMsg, setErrorMsg] = useState('');

  useEffect(() => {
    const fetchModalData = async () => {
      try {
        setFetching(true);
        setErrorMsg('');
        const [groupsRes, userGroupsRes] = await Promise.all([
          axiosInstance.get('/groups'),
          usersApi.getGroups(userId)
        ]);

        const groupsData = groupsRes.data?.data ?? groupsRes.data;
        const groupsArray = Array.isArray(groupsData) ? groupsData : groupsData?.groups || groupsData?.items || groupsData?.data || [];
        setAllGroups(groupsArray);

        const userGroupsData = userGroupsRes.data?.data ?? userGroupsRes.data;
        const userGroupsArray = Array.isArray(userGroupsData) ? userGroupsData : userGroupsData?.groups || userGroupsData?.data || [];
        setAssignedGroups(userGroupsArray);
      } catch (err) {
        setErrorMsg(t('common.fetchError'));
      } finally {
        setFetching(false);
      }
    };
    if (userId) fetchModalData();
  }, [userId, t]);

  const availableGroups = useMemo(() => {
    const assignedIds = new Set(assignedGroups.map(getGroupId));
    return allGroups.filter(g => !assignedIds.has(getGroupId(g)));
  }, [allGroups, assignedGroups]);

  const handleAddGroup = () => {
    if (!selectedGroupToAdd) return;
    const groupObj = allGroups.find(g => getGroupId(g) === selectedGroupToAdd);
    if (!groupObj) return;
    if (!assignedGroups.some(g => getGroupId(g) === getGroupId(groupObj))) {
      setAssignedGroups(prev => [...prev, groupObj]);
    }
    setSelectedGroupToAdd('');
  };

  const handleRemoveGroup = (groupId) => {
    setAssignedGroups(prev => prev.filter(g => getGroupId(g) !== groupId));
  };

  const handleSave = async () => {
    setLoading(true);
    setErrorMsg('');
    try {
      const groupIds = assignedGroups.map(getGroupId);
      await usersApi.setGroups(userId, groupIds);
      onSuccess(t('users.groupsUpdateSuccess'));
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
          <h2 className="text-xl font-bold">{t('users.manageGroups')}</h2>
          <button onClick={onClose} className="text-[var(--text-muted)] hover:text-[var(--text-main)] font-bold">✕</button>
        </div>

        {errorMsg && <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-sm">{errorMsg}</div>}

        {fetching ? (
          <div className="py-12 text-center text-[var(--text-muted)]">{t('common.loading')}</div>
        ) : (
          <div className="space-y-4">
            <div className="flex gap-2">
              <select
                value={selectedGroupToAdd}
                onChange={(e) => setSelectedGroupToAdd(e.target.value)}
                className="flex-1 px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]"
              >
                <option value="">{t('users.assignGroup')}</option>
                {availableGroups.map(g => (
                  <option key={getGroupId(g)} value={getGroupId(g)}>{getGroupName(g)}</option>
                ))}
              </select>
              <button
                onClick={handleAddGroup}
                disabled={!selectedGroupToAdd}
                type="button"
                className="px-4 py-2 bg-[var(--accent-color)] text-white text-sm font-medium rounded-xl hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {t('common.add')}
              </button>
            </div>

            <div className="border border-[var(--border-color)] rounded-xl p-4 max-h-60 overflow-y-auto space-y-2 bg-[var(--bg-primary)]">
              {assignedGroups.length === 0 ? (
                <p className="text-center text-sm text-[var(--text-muted)]">{t('users.noGroupsAssigned')}</p>
              ) : (
                assignedGroups.map((g, idx) => (
                  <div key={getGroupId(g) || idx} className="flex justify-between items-center p-2 rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-color)] text-sm">
                    <span className="font-medium truncate">{getGroupName(g)}</span>
                    <button
                      onClick={() => handleRemoveGroup(getGroupId(g))}
                      type="button"
                      className="text-xs text-red-400 hover:text-red-300 font-medium px-2 py-1 bg-red-500/10 rounded-lg hover:bg-red-500/20"
                    >
                      {t('users.removeGroup')}
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

export default UserGroupsModal;