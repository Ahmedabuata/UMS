import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import axiosInstance from '../api/axiosInstance';
import { handleApiError } from '../utils/handleApiError';
import { Toast } from '../components/Toast';

/**
 * Settings Page (Production-Ready with all fixes)
 * 
 * FEATURES:
 * - 4 Tabs: Profile, Security, Preferences, Sessions
 * - Fallback Chain: /users/me → /users/profile → /users/{id}
 * - Show/Hide Password toggle (3 fields)
 * - Email validation (client-side)
 * - Current Session badge
 * - Spinner in Save buttons
 * - Safe error handling (no [object Object])
 * - Refresh session after profile update
 * - Sync user state via updateUser() after save
 * 
 * FIXES APPLIED:
 * - Phone validation: ignore empty/whitespace (no false errors)
 * - Send null instead of empty string for optional fields
 * - Call updateUser() to sync AuthContext after save
 * - Fallback Chain for robust endpoint discovery
 * - Safe error extraction (no [object Object])
 */
const Settings = () => {
  const { t, i18n } = useTranslation();
  
  // ✅ updateUser is the KEY to fixing stale user data on refresh
  const { user, refreshSession, updateUser } = useAuth();

  const [activeTab, setActiveTab] = useState('profile');
  const [toast, setToast] = useState(null);
  const [saving, setSaving] = useState(false);

  // ============================================================
  // Profile State
  // ============================================================
  const [profile, setProfile] = useState({
    username: '',
    email: '',
    phoneNumber: '',
    firstName: '',
    lastName: '',
  });
  const [profileErrors, setProfileErrors] = useState({});

  // ============================================================
  // Password State
  // ============================================================
  const [passwordData, setPasswordData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });
  const [passwordErrors, setPasswordErrors] = useState({});

  // ============================================================
  // Show/Hide Password (UX enhancement)
  // ============================================================
  const [showPasswords, setShowPasswords] = useState({
    current: false,
    new: false,
    confirm: false,
  });

  const togglePasswordVisibility = (field) => {
    setShowPasswords(prev => ({ ...prev, [field]: !prev[field] }));
  };

  // ============================================================
  // Sessions State
  // ============================================================
  const [sessions, setSessions] = useState([]);
  const [sessionsLoading, setSessionsLoading] = useState(false);

  // ============================================================
  // Safe Error Message Extractor
  // ============================================================
  /**
   * Extracts a string message from various error response shapes.
   * 
   * WHY WE NEED THIS:
   * - Backend may return errors as: string, object with message, object with keyed arrays
   * - Passing an object directly to toast → shows "[object Object]"
   * 
   * @param {any} errRes - Error response from API
   * @param {string} fallbackKey - i18n key to use if extraction fails
   * @returns {string} - Human-readable error message
   */
  const extractErrorMessage = (errRes, fallbackKey) => {
    if (typeof errRes === 'string') {
      return i18n.exists(errRes) ? t(errRes) : errRes;
    }
    if (errRes && typeof errRes === 'object') {
      // Try common fields first
      const msg = errRes.message || errRes.title || errRes.error;
      if (typeof msg === 'string') {
        return i18n.exists(msg) ? t(msg) : msg;
      }
      // Try to extract from keyed errors (e.g., { email: ['Invalid'] })
      for (const value of Object.values(errRes)) {
        if (typeof value === 'string') {
          return i18n.exists(value) ? t(value) : value;
        }
        if (Array.isArray(value) && value[0]) {
          const first = value[0];
          return i18n.exists(first) ? t(first) : first;
        }
      }
    }
    return t(fallbackKey);
  };

  // ============================================================
  // Load Profile from AuthContext
  // ============================================================
  /**
   * WHY WE DEPEND ON [user]:
   * - When updateUser() is called, user state changes
   * - This effect re-runs → form syncs with fresh data
   * 
   * BEFORE FIX:
   * - Form showed stale data after save + refresh
   * 
   * AFTER FIX:
   * - updateUser() triggers this effect → instant form sync
   */
  useEffect(() => {
    if (user) {
      setProfile({
        username: user.username || '',
        email: user.email || '',
        phoneNumber: user.phoneNumber || user.phone_number || '',
        firstName: user.firstName || user.first_name || '',
        lastName: user.lastName || user.last_name || '',
      });
    }
  }, [user]);

  // ============================================================
  // Load Sessions (only when Sessions tab is active)
  // ============================================================
  useEffect(() => {
    if (activeTab === 'sessions') {
      const fetchSessions = async () => {
        try {
          setSessionsLoading(true);
          const res = await axiosInstance.get('/refresh-tokens/me');
          const data = res.data?.data ?? res.data;
          setSessions(Array.isArray(data) ? data : data.items || []);
        } catch (e) {
          console.error('Failed to load sessions:', e);
          setSessions([]);
        } finally {
          setSessionsLoading(false);
        }
      };
      fetchSessions();
    }
  }, [activeTab]);

  // ============================================================
  // Email Validation Helper
  // ============================================================
  const isValidEmail = (email) => {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  };

  // ============================================================
  // Save Profile (with Fallback Chain + All Fixes)
  // ============================================================
  /**
   * Saves user profile with 4 critical fixes:
   * 
   * ✅ FIX 1: Phone validation ignores empty/whitespace
   *    - Before: `profile.phoneNumber.length < 6` failed on empty string
   *    - After: Only validate if phone is provided
   * 
   * ✅ FIX 2: Send null instead of empty string for optional fields
   *    - Before: Empty string stored in DB
   *    - After: null is cleaner + matches API contract
   * 
   * ✅ FIX 3: Call updateUser() after success
   *    - Before: AuthContext didn't know → stale user data
   *    - After: user state synced → fresh data on refresh
   * 
   * ✅ FIX 4: Fallback Chain (me → profile → {id})
   *    - Before: Single endpoint → fails if not supported
   *    - After: Tries multiple → robust across backends
   */
  const handleSaveProfile = async (e) => {
    e.preventDefault();

    // ============================================================
    // ✅ FIX 1: Phone validation (only if provided)
    // ============================================================
    const trimmedPhone = (profile.phoneNumber || '').trim();
    const errors = {};

    if (!profile.email?.trim()) {
      errors.email = t('validation.required');
    } else if (!isValidEmail(profile.email)) {
      errors.email = t('validation.invalidEmail');
    }

    // Only validate phone if it's not empty
    if (trimmedPhone && trimmedPhone.length < 6) {
      errors.phoneNumber = t('validation.invalidPhone');
    }

    if (Object.keys(errors).length > 0) {
      setProfileErrors(errors);
      return;
    }
    setProfileErrors({});

    setSaving(true);
    try {
      // ============================================================
      // ✅ FIX 4: Fallback Chain (robust across backends)
      // ============================================================
      const userId = user?.id || user?._id;
      const endpoints = [
        '/users/me',                              // Best (IDOR-Proof)
        '/users/profile',                         // Alternative
        userId ? `/users/${userId}` : null,       // Legacy (Admin only)
      ].filter(Boolean);

      // ============================================================
      // ✅ FIX 2: Send null instead of empty string
      // ============================================================
      const payload = {
        email: profile.email?.trim(),
        phoneNumber: trimmedPhone || null,      // ← null instead of ""
        firstName: profile.firstName?.trim() || null,
        lastName: profile.lastName?.trim() || null,
      };

      let lastError;
      let success = false;
      let responseData = null;

      for (const endpoint of endpoints) {
        try {
          const res = await axiosInstance.put(endpoint, payload);
          responseData = res.data?.data ?? res.data;
          success = true;
          break;
        } catch (err) {
          const status = err.response?.status;
          // 404 → endpoint doesn't exist → try next
          if (status === 404) {
            lastError = err;
            continue;
          }
          // 403/400/500 → real error → stop
          throw err;
        }
      }

      if (!success) {
        throw lastError || new Error('All endpoints failed');
      }

      // ============================================================
      // ✅ FIX 3: Sync user state via updateUser()
      // ============================================================
      // CRITICAL: This is the fix for "stale data on refresh"
      if (updateUser) {
        updateUser({
          email: responseData?.email || profile.email,
          phoneNumber: responseData?.phoneNumber ?? trimmedPhone,
          firstName: responseData?.firstName || profile.firstName,
          lastName: responseData?.lastName || profile.lastName,
        });
      }

      setToast({ type: 'success', message: t('settings.profileSaved') });

      // Optional: Refresh JWT in background (updates tokens silently)
      if (refreshSession) {
        try {
          await refreshSession();
        } catch (e) {
          console.warn('Background token refresh failed:', e);
        }
      }
    } catch (err) {
      const errRes = handleApiError(err, 'settings.profileError');
      const message = extractErrorMessage(errRes, 'settings.profileError');
      setToast({ type: 'error', message });
    } finally {
      setSaving(false);
    }
  };

  // ============================================================
  // Change Password
  // ============================================================
  const handleChangePassword = async (e) => {
    e.preventDefault();

    // Validation
    if (!passwordData.currentPassword || !passwordData.newPassword) {
      setPasswordErrors({ general: t('validation.required') });
      return;
    }
    if (passwordData.newPassword.length < 8) {
      setPasswordErrors({ newPassword: t('validation.minLength') });
      return;
    }
    if (passwordData.newPassword !== passwordData.confirmPassword) {
      setPasswordErrors({ confirmPassword: t('validation.passwordMismatch') });
      return;
    }

    setPasswordErrors({});
    setSaving(true);
    try {
      await axiosInstance.post('/auth/change-password', {
        currentPassword: passwordData.currentPassword,
        newPassword: passwordData.newPassword,
      });
      setToast({ type: 'success', message: t('settings.passwordChanged') });
      setPasswordData({ currentPassword: '', newPassword: '', confirmPassword: '' });
    } catch (err) {
      const errRes = handleApiError(err, 'settings.passwordError');
      const message = extractErrorMessage(errRes, 'settings.passwordError');
      setToast({ type: 'error', message });
    } finally {
      setSaving(false);
    }
  };

  // ============================================================
  // Sessions Handlers
  // ============================================================
  const handleRevokeSession = async (tokenId) => {
    if (!tokenId) return;
    try {
      await axiosInstance.post('/refresh-tokens/revoke', { tokenId });
      setSessions(sessions.filter(s => (s.id || s.tokenId) !== tokenId));
      setToast({ type: 'success', message: t('settings.sessionRevoked') });
    } catch (err) {
      const errRes = handleApiError(err, 'settings.sessionError');
      const message = extractErrorMessage(errRes, 'settings.sessionError');
      setToast({ type: 'error', message });
    }
  };

  const handleRevokeAllSessions = async () => {
    if (!window.confirm(t('settings.confirmRevokeAll'))) return;
    try {
      await axiosInstance.post('/refresh-tokens/revoke-all');
      setSessions([]);
      setToast({ type: 'success', message: t('settings.allSessionsRevoked') });
    } catch (err) {
      const errRes = handleApiError(err, 'settings.sessionError');
      const message = extractErrorMessage(errRes, 'settings.sessionError');
      setToast({ type: 'error', message });
    }
  };

  // ============================================================
  // Preferences Handlers
  // ============================================================
  const handleLanguageChange = (lang) => {
    i18n.changeLanguage(lang);
    localStorage.setItem('language', lang);
  };

  const handleThemeChange = (theme) => {
    localStorage.setItem('theme', theme);
    document.documentElement.setAttribute('data-theme', theme);
  };

  // ============================================================
  // Spinner Component (for Save buttons)
  // ============================================================
  const Spinner = () => (
    <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
    </svg>
  );

  // ============================================================
  // Eye Icons (for Show/Hide Password)
  // ============================================================
  const EyeIcon = () => (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
    </svg>
  );

  const EyeOffIcon = () => (
    <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
    </svg>
  );

  // ============================================================
  // Tabs Configuration
  // ============================================================
  const tabs = [
    { id: 'profile', label: t('settings.tabs.profile') || 'Profile' },
    { id: 'security', label: t('settings.tabs.security') || 'Security' },
    { id: 'preferences', label: t('settings.tabs.preferences') || 'Preferences' },
    { id: 'sessions', label: t('settings.tabs.sessions') || 'Sessions' },
  ];

  // ============================================================
  // Render
  // ============================================================
  return (
    <div className="space-y-6 text-[var(--text-main)] p-6 bg-[var(--bg-primary)] min-h-screen">
      {toast && <Toast {...toast} onClose={() => setToast(null)} />}

      {/* Header */}
      <div className="flex justify-between items-center bg-[var(--bg-secondary)] border border-[var(--border-color)] p-6 rounded-xl">
        <div>
          <h1 className="text-2xl font-bold">{t('settings.title') || 'Settings'}</h1>
          <p className="text-sm text-[var(--text-muted)] mt-1">
            {t('settings.subtitle') || 'Manage your account settings and preferences.'}
          </p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b border-[var(--border-color)] overflow-x-auto">
        {tabs.map(tab => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`px-4 py-2 text-sm font-medium transition-colors whitespace-nowrap ${
              activeTab === tab.id
                ? 'text-[var(--accent-color)] border-b-2 border-[var(--accent-color)]'
                : 'text-[var(--text-muted)] hover:text-[var(--text-main)]'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      <div className="bg-[var(--bg-secondary)] border border-[var(--border-color)] rounded-xl p-6">

        {/* ==================== Profile Tab ==================== */}
        {activeTab === 'profile' && (
          <form onSubmit={handleSaveProfile} className="space-y-4 max-w-lg">
            <h2 className="text-lg font-bold mb-4">
              {t('settings.profile.title') || 'Profile Information'}
            </h2>

            {/* Username (Disabled - immutable) */}
            <div>
              <label className="block text-xs font-medium mb-1">
                {t('settings.profile.username') || 'Username'}
              </label>
              <input
                type="text"
                value={profile.username}
                disabled
                className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm text-[var(--text-muted)] cursor-not-allowed"
              />
              <p className="text-xs text-[var(--text-muted)] mt-1">
                {t('settings.profile.usernameHint') || 'Username cannot be changed.'}
              </p>
            </div>

            {/* First Name + Last Name */}
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium mb-1">
                  {t('settings.profile.firstName') || 'First Name'}
                </label>
                <input
                  type="text"
                  value={profile.firstName}
                  onChange={(e) => setProfile({ ...profile, firstName: e.target.value })}
                  className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm"
                />
              </div>
              <div>
                <label className="block text-xs font-medium mb-1">
                  {t('settings.profile.lastName') || 'Last Name'}
                </label>
                <input
                  type="text"
                  value={profile.lastName}
                  onChange={(e) => setProfile({ ...profile, lastName: e.target.value })}
                  className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm"
                />
              </div>
            </div>

            {/* Email (with validation) */}
            <div>
              <label className="block text-xs font-medium mb-1">
                {t('settings.profile.email') || 'Email'}
              </label>
              <input
                type="email"
                value={profile.email}
                onChange={(e) => {
                  setProfile({ ...profile, email: e.target.value });
                  if (profileErrors.email) setProfileErrors({ ...profileErrors, email: null });
                }}
                className={`w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border text-sm ${
                  profileErrors.email ? 'border-red-500' : 'border-[var(--border-color)]'
                }`}
              />
              {profileErrors.email && (
                <p className="text-xs text-red-400 mt-1">{profileErrors.email}</p>
              )}
            </div>

            {/* Phone (with validation - only if provided) */}
            <div>
              <label className="block text-xs font-medium mb-1">
                {t('settings.profile.phone') || 'Phone Number'}
              </label>
              <input
                type="text"
                value={profile.phoneNumber}
                onChange={(e) => {
                  setProfile({ ...profile, phoneNumber: e.target.value });
                  if (profileErrors.phoneNumber) setProfileErrors({ ...profileErrors, phoneNumber: null });
                }}
                className={`w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border text-sm ${
                  profileErrors.phoneNumber ? 'border-red-500' : 'border-[var(--border-color)]'
                }`}
              />
              {profileErrors.phoneNumber && (
                <p className="text-xs text-red-400 mt-1">{profileErrors.phoneNumber}</p>
              )}
            </div>

            {/* Save Button with Spinner */}
            <div className="pt-4">
              <button
                type="submit"
                disabled={saving}
                className="px-5 py-2 rounded-xl bg-[var(--accent-color)] text-white text-sm font-medium hover:opacity-90 disabled:opacity-50 flex items-center gap-2"
              >
                {saving ? (
                  <>
                    <Spinner />
                    {t('common.saving')}
                  </>
                ) : (
                  t('common.save')
                )}
              </button>
            </div>
          </form>
        )}

        {/* ==================== Security Tab ==================== */}
        {activeTab === 'security' && (
          <form onSubmit={handleChangePassword} className="space-y-4 max-w-lg">
            <h2 className="text-lg font-bold mb-4">
              {t('settings.security.title') || 'Change Password'}
            </h2>

            {passwordErrors.general && (
              <p className="text-xs text-red-400">{passwordErrors.general}</p>
            )}

            {/* Current Password (with Show/Hide) */}
            <div>
              <label className="block text-xs font-medium mb-1">
                {t('settings.security.currentPassword') || 'Current Password'}
              </label>
              <div className="relative">
                <input
                  type={showPasswords.current ? 'text' : 'password'}
                  value={passwordData.currentPassword}
                  onChange={(e) => setPasswordData({ ...passwordData, currentPassword: e.target.value })}
                  className="w-full px-3 py-2 pr-10 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm"
                />
                <button
                  type="button"
                  onClick={() => togglePasswordVisibility('current')}
                  className="absolute inset-y-0 right-0 flex items-center pr-3 text-[var(--text-muted)] hover:text-[var(--text-main)]"
                >
                  {showPasswords.current ? <EyeOffIcon /> : <EyeIcon />}
                </button>
              </div>
            </div>

            {/* New Password (with Show/Hide) */}
            <div>
              <label className="block text-xs font-medium mb-1">
                {t('settings.security.newPassword') || 'New Password'}
              </label>
              <div className="relative">
                <input
                  type={showPasswords.new ? 'text' : 'password'}
                  value={passwordData.newPassword}
                  onChange={(e) => setPasswordData({ ...passwordData, newPassword: e.target.value })}
                  className="w-full px-3 py-2 pr-10 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm"
                />
                <button
                  type="button"
                  onClick={() => togglePasswordVisibility('new')}
                  className="absolute inset-y-0 right-0 flex items-center pr-3 text-[var(--text-muted)] hover:text-[var(--text-main)]"
                >
                  {showPasswords.new ? <EyeOffIcon /> : <EyeIcon />}
                </button>
              </div>
              {passwordErrors.newPassword && (
                <p className="text-xs text-red-400 mt-1">{passwordErrors.newPassword}</p>
              )}
            </div>

            {/* Confirm Password (with Show/Hide) */}
            <div>
              <label className="block text-xs font-medium mb-1">
                {t('settings.security.confirmPassword') || 'Confirm New Password'}
              </label>
              <div className="relative">
                <input
                  type={showPasswords.confirm ? 'text' : 'password'}
                  value={passwordData.confirmPassword}
                  onChange={(e) => setPasswordData({ ...passwordData, confirmPassword: e.target.value })}
                  className="w-full px-3 py-2 pr-10 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm"
                />
                <button
                  type="button"
                  onClick={() => togglePasswordVisibility('confirm')}
                  className="absolute inset-y-0 right-0 flex items-center pr-3 text-[var(--text-muted)] hover:text-[var(--text-main)]"
                >
                  {showPasswords.confirm ? <EyeOffIcon /> : <EyeIcon />}
                </button>
              </div>
              {passwordErrors.confirmPassword && (
                <p className="text-xs text-red-400 mt-1">{passwordErrors.confirmPassword}</p>
              )}
            </div>

            {/* Submit */}
            <div className="pt-4">
              <button
                type="submit"
                disabled={saving}
                className="px-5 py-2 rounded-xl bg-[var(--accent-color)] text-white text-sm font-medium hover:opacity-90 disabled:opacity-50 flex items-center gap-2"
              >
                {saving ? (
                  <>
                    <Spinner />
                    {t('common.saving')}
                  </>
                ) : (
                  t('settings.security.changePassword') || 'Change Password'
                )}
              </button>
            </div>
          </form>
        )}

        {/* ==================== Preferences Tab ==================== */}
        {activeTab === 'preferences' && (
          <div className="space-y-6 max-w-lg">
            <h2 className="text-lg font-bold mb-4">
              {t('settings.preferences.title') || 'Preferences'}
            </h2>

            <div>
              <label className="block text-xs font-medium mb-1">
                {t('settings.preferences.language') || 'Language'}
              </label>
              <select
                value={i18n.language || 'en'}
                onChange={(e) => handleLanguageChange(e.target.value)}
                className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm"
              >
                <option value="en">English</option>
                <option value="ar">العربية</option>
              </select>
            </div>

            <div>
              <label className="block text-xs font-medium mb-1">
                {t('settings.preferences.theme') || 'Theme'}
              </label>
              <select
                value={localStorage.getItem('theme') || 'olive'}
                onChange={(e) => handleThemeChange(e.target.value)}
                className="w-full px-3 py-2 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-sm"
              >
                <option value="olive">Olive</option>
                <option value="dark">Dark</option>
                <option value="blue">Blue</option>
                <option value="green">Green</option>
                <option value="orange">Orange</option>
                <option value="purple">Purple</option>
                <option value="light">Light</option>
              </select>
            </div>
          </div>
        )}

        {/* ==================== Sessions Tab ==================== */}
        {activeTab === 'sessions' && (
          <div className="space-y-4">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-lg font-bold">
                {t('settings.sessions.title') || 'Active Sessions'}
              </h2>
              <button
                onClick={handleRevokeAllSessions}
                disabled={sessions.length === 0}
                className="px-4 py-2 rounded-xl bg-[var(--accent-color)] text-white text-xs font-medium hover:opacity-90 disabled:opacity-50"
              >
                {t('settings.sessions.revokeAll') || 'Revoke All'}
              </button>
            </div>

            {sessionsLoading ? (
              <p className="text-center text-[var(--text-muted)] py-8">
                {t('common.loading')}
              </p>
            ) : sessions.length === 0 ? (
              <p className="text-center text-[var(--text-muted)] py-8">
                {t('settings.sessions.noSessions') || 'No active sessions.'}
              </p>
            ) : (
              <div className="space-y-2">
                {sessions.map(session => {
                  const sessionId = session.id || session.tokenId;
                  
                  // Detect current session (best-effort)
                  const isCurrent = 
                    session.isCurrent === true || 
                    session.current === true ||
                    (() => {
                      try {
                        return session.userAgent === navigator.userAgent;
                      } catch { return false; }
                    })();

                  return (
                    <div
                      key={sessionId}
                      className={`flex items-center justify-between p-3 rounded-xl border ${
                        isCurrent
                          ? 'bg-[var(--bg-accent)] border-[var(--accent-color)]/30'
                          : 'bg-[var(--bg-primary)] border-[var(--border-color)]'
                      }`}
                    >
                      <div className="text-sm min-w-0">
                        <div className="flex items-center gap-2">
                          <p className="font-medium truncate">{session.userAgent || 'Unknown Device'}</p>
                          {isCurrent && (
                            <span className="text-[10px] px-2 py-0.5 rounded-full bg-[var(--accent-color)] text-white font-medium">
                              Current
                            </span>
                          )}
                        </div>
                        <p className="text-xs text-[var(--text-muted)]">
                          IP: {session.ipAddress || '-'} |{' '}
                          {session.createdAt ? new Date(session.createdAt).toLocaleString() : '-'}
                        </p>
                      </div>
                      <button
                        onClick={() => handleRevokeSession(sessionId)}
                        disabled={isCurrent}
                        className="text-xs px-3 py-1.5 rounded-lg text-[var(--text-muted)] hover:text-[var(--text-main)] border border-[var(--border-color)] disabled:opacity-30 disabled:cursor-not-allowed whitespace-nowrap"
                      >
                        {t('settings.sessions.revoke') || 'Revoke'}
                      </button>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default Settings;