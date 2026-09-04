import { useCallback, useEffect, useMemo, useState } from 'react'
import { adminDepartmentsApi, branchesApi } from '../api/client'
import { Badge, Field, Modal, inputCls } from '../components/ui'

function emptyForm() {
  return { branchId: '', departmentName: '', departmentCode: '', description: '', isActive: true }
}

function validationText(err) {
  const details = err?.data?.errors
  if (details && typeof details === 'object') {
    const entries = Object.values(details).flat().join(' ')
    return entries ? `${err.message || 'Validation failed.'} ${entries}` : (err?.message || 'Request failed')
  }
  return err?.message || 'Request failed'
}

export default function AdministrativeDepartments() {
  const [rows, setRows] = useState([])
  const [branches, setBranches] = useState([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState(emptyForm())
  const [editing, setEditing] = useState(null)

  const showNotice = (msg) => { setNotice(msg); setTimeout(() => setNotice(''), 3000) }

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await adminDepartmentsApi.list()
      setRows(data ?? [])
    } catch (err) {
      setError(err.message || 'Failed to load administrative departments')
    } finally {
      setLoading(false)
    }
  }, [])

  const loadBranches = useCallback(async () => {
    try {
      const data = await branchesApi.list()
      setBranches(data ?? [])
    } catch {
      setBranches([])
    }
  }, [])

  useEffect(() => { load() }, [load])
  useEffect(() => { loadBranches() }, [loadBranches])

  const stats = useMemo(() => {
    const total = rows.length
    const active = rows.filter((r) => r.isActive).length
    const branchesCount = new Set(rows.filter((r) => r.branchId).map((r) => r.branchId)).size
    return { total, active, branches: branchesCount }
  }, [rows])

  const defaultBranchId = branches.length === 1 ? branches[0].id : ''

  const openCreate = () => {
    setError('')
    setForm({ ...emptyForm(), branchId: defaultBranchId })
    setShowCreate(true)
  }

  const handleCreate = async (e) => {
    e.preventDefault()
    setError('')
    if (!form.branchId) { setError('Please select a branch.'); return }
    setSaving(true)
    try {
      await adminDepartmentsApi.create({
        branchId: form.branchId,
        departmentName: form.departmentName.trim(),
        departmentCode: form.departmentCode.trim().toUpperCase(),
        description: form.description?.trim() || null,
        isActive: form.isActive,
      })
      setShowCreate(false)
      setForm(emptyForm())
      showNotice('Administrative department created successfully.')
      await load()
    } catch (err) {
      setError(validationText(err))
    } finally {
      setSaving(false)
    }
  }

  const openEdit = (row) => {
    setError('')
    setEditing({ ...row, description: row.description || '' })
  }

  const handleUpdate = async (e) => {
    e.preventDefault()
    if (!editing) return
    setError('')
    setSaving(true)
    try {
      await adminDepartmentsApi.update(editing.id, {
        departmentName: editing.departmentName,
        departmentCode: editing.departmentCode,
        description: editing.description || null,
        isActive: editing.isActive,
      })
      setEditing(null)
      showNotice('Administrative department updated successfully.')
      await load()
    } catch (err) {
      setError(validationText(err))
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (row) => {
    if (!window.confirm(`Deactivate administrative department "${row.departmentName}"?`)) return
    setError('')
    try {
      await adminDepartmentsApi.remove(row.id)
      showNotice('Administrative department deactivated.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to deactivate administrative department')
    }
  }

  const handleRestore = async (id) => {
    setError('')
    try {
      await adminDepartmentsApi.restore(id)
      showNotice('Administrative department restored.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to restore administrative department')
    }
  }

  const branchNameOf = (id) => branches.find((b) => b.id === id)?.branchName || '—'
  const truncate = (s, n = 50) => (s && s.length > n ? `${s.slice(0, n)}…` : s || '—')

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Administrative Departments</h1>
          <p className="text-gray-500 text-sm mt-1">Manage administrative structure mapped to campus branches.</p>
        </div>
        <button onClick={openCreate} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 transition">
          Add Administrative Department
        </button>
      </div>

      {notice && <div className="mb-4 rounded-lg bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm">{notice}</div>}
      {error && <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm whitespace-pre-wrap">{error}</div>}

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <StatCard label="Total Departments" value={stats.total} />
        <StatCard label="Active" value={stats.active} />
        <StatCard label="Branches" value={stats.branches} />
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <p className="p-6 text-gray-500">Loading administrative departments...</p>
        ) : rows.length === 0 ? (
          <p className="p-6 text-gray-500">No administrative departments yet. Use "Add Administrative Department" to create one.</p>
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Name</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Code</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Branch</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Description</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {rows.map((row) => (
                <tr key={row.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{row.departmentName}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{row.departmentCode || '—'}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{row.branchName || branchNameOf(row.branchId)}</td>
                  <td className="px-6 py-4 text-sm text-gray-500 max-w-sm">{truncate(row.description)}</td>
                  <td className="px-6 py-4">
                    {row.isActive ? <Badge tone="green">Active</Badge> : <Badge tone="red">Inactive</Badge>}
                  </td>
                  <td className="px-6 py-4 text-right whitespace-nowrap">
                    <button onClick={() => openEdit(row)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">
                      Edit
                    </button>
                    {row.isActive ? (
                      <button onClick={() => handleDelete(row)} className="text-red-600 hover:text-red-800 text-sm font-medium">
                        Deactivate
                      </button>
                    ) : (
                      <button onClick={() => handleRestore(row.id)} className="text-green-600 hover:text-green-800 text-sm font-medium">
                        Restore
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && (
        <Modal title="Add Administrative Department" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate} className="space-y-4">
            <Field label="Branch *">
              <select value={form.branchId} onChange={(e) => setForm({ ...form, branchId: e.target.value })} className={inputCls} required>
                <option value="">Select branch</option>
                {branches.map((b) => <option key={b.id} value={b.id}>{b.branchName} ({b.branchCode})</option>)}
              </select>
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Department Name *">
                <input value={form.departmentName} onChange={(e) => setForm({ ...form, departmentName: e.target.value })} placeholder="Student Affairs" required className={inputCls} />
              </Field>
              <Field label="Department Code *">
                <input value={form.departmentCode} onChange={(e) => setForm({ ...form, departmentCode: e.target.value })} placeholder="SA" required className={inputCls} />
              </Field>
            </div>
            <Field label="Description">
              <textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} rows={3} className={inputCls} />
            </Field>
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500" />
              Active
            </label>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setShowCreate(false)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">Cancel</button>
              <button type="submit" disabled={saving} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                {saving ? 'Creating...' : 'Create'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {editing && (
        <Modal title={`Edit — ${editing.departmentName}`} onClose={() => setEditing(null)}>
          <form onSubmit={handleUpdate} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Department Name *">
                <input value={editing.departmentName || ''} onChange={(e) => setEditing({ ...editing, departmentName: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Department Code *">
                <input value={editing.departmentCode || ''} onChange={(e) => setEditing({ ...editing, departmentCode: e.target.value })} required className={inputCls} />
              </Field>
            </div>
            <Field label="Description">
              <textarea value={editing.description || ''} onChange={(e) => setEditing({ ...editing, description: e.target.value })} rows={3} className={inputCls} />
            </Field>
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input type="checkbox" checked={editing.isActive} onChange={(e) => setEditing({ ...editing, isActive: e.target.checked })} className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500" />
              Active
            </label>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setEditing(null)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">Cancel</button>
              <button type="submit" disabled={saving} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                {saving ? 'Saving...' : 'Save'}
              </button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}

function StatCard({ label, value }) {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-5">
      <p className="text-sm font-medium text-gray-500">{label}</p>
      <p className="mt-1 text-3xl font-bold text-gray-900">{value}</p>
    </div>
  )
}