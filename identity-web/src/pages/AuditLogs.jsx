import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import auditLogsApi from '../api/auditLogs.api';
import { handleApiError } from '../utils/handleApiError';
import { useDebounce } from '../hooks/useDebounce';
import { Toast } from '../components/Toast';
import Gate from '../components/Gate';

const LIMIT = 20;

// Available action filters (matching backend UPPER_SNAKE_CASE format)
const ACTION_OPTIONS = [
  { value: 'All', label: 'All Actions' },
  { value: 'ACTIVATE_USER', label: 'Activate User' },
  { value: 'DEACTIVATE_USER', label: 'Deactivate User' },
  { value: 'SOFT_DELETE_USER', label: 'Delete User' },
  { value: 'REVOKE_ALL_TOKENS', label: 'Revoke All Tokens' },
  { value: 'BULK_SYNC_PERMISSIONS', label: 'Bulk Sync Permissions' },
  { value: 'ASSIGN_PERMISSION', label: 'Assign Permission' },
  { value: 'REMOVE_PERMISSION', label: 'Remove Permission' },
  { value: 'CREATE_USER', label: 'Create User' },
  { value: 'UPDATE_USER', label: 'Update User' },
  { value: 'LOGIN', label: 'Login' },
  { value: 'LOGOUT', label: 'Logout' },
];

const AuditLogs = () => {
  const { t, i18n } = useTranslation();

  // ============================================================
  // State
  // ============================================================
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);
  const [toast, setToast] = useState(null);

  // Filters
  const [search, setSearch] = useState('');
  const [actionFilter, setActionFilter] = useState('All');
  const [userFilter, setUserFilter] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');

  // Pagination
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);

  // Details Modal
  const [selectedLog, setSelectedLog] = useState(null);

  const debouncedSearch = useDebounce(search, 500);
  const debouncedUser = useDebounce(userFilter, 500);

  // ============================================================
  // Validation
  // ============================================================
  const isInvalidDateRange = useMemo(() => {
    return dateFrom && dateTo && new Date(dateFrom) > new Date(dateTo);
  }, [dateFrom, dateTo]);

  // ============================================================
  // Build Query Params (matching Backend: page, pageSize, from, to, etc.)
  // ============================================================
  const buildFilterParams = useCallback(() => {
    const p = { page, pageSize: LIMIT };
    if (debouncedSearch) p.search = debouncedSearch;
    if (actionFilter && actionFilter !== 'All') p.action = actionFilter;
    if (debouncedUser) p.user = debouncedUser;
    if (dateFrom) p.from = dateFrom;   // ✅ from (not date_from)
    if (dateTo) p.to = dateTo;         // ✅ to (not date_to)
    return p;
  }, [page, debouncedSearch, actionFilter, debouncedUser, dateFrom, dateTo]);

  // ============================================================
  // Fetch Logs
  // ============================================================
  const fetchLogs = useCallback(async () => {
    if (isInvalidDateRange) {
      setToast({ type: 'error', message: t('auditLogs.invalidDateRange') });
      return;
    }

    try {
      setLoading(true);
      const res = await auditLogsApi.getAll(buildFilterParams());
      const body = res?.data?.data ?? res?.data;

      // ✅ Backend uses: items, total, page, pageSize
      const list = Array.isArray(body)
        ? body
        : body.items || body.data || [];
      setLogs(list.filter(Boolean));
      setTotal(body.total || list.length);
    } catch (err) {
      const errRes = handleApiError(err, 'auditLogs.fetchError');
      setToast({
        type: 'error',
        message: typeof errRes === 'string' && i18n.exists(errRes) ? t(errRes) : t('auditLogs.fetchError'),
      });
    } finally {
      setLoading(false);
    }
  }, [buildFilterParams, isInvalidDateRange, t, i18n]);

  useEffect(() => { fetchLogs(); }, [fetchLogs]);
  useEffect(() => { setPage(1); }, [debouncedSearch, debouncedUser, actionFilter, dateFrom, dateTo]);

  // ============================================================
  // Export CSV
  // ============================================================
  const handleExport = async () => {
    if (isInvalidDateRange) {
      setToast({ type: 'error', message: t('auditLogs.invalidDateRange') });
      return;
    }

    try {
      setExporting(true);
      const params = { ...buildFilterParams() };
      delete params.page;
      delete params.pageSize;

      const res = await auditLogsApi.export(params);

      let filename = `audit-logs-${new Date().toISOString().split('T')[0]}.csv`;
      const disposition = res.headers['content-disposition'] || res.headers['Content-Disposition'];
      if (disposition) {
        const m = disposition.match(/filename="?([^"]+)"?/);
        if (m) filename = m[1];
      }

      const blob = new Blob([res.data], { type: 'text/csv' });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => window.URL.revokeObjectURL(url), 1000);

      setToast({ type: 'success', message: t('auditLogs.exportSuccess') });
    } catch (err) {
      const errRes = handleApiError(err, 'auditLogs.exportError');
      setToast({
        type: 'error',
        message: typeof errRes === 'string' && i18n.exists(errRes) ? t(errRes) : t('auditLogs.exportError'),
      });
    } finally {
      setExporting(false);
    }
  };

  // ============================================================
  // Helpers
  // ============================================================
  const parseJsonSafe = (str) => {
    if (!str) return null;
    try {
      let parsed = typeof str === 'string' ? JSON.parse(str) : str;
      if (typeof parsed === 'string') parsed = JSON.parse(parsed);
      return parsed;
    } catch {
      return null;
    }
  };

  const getDisplayUser = (log) => {
    return (
      log.userName ||
      log.username ||
      log.createdByName ||
      log.userId ||
      '-'
    );
  };

  const getActorName = (log) => {
    return log.createdByName || '-';
  };

  const getDetailsText = (log) => {
    const oldV = parseJsonSafe(log.oldValues);
    const newV = parseJsonSafe(log.newValues);

    if (oldV || newV) {
      // Special case: IsActive toggle
      const oldActive = oldV?.IsActive ?? oldV?.isActive;
      const newActive = newV?.IsActive ?? newV?.isActive;
      if (oldActive !== undefined || newActive !== undefined) {
        return `IsActive: ${oldActive} → ${newActive}`;
      }
      const str = JSON.stringify(oldV || newV);
      return str.length > 80 ? str.slice(0, 80) + '…' : str;
    }
    return '-';
  };

  const totalPages = Math.ceil(total / LIMIT) || 1;

  // ============================================================
  // Render
  // ============================================================
  return (
    <div className="space-y-6 text-[var(--text-main)] p-6 bg-[var(--bg-primary)] min-h-screen">
      {toast && <Toast type={toast.type} message={toast.message} onClose={() => setToast(null)} />}

      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold">{t('auditLogs.title')}</h1>
          <p className="text-sm text-[var(--text-muted)] mt-1">{t('auditLogs.subtitle')}</p>
        </div>
        <Gate permission="AUDIT_READ">
          <button
            type="button"
            onClick={handleExport}
            disabled={exporting}
            className="text-sm px-4 py-2 rounded-xl bg-[var(--accent-color)] text-white hover:opacity-90 disabled:opacity-50 flex items-center gap-2 font-medium"
          >
            {exporting && (
              <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
            )}
            {exporting ? t('common.exporting') : t('auditLogs.exportBtn')}
          </button>
        </Gate>
      </div>

      {/* Filters */}
      <div className="grid grid-cols-1 md:grid-cols-5 gap-3 bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl p-4">
        <input
          type="text"
          placeholder={t('auditLogs.searchPlaceholder')}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm text-[var(--text-main)] placeholder:text-[var(--text-muted)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]"
        />
        <select
          value={actionFilter}
          onChange={(e) => setActionFilter(e.target.value)}
          className="px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm text-[var(--text-main)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]"
        >
          {ACTION_OPTIONS.map(opt => (
            <option key={opt.value} value={opt.value}>{opt.label}</option>
          ))}
        </select>
        <input
          type="text"
          placeholder={t('auditLogs.filterUser')}
          value={userFilter}
          onChange={(e) => setUserFilter(e.target.value)}
          className="px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm text-[var(--text-main)] placeholder:text-[var(--text-muted)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-color)]"
        />
        <input
          type="date"
          value={dateFrom}
          onChange={(e) => setDateFrom(e.target.value)}
          className="px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm text-[var(--text-main)] focus:outline-none"
        />
        <input
          type="date"
          value={dateTo}
          onChange={(e) => setDateTo(e.target.value)}
          className="px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm text-[var(--text-main)] focus:outline-none"
        />
      </div>

      {isInvalidDateRange && (
        <p className="text-xs text-[var(--text-muted)] bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl p-3">
          ⚠️ {t('auditLogs.invalidDateRange')}
        </p>
      )}

      {/* Table */}
      <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-sm">
            <thead>
              <tr className="border-b border-[var(--border-color)] bg-[var(--bg-primary)]/50 text-[var(--text-muted)] text-xs uppercase tracking-wider">
                <th className="p-4">{t('auditLogs.action')}</th>
                <th className="p-4">{t('auditLogs.user')}</th>
                <th className="p-4">{t('auditLogs.details')}</th>
                <th className="p-4">{t('auditLogs.ipAddress')}</th>
                <th className="p-4 text-right">{t('auditLogs.timestamp')}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--border-color)]">
              {loading ? (
                <tr><td colSpan="5" className="p-6 text-center text-[var(--text-muted)]">{t('common.loading')}</td></tr>
              ) : logs.length === 0 ? (
                <tr><td colSpan="5" className="p-6 text-center text-[var(--text-muted)]">{t('auditLogs.noLogs')}</td></tr>
              ) : (
                logs.map((log) => (
                  <tr
                    key={log.id}
                    onClick={() => setSelectedLog(log)}
                    className="hover:bg-[var(--bg-primary)]/40 transition-colors cursor-pointer"
                  >
                    <td className="p-4">
                      <span className="px-2.5 py-1 rounded-full text-xs bg-[var(--accent-color)]/10 text-[var(--accent-color)] border border-[var(--accent-color)]/20 font-mono">
                        {log.action || '-'}
                      </span>
                    </td>
                    <td className="p-4 font-medium">{getDisplayUser(log)}</td>
                    <td className="p-4 text-[var(--text-muted)] text-xs truncate max-w-xs">{getDetailsText(log)}</td>
                    <td className="p-4 text-[var(--text-muted)] font-mono text-xs">{log.ipAddress || '::1'}</td>
                    <td className="p-4 text-right text-[var(--text-muted)] text-xs">
                      {log.timestamp ? new Date(log.timestamp).toLocaleString() : '-'}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {totalPages > 1 && (
          <div className="flex justify-between items-center p-4 border-t border-[var(--border-color)] text-xs text-[var(--text-muted)]">
            <span>{t('common.page')} {page} / {totalPages} - {total}</span>
            <div className="flex gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage(p => p - 1)}
                className="px-3 py-1.5 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] disabled:opacity-50"
              >
                {t('common.prev')}
              </button>
              <button
                disabled={page >= totalPages}
                onClick={() => setPage(p => p + 1)}
                className="px-3 py-1.5 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] disabled:opacity-50"
              >
                {t('common.next')}
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Details Drawer */}
      {selectedLog && (
        <div
          className="fixed inset-0 z-50 flex justify-end bg-black/50 backdrop-blur-sm"
          onClick={() => setSelectedLog(null)}
        >
          <div
            className="bg-[var(--bg-secondary)] border-l border-[var(--border-color)] w-full max-w-lg h-full p-6 overflow-y-auto space-y-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex justify-between items-center">
              <h2 className="text-xl font-bold text-[var(--text-main)]">
                {t('auditLogs.details') || 'Log Details'}
              </h2>
              <button
                onClick={() => setSelectedLog(null)}
                className="px-3 py-1.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm"
              >
                {t('common.close')}
              </button>
            </div>

            <div className="space-y-3 text-sm">
              <p><span className="text-[var(--text-muted)]">Action:</span> <strong>{selectedLog.action}</strong></p>
              <p><span className="text-[var(--text-muted)]">User:</span> {getDisplayUser(selectedLog)}</p>
              <p><span className="text-[var(--text-muted)]">Actor:</span> {getActorName(selectedLog)}</p>
              <p><span className="text-[var(--text-muted)]">IP:</span> {selectedLog.ipAddress || '::1'}</p>
              <p><span className="text-[var(--text-muted)]">Timestamp:</span> {new Date(selectedLog.timestamp).toLocaleString()}</p>
            </div>

            <div className="pt-4 space-y-3">
              <h3 className="font-bold text-sm">Old vs New Values</h3>

              <div className="bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl p-3">
                <p className="font-bold text-xs text-[var(--text-muted)] mb-2">Old Values:</p>
                <pre className="whitespace-pre-wrap text-xs text-[var(--text-main)]">
                  {JSON.stringify(parseJsonSafe(selectedLog.oldValues), null, 2) || '-'}
                </pre>
              </div>

              <div className="bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl p-3">
                <p className="font-bold text-xs text-[var(--text-muted)] mb-2">New Values:</p>
                <pre className="whitespace-pre-wrap text-xs text-[var(--text-main)]">
                  {JSON.stringify(parseJsonSafe(selectedLog.newValues), null, 2) || '-'}
                </pre>
              </div>

              <div className="bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-xl p-3">
                <p className="font-bold text-xs text-[var(--text-muted)] mb-2">Full JSON:</p>
                <pre className="text-xs overflow-x-auto whitespace-pre-wrap text-[var(--text-main)]">
                  {JSON.stringify(selectedLog, null, 2)}
                </pre>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AuditLogs;