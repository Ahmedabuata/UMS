import { useEffect, useState } from 'react'
import { policiesApi } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { Field, inputCls } from '../components/ui'

export default function SecurityPolicies() {
  const { permissions } = useAuth()
  const canWrite = permissions.includes('USER_WRITE')

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  const [base, setBase] = useState(null)
  const [form, setForm] = useState({
    minLength: 8,
    requireUppercase: true,
    requireLowercase: true,
    requireDigit: true,
    requireSpecial: true,
    expirationDays: 90,
    maxFailedAttempts: 5,
    lockoutMinutes: 30,
    twoFactorEnabled: false,
  })

  useEffect(() => {
    ;(async () => {
      setLoading(true)
      setError('')
      try {
        const data = await policiesApi.get()
        setBase(data)
        const pp = data?.passwordPolicy ?? {}
        setForm({
          minLength: pp.minLength ?? 8,
          requireUppercase: pp.requireUppercase ?? true,
          requireLowercase: pp.requireLowercase ?? true,
          requireDigit: pp.requireDigit ?? true,
          requireSpecial: pp.requireSpecial ?? true,
          expirationDays: pp.expirationDays ?? 90,
          maxFailedAttempts: pp.maxFailedAttempts ?? 5,
          lockoutMinutes: pp.lockoutMinutes ?? 30,
          twoFactorEnabled: data?.twoFactor?.enabled ?? false,
        })
      } catch (err) {
        setError(err.message || 'Failed to load security policies')
      } finally {
        setLoading(false)
      }
    })()
  }, [])

  const set = (key, value) => setForm((f) => ({ ...f, [key]: value }))

  const handleSave = async (e) => {
    e.preventDefault()
    if (!canWrite) return
    setSaving(true)
    setError('')
    setNotice('')
    try {
      const payload = {
        ...base,
        passwordPolicy: {
          ...(base?.passwordPolicy ?? {}),
          minLength: Number(form.minLength),
          requireUppercase: form.requireUppercase,
          requireLowercase: form.requireLowercase,
          requireDigit: form.requireDigit,
          requireSpecial: form.requireSpecial,
          expirationDays: Number(form.expirationDays),
          maxFailedAttempts: Number(form.maxFailedAttempts),
          lockoutMinutes: Number(form.lockoutMinutes),
        },
        twoFactor: {
          ...(base?.twoFactor ?? {}),
          enabled: form.twoFactorEnabled,
        },
      }
      await policiesApi.update(payload)
      setNotice('Security policies saved. Changes apply to new logins immediately.')
      window.setTimeout(() => setNotice(''), 4000)
    } catch (err) {
      setError(err.message || 'Failed to save security policies')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p className="text-gray-500">Loading security policies...</p>

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Security Policies</h1>
        <p className="text-gray-500 text-sm mt-1">Configure password policy, lockout rules, and two-factor authentication.</p>
      </div>

      {!canWrite && (
        <div className="mb-4 rounded-lg bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 text-sm">
          You have read-only access. Changes to security policies require the USER_WRITE permission.
        </div>
      )}
      {notice && <div className="mb-4 rounded-lg bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm">{notice}</div>}
      {error && <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>}

      <form onSubmit={handleSave} className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-8 max-w-2xl">
        <section>
          <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wider mb-4">Password Policy</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Field label="Minimum length">
              <input
                type="number"
                min={4}
                value={form.minLength}
                onChange={(e) => set('minLength', e.target.value)}
                disabled={!canWrite}
                className={inputCls}
              />
            </Field>
            <Field label="Expiry (days)">
              <input
                type="number"
                min={0}
                value={form.expirationDays}
                onChange={(e) => set('expirationDays', e.target.value)}
                disabled={!canWrite}
                className={inputCls}
              />
            </Field>
          </div>
          <div className="mt-5 grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Toggle
              label="Require uppercase"
              checked={form.requireUppercase}
              onChange={(v) => set('requireUppercase', v)}
              disabled={!canWrite}
            />
            <Toggle
              label="Require lowercase"
              checked={form.requireLowercase}
              onChange={(v) => set('requireLowercase', v)}
              disabled={!canWrite}
            />
            <Toggle
              label="Require digit"
              checked={form.requireDigit}
              onChange={(v) => set('requireDigit', v)}
              disabled={!canWrite}
            />
            <Toggle
              label="Require special character"
              checked={form.requireSpecial}
              onChange={(v) => set('requireSpecial', v)}
              disabled={!canWrite}
            />
          </div>
        </section>

        <section>
          <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wider mb-4">Account Lockout</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Field label="Max failed login attempts">
              <input
                type="number"
                min={1}
                value={form.maxFailedAttempts}
                onChange={(e) => set('maxFailedAttempts', e.target.value)}
                disabled={!canWrite}
                className={inputCls}
              />
            </Field>
            <Field label="Lockout duration (minutes)">
              <input
                type="number"
                min={1}
                value={form.lockoutMinutes}
                onChange={(e) => set('lockoutMinutes', e.target.value)}
                disabled={!canWrite}
                className={inputCls}
              />
            </Field>
          </div>
        </section>

        <section>
          <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wider mb-4">Two-Factor Authentication</h2>
          <div className="space-y-3">
            <Toggle
              label="Enable 2FA for password users"
              checked={form.twoFactorEnabled}
              onChange={(v) => set('twoFactorEnabled', v)}
              disabled={!canWrite}
            />
            <p className="text-xs text-gray-500">
              Requires users with a password to complete a one-time code on sign-in. Users with an active 2FA setup are unaffected.
            </p>
            <div className="rounded-lg bg-gray-50 border border-gray-200 px-4 py-3 text-sm text-gray-500">
              Require 2FA for admin: not configurable yet — the existing policies API has no field for this setting.
            </div>
          </div>
        </section>

        <div className="flex justify-end border-t border-gray-200 pt-5">
          <button
            type="submit"
            disabled={!canWrite || saving}
            className="rounded-lg bg-indigo-600 px-5 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50 transition"
          >
            {saving ? 'Saving...' : 'Save policies'}
          </button>
        </div>
      </form>
    </div>
  )
}

function Toggle({ label, checked, onChange, disabled }) {
  return (
    <button
      type="button"
      onClick={() => onChange(!checked)}
      disabled={disabled}
      className="flex items-center justify-between gap-3 rounded-lg border border-gray-200 px-4 py-3 text-left disabled:cursor-not-allowed disabled:opacity-60"
    >
      <span className="text-sm font-medium text-gray-800">{label}</span>
      <span
        className={`relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition ${
          checked ? 'bg-indigo-600' : 'bg-gray-300'
        }`}
      >
        <span
          className={`inline-block h-4 w-4 transform rounded-full bg-white transition ${
            checked ? 'translate-x-6' : 'translate-x-1'
          }`}
        />
      </span>
    </button>
  )
}