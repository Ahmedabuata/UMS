import { useCallback, useEffect, useMemo, useState } from 'react'
import { employeesApi, rolesApi, usersApi } from '../api/client'
import { Field, inputCls } from './ui'

function generateTempPassword(len = 12) {
  const upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ'
  const lower = 'abcdefghijkmnopqrstuvwxyz'
  const digits = '23456789'
  const symbols = '@#$%'
  const all = upper + lower + digits + symbols
  const chars = [
    upper[Math.floor(Math.random() * upper.length)],
    lower[Math.floor(Math.random() * lower.length)],
    digits[Math.floor(Math.random() * digits.length)],
    symbols[Math.floor(Math.random() * symbols.length)],
  ]
  while (chars.length < len) {
    chars.push(all[Math.floor(Math.random() * all.length)])
  }
  for (let i = chars.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1))
    ;[chars[i], chars[j]] = [chars[j], chars[i]]
  }
  return chars.join('')
}

export default function AddUserAccountModal({ onClose, onCreated }) {
  const [options, setOptions] = useState([])
  const [roles, setRoles] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const [employeeId, setEmployeeId] = useState('')
  const [roleId, setRoleId] = useState('')
  const [username, setUsername] = useState('')
  const [tempPassword, setTempPassword] = useState('')
  const [showPwd, setShowPwd] = useState(false)

  useEffect(() => {
    setTempPassword(generateTempPassword())
    ;(async () => {
      try {
        const [emps, rs] = await Promise.all([employeesApi.withoutUserAccount(), rolesApi.list()])
        setOptions(emps?? [])
        setRoles((rs?? []).filter((r) => r.isActive!== false))
      } catch (err) {
        setError(err.message || 'Failed to load employees')
      } finally {
        setLoading(false)
      }
    })()
  }, [])

  const selected = useMemo(() => options.find((o) => o.id === employeeId), [options, employeeId])

  const handleSelectEmployee = (e) => {
    const id = e.target.value
    setEmployeeId(id)
    const emp = options.find((o) => o.id === id)
    if (emp?.email) setUsername(emp.email)
  }

  const regenerate = () => setTempPassword(generateTempPassword())

  const handleSave = useCallback(async () => {
    setError('')
    if (!employeeId ||!roleId ||!username.trim()) {
      setError('Employee, role and username are required.')
      return
    }
    setSaving(true)
    try {
      const created = await usersApi.createForEmployee(employeeId, {
        username: username.trim(),
        roleId,
        tempPassword,
      })
      onCreated && onCreated(created)
      onClose()
    } catch (err) {
      setError(err.message || 'Failed to create user account')
    } finally {
      setSaving(false)
    }
  }, [employeeId, roleId, username, tempPassword, onCreated, onClose])

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-lg bg-white rounded-xl shadow-xl max-h- overflow-y-auto p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">Create User Account for Existing Employee</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl leading-none">×</button>
        </div>
        {loading? (
          <p className="text-gray-500 py-4">Loading employees...</p>
        ) : options.length === 0? (
          <div className="rounded-lg bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 text-sm">
            All employees have accounts. Go to HR Management &gt; + Add New Employee.
          </div>
        ) : (
          <div className="space-y-4">
            {error && <div className="rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>}
            <Field label="Employee *">
              <select value={employeeId} onChange={handleSelectEmployee} className={inputCls}>
                <option value="">Select employee</option>
                {options.map((o) => (
                  <option key={o.id} value={o.id}>{o.employeeNumber} - {o.fullName}</option>
                ))}
              </select>
            </Field>
            {selected && (
              <Field label="Email (read-only)">
                <input value={selected.email || ''} readOnly className={`${inputCls} bg-gray-50 text-gray-700`} />
              </Field>
            )}
            <Field label="Role *">
              <select value={roleId} onChange={(e) => setRoleId(e.target.value)} className={inputCls}>
                <option value="">Select role</option>
                {roles.map((r) => (
                  <option key={r.id} value={r.id}>{r.name || r.roleName || r.displayName}</option>
                ))}
              </select>
            </Field>
            <Field label="Username *">
              <input value={username} onChange={(e) => setUsername(e.target.value)} placeholder="username" className={inputCls} />
            </Field>
            <Field label="Temp Password *">
              <div className="flex items-center gap-2">
                <input value={tempPassword} type={showPwd? 'text' : 'password'} readOnly className={`${inputCls} bg-gray-50 text-gray-700 font-mono`} />
                <button type="button" onClick={() => setShowPwd((s) =>!s)} className="rounded-lg border border-gray-300 px-3 py-2 text-sm"> {showPwd? 'Hide' : 'Show'} </button>
                <button type="button" onClick={regenerate} className="rounded-lg border border-gray-300 px-3 py-2 text-sm">Regenerate</button>
              </div>
            </Field>
            <div className="flex justify-end gap-3 pt-2">
              <button onClick={onClose} className="rounded-lg px-4 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-100">Cancel</button>
              <button onClick={handleSave} disabled={saving} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                {saving? 'Creating…' : 'Create Account'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}