import React, { useState, useEffect, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import rolesApi from '../api/roles.api';
import permissionsApi from '../api/permissions.api';
import { handleApiError } from '../utils/handleApiError';
import { Toast } from '../components/Toast';
import Gate from '../components/Gate';

/**
 * RolePermissions Page
 * 
 * Full-page editor for a role's permissions.
 * 
 * ARCHITECTURE:
 * - GOLDEN RULE #11: Bulk Operations (syncPermissions) — no individual add/remove.
 * - GOLDEN RULE #12: Enterprise Pattern (Full Page, NOT Modal).
 * - Uses CSS Variables + Tailwind (theme-aware).
 * - Uses i18n for all text (multi-language).
 * 
 * FLOW:
 * 1. Load: Promise.all([getRole, getAllPermissions, getRolePermissions])
 * 2. Display: Search + Grouped by Module + Checkboxes
 * 3. User toggles: Local state only (NO HTTP requests)
 * 4. Save: PUT /api/roles/{id}/permissions (SINGLE request)
 * 
 * THEME VARIABLES USED:
 * - --bg-primary, --bg-secondary, --bg-accent
 * - --text-main, --text-muted
 * - --accent-color, --accent-hover
 * - --border-color
 * 
 * ACCESS: /roles/:id/permissions
 */
const RolePermissions = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { t } = useTranslation();

  // ============================================================
  // State
  // ============================================================
  const [role, setRole] = useState(null);
  const [allPermissions, setAllPermissions] = useState([]);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [fetching, setFetching] = useState(true);
  const [saving, setSaving] = useState(false);
  const [toast, setToast] = useState(null);
  const [search, setSearch] = useState('');

  // ============================================================
  // Step 1: Load Data on Mount
  // ============================================================
  useEffect(() => {
    const fetchData = async () => {
      try {
        setFetching(true);

        // Parallel: Role + All Permissions + Role's Permissions
        const [roleRes, allPermsRes, rolePermsRes] = await Promise.all([
          rolesApi.getById(id),
          permissionsApi.getAll({ pageSize: 500 }),
          rolesApi.getPermissions(id, { pageSize: 500 }),
        ]);

        // ============================================================
        // Extract role data
        // ============================================================
        const roleData = roleRes.data?.data ?? roleRes.data;
        setRole(roleData?.role || roleData);

        // ============================================================
        // Extract all permissions
        // ============================================================
        const allData = allPermsRes.data?.data ?? allPermsRes.data;
        const allList = Array.isArray(allData)
          ? allData
          : allData.items || allData.permissions || [];
        setAllPermissions(allList.filter(Boolean));

        // ============================================================
        // Extract role's assigned permission IDs
        // ============================================================
        const rolePermsData = rolePermsRes.data?.data ?? rolePermsRes.data;
        const rolePerms = Array.isArray(rolePermsData)
          ? rolePermsData
          : rolePermsData.items || rolePermsData.permissions || [];
        setSelectedIds(new Set(rolePerms.map(p => p.id).filter(Boolean)));

      } catch (err) {
        const errRes = handleApiError(err, 'roles.fetchError');
        setToast({ type: 'error', message: t(errRes) || t('roles.fetchError') });
      } finally {
        setFetching(false);
      }
    };
    if (id) fetchData();
  }, [id, t]);

  // ============================================================
  // Step 2: Filter by Search (useMemo for Performance)
  // ============================================================
  const filtered = useMemo(() => {
    if (!search) return allPermissions;
    const s = search.toLowerCase();
    return allPermissions.filter(p =>
      (p.permissionName || '').toLowerCase().includes(s) ||
      (p.description || '').toLowerCase().includes(s) ||
      (p.module || '').toLowerCase().includes(s)
    );
  }, [allPermissions, search]);

  // ============================================================
  // Step 3: Group by Module (useMemo for Performance)
  // ============================================================
  const grouped = useMemo(() => {
    return filtered.reduce((acc, p) => {
      const mod = p.module || 'General';
      if (!acc[mod]) acc[mod] = [];
      acc[mod].push(p);
      return acc;
    }, {});
  }, [filtered]);

  // ============================================================
  // Step 4: Handlers
  // ============================================================
  const togglePerm = (permId) => {
    setSelectedIds(prev => {
      const next = new Set(prev);
      if (next.has(permId)) next.delete(permId);
      else next.add(permId);
      return next;
    });
  };

  const handleSelectAll = () => {
    setSelectedIds(prev => {
      const next = new Set(prev);
      filtered.forEach(p => next.add(p.id));
      return next;
    });
  };

  const handleDeselectAll = () => {
    setSelectedIds(prev => {
      const next = new Set(prev);
      filtered.forEach(p => next.delete(p.id));
      return next;
    });
  };

  // ============================================================
  // Step 5: Save (GOLDEN RULE #11: Bulk Operation)
  // ============================================================
  /**
   * Sends ALL selected permissions in ONE request.
   * 
   * Backend behavior:
   * - DELETE old permissions
   * - INSERT new permissions
   * - All within a Transaction (atomic)
   * - Revoke tokens ONCE
   * - Audit log ONCE
   * 
   * ⚠️ After save: user must logout + login.
   */
  const handleSave = async () => {
    setSaving(true);
    try {
      await rolesApi.syncPermissions(id, Array.from(selectedIds));
      setToast({
        type: 'success',
        message: t('roles.permissionsSaved'),
      });
    } catch (err) {
      const errRes = handleApiError(err, 'roles.saveError');
      setToast({ type: 'error', message: t(errRes) || t('roles.saveError') });
    } finally {
      setSaving(false);
    }
  };

  // ============================================================
  // Step 6: Render
  // ============================================================
  return (
    <div className="space-y-6 p-6 min-h-screen bg-[var(--bg-primary)] text-[var(--text-main)]">
      {toast && <Toast {...toast} onClose={() => setToast(null)} />}

      {/* ==================== Header ==================== */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 bg-[var(--bg-secondary)] border border-[var(--border-color)] p-6 rounded-xl">
        <div>
          <h1 className="text-2xl font-bold text-[var(--text-main)]">
            {t('roles.managePermissions')}
          </h1>
          <p className="text-sm text-[var(--text-muted)] mt-1">
            {role?.roleName && `${t('roles.role')}: ${role.roleName}`}
          </p>
        </div>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => navigate('/roles')}
            className="px-4 py-2 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm font-medium text-[var(--text-main)] hover:bg-[var(--bg-secondary)] transition-colors"
          >
            ← {t('roles.backToRoles')}
          </button>
          <Gate permission="ROLE_WRITE">
            <button
              type="button"
              onClick={handleSave}
              disabled={saving || fetching}
              className="px-5 py-2 rounded-xl bg-[var(--accent-color)] hover:bg-[var(--accent-hover)] text-white text-sm font-medium disabled:opacity-50 flex items-center gap-2 transition-colors"
            >
              {saving && (
                <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
              )}
              {saving ? t('common.saving') : t('roles.savePermissions')}
            </button>
          </Gate>
        </div>
      </div>

      {/* ==================== Toolbar ==================== */}
      <div className="flex flex-col md:flex-row justify-between items-center gap-4 bg-[var(--bg-secondary)] border border-[var(--border-color)] p-4 rounded-xl">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder={t('roles.searchPermissions')}
          className="px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm w-full md:w-80 text-[var(--text-main)] placeholder:text-[var(--text-muted)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]"
        />
        <div className="flex gap-2 w-full md:w-auto">
          <button
            type="button"
            onClick={handleSelectAll}
            className="px-3 py-2 text-xs rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-[var(--text-muted)] hover:bg-[var(--bg-secondary)] hover:text-[var(--text-main)] font-medium transition-colors"
          >
            {t('roles.selectAll')}
          </button>
          <button
            type="button"
            onClick={handleDeselectAll}
            className="px-3 py-2 text-xs rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-[var(--text-muted)] hover:bg-[var(--bg-secondary)] hover:text-[var(--text-main)] font-medium transition-colors"
          >
            {t('roles.deselectAll')}
          </button>
        </div>
      </div>

      {/* ==================== Info Bar ==================== */}
      {!fetching && allPermissions.length > 0 && (
        <div className="flex items-center justify-between px-4 text-sm text-[var(--text-muted)]">
          <span>
            <strong className="text-[var(--accent-color)]">{selectedIds.size}</strong> {t('roles.selected')}
          </span>
          <span>
            {filtered.length} / {allPermissions.length} {t('roles.totalPermissions')}
          </span>
        </div>
      )}

      {/* ==================== Permissions Grid ==================== */}
      {fetching ? (
        <div className="py-20 text-center text-[var(--text-muted)]">
          {t('common.loading')}
        </div>
      ) : Object.keys(grouped).length === 0 ? (
        <div className="py-20 text-center text-[var(--text-muted)] bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl">
          {t('roles.noPermissions')}
        </div>
      ) : (
        <div className="space-y-6">
          {Object.entries(grouped).map(([mod, perms]) => {
            const selectedInModule = perms.filter(p => selectedIds.has(p.id)).length;
            return (
              <div
                key={mod}
                className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl p-6 space-y-4"
              >
                {/* Module Header */}
                <h3 className="text-lg font-bold uppercase tracking-wider text-[var(--accent-color)] border-b border-[var(--border-color)] pb-2 flex justify-between items-center">
                  <span>{mod}</span>
                  <span className="text-xs bg-[var(--bg-accent)] text-[var(--accent-color)] px-2.5 py-1 rounded-full font-normal">
                    {selectedInModule}/{perms.length}
                  </span>
                </h3>

                {/* Permissions Grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                  {perms.map(p => {
                    const isChecked = selectedIds.has(p.id);
                    return (
                      <div
                        key={p.id}
                        className={`flex items-start p-3 rounded-xl border transition-colors ${
                          isChecked
                            ? 'bg-[var(--bg-accent)] border-[var(--accent-color)]/40'
                            : 'bg-[var(--bg-primary)] border-[var(--border-color)]'
                        }`}
                      >
                        <label className="flex items-start gap-3 cursor-pointer flex-1">
                          <input
                            type="checkbox"
                            checked={isChecked}
                            onChange={() => togglePerm(p.id)}
                            className="mt-1 h-4 w-4 rounded border-[var(--border-color)] text-[var(--accent-color)] focus:ring-[var(--accent-color)]"
                          />
                          <div className="text-sm min-w-0">
                            <p
                              className={`font-mono text-xs font-semibold truncate ${
                                isChecked ? 'text-[var(--accent-color)]' : 'text-[var(--text-main)]'
                              }`}
                              title={p.permissionName}
                            >
                              {p.permissionName}
                            </p>
                            {p.description && (
                              <p className="text-xs text-[var(--text-muted)] mt-0.5 truncate" title={p.description}>
                                {p.description}
                              </p>
                            )}
                          </div>
                        </label>
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default RolePermissions;