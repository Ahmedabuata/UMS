import { createContext, useCallback, useContext, useMemo, useState } from 'react'
import { authApi, clearSession, getStoredUser, getToken, setSession } from '../api/client'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => getStoredUser())
  const [token, setToken] = useState(() => getToken())
  const [initializing, setInitializing] = useState(false)
  const [mustChangePassword, setMustChangePassword] = useState(() => localStorage.getItem('ums_must_change') === 'true')
  const [permissions, setPermissions] = useState(() => {
    try {
      return JSON.parse(localStorage.getItem('permissions') || '[]')
    } catch {
      return []
    }
  })

  const login = useCallback(async (username, password) => {
    const auth = await authApi.login(username, password)
    setSession(auth)
    localStorage.setItem('permissions', JSON.stringify(auth.permissions ?? []))
    localStorage.setItem('ums_must_change', auth.mustChangePassword ? 'true' : 'false')
    setToken(auth.token)
    setUser(auth.user)
    setMustChangePassword(Boolean(auth.mustChangePassword))
    setPermissions(auth.permissions ?? [])
    return auth
  }, [])

  const changePassword = useCallback(async (oldPassword, newPassword) => {
    if (!user?.id) throw new Error('No active user')
    const ok = await authApi.changePassword(user.id, oldPassword, newPassword)
    if (ok) {
      localStorage.setItem('ums_must_change', 'false')
      setMustChangePassword(false)
      await login(user.username, newPassword)
    }
    return ok
  }, [user, login])

  const logout = useCallback(() => {
    clearSession()
    localStorage.removeItem('permissions')
    localStorage.removeItem('ums_must_change')
    setToken(null)
    setUser(null)
    setMustChangePassword(false)
    setPermissions([])
  }, [])

  const value = useMemo(() => ({
    user,
    token,
    permissions,
    mustChangePassword,
    isAuthenticated: Boolean(token),
    login,
    logout,
    changePassword,
    initializing
  }), [user, token, permissions, mustChangePassword, login, logout, changePassword, initializing])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}