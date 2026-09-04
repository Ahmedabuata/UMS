import { useCallback, useEffect, useState } from 'react'
import { rolesApi, usersApi } from '../api/client'
import AddUserAccountModal from '../components/AddUserAccountModal'
import { Badge, Field, inputCls } from '../components/ui'
import { useAuth } from '../context/AuthContext'

const PAGE_SIZE = 20

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
  while (chars.length < len) chars.push(all[Math.floor(Math.random() * all.length)])
  for (let i = chars.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1))
    ;[chars[i], chars[j]] = [chars[j], chars[i]]
  }
  return chars.join('')
}

export default function Users() {
  const { user } = useAuth()
  const [users, setUsers] = useState([])
  const [total, setTotal] = useState(0)
  const [totalPages, setTotalPages] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState('')

  const [showAdd, setShowAdd] = useState(false)
  const [viewUser, setViewUser] = useState(null)
  const [editUser, setEditUser] = useState(null)
  const [roles, setRoles] = useState([])
  const [editSaving, setEditSaving] = useState(false)
  const [resetUser, setResetUser] = useState(null)
  const [resetPwd, setResetPwd] = useState('')
  const [resetSaving, setResetSaving] = useState(false)

  const showNotice = (msg) => {
    setNotice(msg)
    window.setTimeout(() => setNotice(''), 3500)
  }

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await usersApi.list({
        search: search || undefined,
        active: statusFilter === 'active' ? true : statusFilter === 'inactive' ? false : undefined,
        mustChangePwd: statusFilter === 'mustChange' ? true : undefined,
        page,
        pageSize: PAGE_SIZE,
      })
      setUsers(data?.items ?? [])
      setTotal(data?.total ?? 0)
      setTotalPages(data?.totalPages ?? 0)
    } catch (err) {
      setError(err.message || 'Failed to load users')
    } finally {
      setLoading(false)
    }
  }, [search, statusFilter, page])

  useEffect(() => { load() }, [load])

  useEffect(() => {
    setPage(1)
  }, [search, statusFilter])

  const handleToggleActive = async (u) => {
    try {
      await usersApi.setActive(u.id, !u.isActive)
      showNotice(u.isActive ? 'User deactivated.' : 'User activated.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to update user status')
    }
  }

  const openEdit = async (u) => {
    setError('')
    try {
      const rs = (await rolesApi.list()) ?? []
      setRoles(rs.filter((r) => r.isActive !== false))
    } catch { /* ignore role load error */ }
    setEditUser({ id: u.id, username: u.username, roleId: '' })
  }

  const toggleRoleId = (e) => setEditUser((s) => (s ? { ...s, roleId: e.target.value } : s))

  const handleEditSave = async () => {
    if (!editUser) return
    setEditSaving(true)
    setError('')
    try {
      await usersApi.update(editUser.id, { roleId: editUser.roleId || undefined })
      setEditUser(null)
      showNotice('User updated.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to update user')
    } finally {
      setEditSaving(false)
    }
  }

  const openReset = (u) => {
    setError('')
    setResetPwd(generateTempPassword())
    setResetUser(u)
  }

  const regenerateReset = () => setResetPwd(generateTempPassword())

  const handleResetSave = async () => {
    if (!resetUser) return
    setResetSaving(true)
    setError('')
    try {
      await usersApi.resetPassword(resetUser.id, resetPwd)
      showNotice('Password reset. User must change on next login.')
      setResetUser(null)
      await load()
    } catch (err) {
      setError(err.message || 'Failed to reset password')
    } finally {
      setResetSaving(false)
    }
  }

  const resetFilters = () => {
    setSearch('')
    setStatusFilter('')
  }

  const startPage = (page - 1) * PAGE_SIZE + 1
  const endPage = Math.min(page * PAGE_SIZE, total)

  const isProtected = (u) => u.id === user?.id || u.roleName === 'SUPER_ADMIN'

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Users</h1>
          <p className="text-gray-500 text-sm mt-1">Auth accounts and their linked records.</p>
        </div>
        <button onClick={() => setShowAdd(true)} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
          + Add New User Account
        </button>
      </div>

      {notice && <div className="mb-4 rounded-lg bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm">{notice}</div>}
      {error && <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>}

      <div className="mb-6 bg-white rounded-xl shadow-sm border border-gray-200 p-4 flex flex-wrap items-center gap-3">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search by username, employee #, academic #, student #, email, full name"
          className="flex-1 min-w-[200px] rounded-lg border border-gray-300 px-4 py-2 text-gray-900 focus:ring-2 focus:ring-indigo-500 outline-none"
        />
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-700 focus:ring-2 focus:ring-indigo-500 outline-none"
        >
          <option value="">All status</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
          <option value="mustChange">Must Change PWD</option>
        </select>
        <button onClick={resetFilters} className="rounded-lg border border-gray-300 px-3 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50">
          Reset
        </button>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <p className="p-6 text-gray-500">Loading users...</p>
        ) : users.length === 0 ? (
          <p className="p-6 text-gray-500">No users found.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Username</th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Full name</th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Employee/Academic/Student #</th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Email</th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Department</th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Branch</th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Role</th>
                  <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                  <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {users.map((u) => (
                  <tr key={u.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 text-sm font-medium text-gray-900">
                      <span className="inline-flex items-center gap-2">
                        {u.username}
                        {u.mustChangePassword && (
                          <button
                            onClick={() => openReset(u)}
                            title="Must change password on next login - click to reset"
                            className="rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold text-amber-700 uppercase cursor-pointer hover:bg-amber-200"
                          >
                            change pwd
                          </button>
                        )}
                      </span>
                      {u.linkedEntity && (
                        <span className="block text-xs text-gray-400">Linked: {u.linkedEntity}</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-700">{u.fullName || '—'}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">
                      {u.identifierNumber ? <span className="font-medium text-gray-800">{u.identifierNumber}</span> : <span className="text-gray-300">—</span>}
                      {u.linkedEntity && <span className="block text-[10px] text-gray-400">{u.linkedEntity}</span>}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-700">{u.email || '—'}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">{u.departmentName || '—'}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">{u.branchName || '—'}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">{u.roleName || '—'}</td>
                    <td className="px-6 py-4 text-sm">
                      <span className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium ${u.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                        {u.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right whitespace-nowrap">
                      <button onClick={() => setViewUser(u)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">View</button>
                      <button onClick={() => openEdit(u)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">Edit</button>
                      {isProtected(u) ? (
                        <Badge tone="red">Protected</Badge>
                      ) : (
                        <>
                          <button onClick={() => handleToggleActive(u)} className="text-amber-600 hover:text-amber-800 text-sm font-medium mr-3">
                            {u.isActive ? 'Deactivate' : 'Activate'}
                          </button>
                          <button onClick={() => openReset(u)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium">
                            Reset Password
                          </button>
                        </>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {total > 0 && (
        <div className="mt-4 flex items-center justify-between text-sm text-gray-600">
          <span>Showing {startPage}–{endPage} of {total}</span>
          <div className="flex gap-2">
            <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1} className="rounded-lg border border-gray-300 px-3 py-1.5 font-medium disabled:opacity-40 hover:bg-gray-50">Prev</button>
            <span className="px-3 py-1.5">Page {page} / {totalPages || 1}</span>
            <button onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page >= totalPages} className="rounded-lg border border-gray-300 px-3 py-1.5 font-medium disabled:opacity-40 hover:bg-gray-50">Next</button>
          </div>
        </div>
      )}

      {showAdd && <AddUserAccountModal onClose={() => setShowAdd(false)} />}

      {viewUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-lg bg-white rounded-xl shadow-xl p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-900">User — {viewUser.username}</h2>
              <button onClick={() => setViewUser(null)} className="text-gray-400 hover:text-gray-600 text-xl leading-none">×</button>
            </div>
            <dl className="space-y-3 text-sm">
              {[
                ['Username', viewUser.username],
                ['Full Name', viewUser.fullName],
                ['Email', viewUser.email],
                ['Phone', viewUser.phoneNumber],
                ['Number', viewUser.identifierNumber],
                ['Department', viewUser.departmentName],
                ['Branch', viewUser.branchName],
                ['Role', viewUser.roleName],
                ['Linked', viewUser.linkedEntity],
                ['Status', viewUser.isActive ? 'Active' : 'Inactive'],
                ['Must Change PWD', viewUser.mustChangePassword ? 'Yes' : 'No'],
                ['Created', viewUser.createdAt],
              ].map(([label, value]) => (
                <div key={label} className="flex justify-between gap-4">
                  <dt className="text-xs font-medium text-gray-500 uppercase tracking-wide">{label}</dt>
                  <dd className="text-gray-900 text-right">{value || '—'}</dd>
                </div>
              ))}
            </dl>
            <div className="flex justify-end pt-4">
              <button onClick={() => setViewUser(null)} className="rounded-lg px-4 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-100">Close</button>
            </div>
          </div>
        </div>
      )}

      {editUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-lg bg-white rounded-xl shadow-xl p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-900">Edit User — {editUser.username}</h2>
              <button onClick={() => setEditUser(null)} className="text-gray-400 hover:text-gray-600 text-xl leading-none">×</button>
            </div>
            <div className="space-y-4">
              <Field label="Change Role">
                <select value={editUser.roleId} onChange={toggleRoleId} className={inputCls}>
                  <option value="">Current role</option>
                  {roles.map((r) => (
                    <option key={r.id} value={r.id}>{r.name || r.displayName}</option>
                  ))}
                </select>
              </Field>
              <p className="text-xs text-gray-500">ID cannot be changed.</p>
              <div className="flex justify-end gap-3 pt-2">
                <button onClick={() => setEditUser(null)} className="rounded-lg px-4 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-100">Cancel</button>
                <button onClick={handleEditSave} disabled={editSaving} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                  {editSaving ? 'Saving…' : 'Save'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {resetUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="w-full max-w-lg bg-white rounded-xl shadow-xl p-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-semibold text-gray-900">Reset Password — {resetUser.username}</h2>
              <button onClick={() => setResetUser(null)} className="text-gray-400 hover:text-gray-600 text-xl leading-none">×</button>
            </div>
            <div className="space-y-4">
              <div className="rounded-lg bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 text-sm">
                A temporary password will be set. The user must change it on next login.
              </div>
              <Field label="Temporary Password">
                <div className="flex items-center gap-2">
                  <input value={resetPwd} readOnly className={`${inputCls} bg-gray-50 text-gray-700 font-mono`} />
                  <button onClick={regenerateReset} className="rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-600 hover:bg-gray-50">Regenerate</button>
                </div>
              </Field>
              <div className="flex justify-end gap-3 pt-2">
                <button onClick={() => setResetUser(null)} className="rounded-lg px-4 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-100">Cancel</button>
                <button onClick={handleResetSave} disabled={resetSaving} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                  {resetSaving ? 'Saving…' : 'Reset Password'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
