import React, { useState, useMemo } from 'react';
import { Outlet, NavLink, useNavigate } from 'react-router-dom';
import { useTheme } from '../context/ThemeContext.jsx';
import { useLanguage } from '../context/LanguageContext.jsx';
import { useAuth } from '../context/AuthContext.jsx';
import { useTranslation } from 'react-i18next';
import { usePermissions } from '../hooks/usePermissions';

export default function Layout() {
  const { theme, changeTheme, availableThemes } = useTheme();
  const { language, changeLanguage } = useLanguage();
  const { t } = useTranslation();
  const { logout } = useAuth();
  const { hasPermission, isSuperAdmin, loading: permsLoading } = usePermissions();
  const navigate = useNavigate();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  // ============================================================
  // ✅ FIX #5: Close sidebar on logout (mobile issue)
  // ============================================================
  const handleLogout = () => {
    setIsSidebarOpen(false);   // ← ✅ إغلاق Sidebar قبل Logout
    logout();
    navigate('/login');
  };

  // ============================================================
  // Nav Items (Full List)
  // ✅ FIX #1: Added /settings
  // ============================================================
  const navItems = useMemo(() => [
    { to: '/dashboard',   label: t('nav.dashboard'),   permission: null },
    { to: '/users',       label: t('nav.users'),       permission: 'USER_READ' },
    { to: '/roles',       label: t('nav.roles'),       permission: 'ROLE_READ' },
    { to: '/groups',      label: t('nav.groups'),      permission: 'GROUP_READ' },
    { to: '/permissions', label: t('nav.permissions'), permission: 'PERMISSION_READ' },
    { to: '/audit-logs',  label: t('nav.auditLogs'),   permission: 'AUDIT_READ' },
    { to: '/settings',    label: t('nav.settings'),    permission: null },  // 🆕
  ], [t]);

  // ============================================================
  // Filtered Nav Items (based on permissions)
  // ============================================================
  const filteredNavItems = useMemo(() => {
    return navItems.filter(item => {
      if (isSuperAdmin) return true;
      if (!item.permission) return true;
      return hasPermission(item.permission);
    });
  }, [navItems, isSuperAdmin, hasPermission]);

  // ============================================================
  // ✅ FIX #6: Loading Skeleton (prevent flash)
  // ============================================================
  if (permsLoading) {
    return (
      <div className="min-h-screen bg-[var(--bg-primary)] flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[var(--accent-color)] mx-auto mb-4"></div>
          <p className="text-[var(--text-muted)] text-sm">
            {t('common.loading') || 'Loading...'}
          </p>
        </div>
      </div>
    );
  }

  // ============================================================
  // Render
  // ============================================================
  return (
    <div className="min-h-screen bg-[var(--bg-primary)] text-[var(--text-main)] flex">

      {/* Mobile Hamburger Button */}
      <button
        onClick={() => setIsSidebarOpen(!isSidebarOpen)}
        className="lg:hidden fixed top-4 left-4 z-50 p-2 rounded-lg bg-[var(--bg-secondary)] border border-[var(--border-color)]"
        aria-label="Toggle sidebar"
      >
        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d={isSidebarOpen ? "M6 18L18 6M6 6l12 12" : "M4 6h16M4 12h16M4 18h16"}
          />
        </svg>
      </button>

      {/* Overlay (Mobile) */}
      {isSidebarOpen && (
        <div
          className="lg:hidden fixed inset-0 bg-black/50 z-30"
          onClick={() => setIsSidebarOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside className={`
        w-64 bg-[var(--bg-secondary)] border-r border-[var(--border-color)] p-4 flex flex-col
        fixed lg:static inset-y-0 left-0 z-40 transform transition-transform duration-200
        ${isSidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
      `}>
        {/* Theme + Language */}
        <div className="flex justify-between items-center mb-6 gap-2 mt-12 lg:mt-0">
          <select
            value={theme}
            onChange={(e) => changeTheme(e.target.value)}
            className="bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-lg px-2 py-1 text-sm flex-1"
            aria-label="Change theme"
          >
            {availableThemes.map(th => (
              <option key={th.id} value={th.id}>{th.icon} {th.name}</option>
            ))}
          </select>
          <button
            onClick={() => changeLanguage(language === 'ar' ? 'en' : 'ar')}
            className="bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-lg px-3 py-1 text-sm hover:bg-[var(--bg-tertiary)] transition-colors"
            aria-label="Toggle language"
          >
            {language === 'ar' ? 'English' : 'عربي'}
          </button>
        </div>

        {/* App Name */}
        <div className="font-bold text-lg mb-6">{t('app.name')}</div>

        {/* Navigation */}
        <nav className="space-y-2 flex-1">
          {filteredNavItems.map(item => (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={() => setIsSidebarOpen(false)}
              className={({ isActive }) =>
                `block px-3 py-2 rounded-lg transition-colors ${
                  isActive
                    ? 'bg-[var(--accent-color)] text-white'
                    : 'hover:bg-[var(--bg-tertiary)] text-[var(--text-muted)] hover:text-[var(--text-main)]'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
          {filteredNavItems.length === 0 && (
            <p className="text-xs text-[var(--text-muted)] p-3 text-center">
              {t('common.noPermission')}
            </p>
          )}
        </nav>

        {/* Logout */}
        <button
          onClick={handleLogout}
          className="mt-auto bg-red-500/10 text-red-500 border border-red-500/20 rounded-lg px-3 py-2 text-sm hover:bg-red-500/20 transition-colors"
        >
          {t('auth.logout')}
        </button>
      </aside>

      {/* ============================================================ */}
      {/* ✅ FIX #7: Main Content (prevent double scroll)              */}
      {/* ============================================================ */}
      <main className="flex-1 p-6 lg:p-6 pt-16 lg:pt-6 bg-[var(--bg-primary)] overflow-y-auto h-screen">
        <Outlet />
      </main>
    </div>
  );
}