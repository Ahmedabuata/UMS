import React, { createContext, useContext, useState, useEffect } from 'react';
import axiosInstance from '../api/axiosInstance';

const AuthContext = createContext(null);

// ============================================================
// JWT Helper: Safe Decode (UTF-8 + Base64url)
// ============================================================
/**
 * Safely decodes a JWT payload, supporting UTF-8 (including Arabic).
 * 
 * WHY WE NEED THIS:
 * - Native atob() fails with UTF-8 chars (Arabic, emojis, etc.)
 * - JWT uses base64url (not base64), so we need to replace chars first
 * 
 * @param {string} token - JWT token
 * @returns {object|null} - Decoded payload, or null if invalid
 */
const parseJwtPayload = (token) => {
  if (!token || typeof token !== 'string') return null;

  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const base64Url = parts[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');

    // Safe UTF-8 decode (handles Arabic + emojis correctly)
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    return JSON.parse(jsonPayload);
  } catch (e) {
    console.error('Failed to parse JWT:', e);
    return null;
  }
};

// ============================================================
// JWT Helper: Check Expiration
// ============================================================
/**
 * Checks if a JWT token has expired.
 * 
 * WHY WE NEED THIS:
 * - Prevents "UI Flash" on page refresh (showing Dashboard briefly then logout)
 * - Prevents API 401 errors when tokens are stale
 * 
 * @param {string} token - JWT token
 * @returns {boolean} - true if expired
 */
const isTokenExpired = (token) => {
  const payload = parseJwtPayload(token);
  if (!payload || !payload.exp) return true;

  const currentTime = Date.now() / 1000;
  return payload.exp < currentTime;
};

// ============================================================
// JWT Helper: Extract User from Token
// ============================================================
/**
 * Extracts user object from JWT payload with roles + permissions.
 * 
 * IMPORTANT:
 * - Merges with baseUser to preserve extra fields (firstName, lastName)
 * - JWT only contains: sub, email, username, roles, permissions
 * - JWT does NOT contain: firstName, lastName, phoneNumber, profile
 * - So we MUST merge with existing user data
 * 
 * @param {string} token - JWT token
 * @param {object} baseUser - Existing user object (preserves firstName, lastName, etc.)
 * @returns {object} - Enriched user object
 */
const extractUserFromToken = (token, baseUser = {}) => {
  const payload = parseJwtPayload(token);
  if (!payload) return baseUser;

  return {
    ...baseUser,
    id: baseUser.id || payload.sub || payload.userId || payload.uid || payload.nameid,
    username: baseUser.username || payload.username || payload.unique_name,
    email: baseUser.email || payload.email,
    roles: baseUser.roles?.length
      ? baseUser.roles
      : (payload.roles ||
         payload.role ||
         payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
         []),
    permissions: baseUser.permissions?.length
      ? baseUser.permissions
      : (payload.permissions || payload.perm || []),
  };
};

// ============================================================
// AuthProvider Component
// ============================================================
export const AuthProvider = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  // ============================================================
  // Boot: Restore session from localStorage
  // ============================================================
  /**
   * Restores auth state on page refresh.
   * 
   * FLOW:
   * 1. Read token from localStorage
   * 2. Check expiration (prevents UI flash)
   * 3. Re-parse JWT for fresh roles/permissions
   * 4. Merge with saved user (preserves firstName, lastName)
   */
  useEffect(() => {
    const bootstrap = () => {
      const token = localStorage.getItem('accessToken');

      if (!token) {
        setLoading(false);
        return;
      }

      if (isTokenExpired(token)) {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        setLoading(false);
        return;
      }

      try {
        axiosInstance.defaults.headers.common['Authorization'] = `Bearer ${token}`;

        const savedUser = localStorage.getItem('user');
        const baseUser = savedUser ? JSON.parse(savedUser) : {};

        // Merge JWT data with saved user (preserves firstName, lastName)
        const freshUser = extractUserFromToken(token, baseUser);

        setUser(freshUser);
        setIsAuthenticated(true);

        // Sync localStorage with fresh user data
        localStorage.setItem('user', JSON.stringify(freshUser));
      } catch (e) {
        console.error('Failed to restore session:', e);
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
      } finally {
        setLoading(false);
      }
    };

    bootstrap();
  }, []);

  // ============================================================
  // login() - Authenticate user
  // ============================================================
  const login = async (emailOrUsername, password) => {
    setLoading(true);
    try {
      if (typeof emailOrUsername === 'object') {
        password = emailOrUsername.password;
        emailOrUsername = emailOrUsername.email || emailOrUsername.usernameOrEmail;
      }

      const { data } = await axiosInstance.post('/auth/login', {
        EmailOrUsername: emailOrUsername,
        password: password,
      });
      const res = data.data || data;

      // Extract user from JWT + merge with API response (includes firstName, lastName)
      let finalUser = extractUserFromToken(res.accessToken, res.user || {});

      localStorage.setItem('accessToken', res.accessToken);
      localStorage.setItem('refreshToken', res.refreshToken);
      localStorage.setItem('user', JSON.stringify(finalUser));

      axiosInstance.defaults.headers.common['Authorization'] = `Bearer ${res.accessToken}`;

      setIsAuthenticated(true);
      setUser(finalUser);

      return { success: true, user: finalUser };
    } catch (err) {
      console.error('Login error', err.response?.data);
      return {
        success: false,
        message: err.response?.data?.message || 'Login failed',
      };
    } finally {
      setLoading(false);
    }
  };

  // ============================================================
  // logout() - Clear session + revoke token
  // ============================================================
  const logout = async () => {
    try {
      const rt = localStorage.getItem('refreshToken');
      if (rt) {
        await axiosInstance.post('/auth/revoke-token', { refreshToken: rt });
      }
    } catch (e) {
      console.warn('Token revoke failed (non-blocking):', e?.message);
    }

    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');

    delete axiosInstance.defaults.headers.common['Authorization'];
    setIsAuthenticated(false);
    setUser(null);

    window.location.href = '/login';
  };

  // ============================================================
  // refreshSession() - Renew access token
  // ============================================================
  /**
   * Refreshes the access token using refresh token.
   * 
   * ⚠️ CRITICAL FIX (v2):
   * - Using FUNCTIONAL setUser(prevUser => ...) instead of closure `user`
   * 
   * WHY THIS FIX MATTERS (RACE CONDITION):
   * - BEFORE: `const currentUser = user || {}` used STALE closure value
   *   Scenario:
   *   1. Settings.jsx calls updateUser({ firstName: "Ata" })  → user state = "Ata"
   *   2. Settings.jsx calls await refreshSession()
   *   3. refreshSession uses stale `user` closure = "Almahallawi" (OLD!)
   *   4. setUser(extractUserFromToken(token, "Almahallawi"))
   *   5. Result: "Ata" OVERWRITTEN by "Almahallawi" ❌
   * 
   * - AFTER: `setUser(prevUser => ...)` gets LATEST user state
   *   1. updateUser sets state to "Ata"
   *   2. refreshSession functional setState reads prevUser = "Ata" ✅
   *   3. extractUserFromToken preserves "Ata" + updates tokens/roles
   *   4. Result: "Ata" preserved ✅
   * 
   * @returns {boolean} - true if refresh succeeded
   */
  const refreshSession = async () => {
    const rt = localStorage.getItem('refreshToken');
    if (!rt) return false;

    try {
      const { data } = await axiosInstance.post('/auth/refresh-token', {
        refreshToken: rt,
      });
      const res = data.data || data;

      if (res.accessToken) {
        // Update tokens in localStorage + axios header
        localStorage.setItem('accessToken', res.accessToken);
        localStorage.setItem('refreshToken', res.refreshToken || rt);
        axiosInstance.defaults.headers.common['Authorization'] = `Bearer ${res.accessToken}`;

        // ============================================================
        // ✅ FIX: Use FUNCTIONAL setState to access LATEST user state
        // ============================================================
        // WHY: Avoids stale closure — uses latest user (with updateUser data)
        // 
        // PREVIOUS BUG:
        //   const currentUser = user || {};  // stale → old firstName
        //   setUser(extractUserFromToken(token, currentUser));
        //   → Overwrites updateUser changes ❌
        // 
        // FIX:
        //   setUser(prevUser => {
        //     const freshUser = extractUserFromToken(token, prevUser);
        //     return freshUser;  // Preserves updateUser data ✅
        //   });
        // ============================================================
        setUser(prevUser => {
          const freshUser = extractUserFromToken(res.accessToken, prevUser || {});
          try {
            localStorage.setItem('user', JSON.stringify(freshUser));
          } catch (e) {
            console.error('Failed to update localStorage user:', e);
          }
          return freshUser;
        });

        return true;
      }
      return false;
    } catch (e) {
      console.error('Session refresh failed:', e);
      return false;
    }
  };

  // ============================================================
  // updateUser() - Sync user data after profile update
  // ============================================================
  /**
   * Updates user state + localStorage after profile changes.
   * 
   * WHY WE NEED THIS:
   * - Settings page saves profile → Backend updated
   * - Before: AuthContext didn't know → stale user state
   * - After: Called from Settings → instant UI sync
   * 
   * IMPORTANT:
   * - Merges with existing user (partial update)
   * - Preserves roles, permissions, id (not overwritten)
   * - Uses functional setState to avoid stale closure
   * 
   * @param {object} updatedData - Partial user data to merge
   */
  const updateUser = (updatedData) => {
    if (!updatedData || typeof updatedData !== 'object') return;

    setUser(prevUser => {
      const merged = { ...(prevUser || {}), ...updatedData };
      try {
        localStorage.setItem('user', JSON.stringify(merged));
      } catch (e) {
        console.error('Failed to update localStorage user:', e);
      }
      return merged;
    });
  };

  // ============================================================
  // clearSession() - Pure local clear (no API call)
  // ============================================================
  const clearSession = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    delete axiosInstance.defaults.headers.common['Authorization'];
    setIsAuthenticated(false);
    setUser(null);
  };

  // ============================================================
  // revokeSession() - Logout with backend revoke
  // ============================================================
  const revokeSession = async () => {
    try {
      const rt = localStorage.getItem('refreshToken');
      if (rt) {
        await axiosInstance.post('/auth/revoke-token', { refreshToken: rt });
      }
    } catch (e) {
      console.warn('Revoke failed (proceeding with local clear):', e?.message);
    } finally {
      clearSession();
      window.location.href = '/login';
    }
  };

  // ============================================================
  // Context Value
  // ============================================================
  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        user,
        loading,
        login,
        logout,
        refreshSession,
        updateUser,
        revokeSession,
        clearSession,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

// ============================================================
// useAuth Hook
// ============================================================
export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
};

export default AuthContext;