import React, { useEffect, useState } from 'react';
import { useTheme } from '../context/ThemeContext.jsx';
import { useLanguage } from '../context/LanguageContext.jsx';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext.jsx';
import axiosInstance from '../api/axiosInstance.js';

export default function Dashboard() {
  const { theme } = useTheme();
  const { language } = useLanguage();
  const { t } = useTranslation();
  const { user } = useAuth();

  const roles = user?.roles || user?.assignedRoles || [];
  const perms = user?.permissions || [];
  const [loadingRoles, setLoadingRoles] = useState(!roles.length);

  const [stats, setStats] = useState({
    totalUsers: 0,
    totalRoles: 0,
    totalGroups: 0,
    auditLogsToday: 0,
  });
  const [loadingStats, setLoadingStats] = useState(false);

  const fetchStats = async () => {
    setLoadingStats(true);
    try {
      const results = await Promise.allSettled([
        axiosInstance.get('/users/count'),
        axiosInstance.get('/roles/count'),
        axiosInstance.get('/groups/count'),
        axiosInstance.get('/audit-logs/count'),
      ]);
      const getCount = (r) => {
        if (r.status!== 'fulfilled') return 0;
        const d = r.value.data;
        return d?.count?? d?.data?.count?? d?.total?? 0;
      };
      setStats({
        totalUsers: getCount(results[0]),
        totalRoles: getCount(results[1]),
        totalGroups: getCount(results[2]),
        auditLogsToday: getCount(results[3]),
      });
    } finally {
      setLoadingStats(false);
    }
  };

  useEffect(() => {
    if (roles.length) {
      setLoadingRoles(false);
      return;
    }
    const fetchMe = async () => {
      setLoadingRoles(true);
      try {
        // Gold الـ backend Gold سيعيد Gold الآن Gold 56 صلاحية Gold بعد Gold إصلاح user_roles Gold
        await axiosInstance.get('/auth/me'); // Gold يحدث Gold الـ user Gold في AuthContext Gold تلقائياً Gold إذا Gold كان Gold interceptor Gold موجوداً Gold
      } catch {
        // Gold لا يوجد endpoint Gold
      } finally {
        setLoadingRoles(false);
      }
    };
    fetchMe();
  }, []); // ✅ Gold مرة Gold واحدة Gold فقط Gold - لا يعتمد Gold على user Gold المتغير Gold

  useEffect(() => {
    fetchStats();
  }, []);

  return (
    <div className="space-y-6">
      <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl p-6">
        <h1 className="text-2xl font-bold">{t('dashboard.welcome')} {user?.username || user?.email || ''}</h1>
        <p className="text-[var(--text-muted)] text-sm mt-2">{t('dashboard.subtitle')}</p>
        <div className="mt-2 text-xs text-[var(--text-muted)]">{user?.email}</div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {[
          { label: t('dashboard.totalUsers'), value: stats.totalUsers, icon: '👥' },
          { label: t('dashboard.totalRoles'), value: stats.totalRoles, icon: '🛡️' },
          { label: t('dashboard.totalGroups'), value: stats.totalGroups, icon: '📂' },
          { label: t('dashboard.auditLogs'), value: stats.auditLogsToday, icon: '📊' },
        ].map(card => (
          <div key={card.label} className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl p-5 flex items-center justify-between">
            <div>
              <p className="text-sm text-[var(--text-muted)]">{card.label}</p>
              <h3 className="text-2xl font-bold mt-1">{loadingStats? '...' : card.value}</h3>
            </div>
            <div className="p-3 bg-[var(--accent-color)]/10 text-[var(--accent-color)] rounded-lg">{card.icon}</div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl p-6">
          <h2 className="font-semibold">{t('dashboard.assignedRoles')}</h2>
          <div className="mt-3">
            {loadingRoles? (
              <p className="text-[var(--text-muted)] text-sm">{t('common.loading')}</p>
            ) : roles.length? (
              <div className="flex flex-wrap gap-2">
                {roles.map(r => (
                  <span key={typeof r === 'string'? r : r.name || r.role_name} className="bg-[var(--accent-color)]/20 text-[var(--accent-color)] border border-[var(--accent-color)]/30 rounded-full px-3 py-1 text-xs font-medium">
                    {typeof r === 'string'? r : r.name || r.role_name}
                  </span>
                ))}
              </div>
            ) : (
              <p className="text-[var(--text-muted)] text-sm mt-2">{t('dashboard.noRoles')}</p>
            )}
          </div>
        </div>

        <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl p-6">
          <h2 className="font-semibold">{t('dashboard.themeInfo')}</h2>
          <div className="mt-3 space-y-1 text-sm">
            <div>{t('dashboard.currentTheme')}: <span className="font-bold uppercase">{theme}</span></div>
            <div>{t('dashboard.currentLang')}: <span className="font-bold">{language}</span></div>
            <div>{t('dashboard.sessionPerms')}: <span className="font-bold">{perms.length}</span></div>
            {/* ✅ Gold لا تعرض Gold كل الصلاحيات Gold كـ نص Gold - اعرض Gold عددها Gold فقط Gold */}
            {perms.length > 0 && perms.length <= 10 && (
              <div className="text-xs text-[var(--text-muted)] mt-2 break-words">{perms.map(p => typeof p === 'string'? p : p.permission_name || p.name).join(', ')}</div>
            )}
            {perms.length > 10 && (
              <div className="text-xs text-[var(--text-muted)] mt-2">{t('dashboard.permsCountNote', { count: perms.length }) || `تم تحميل ${perms.length} صلاحية بنجاح`}</div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}