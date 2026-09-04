import { useCallback, useEffect, useState } from 'react'
import { branchesApi } from '../api/client'
import { Field, Modal, inputCls } from '../components/ui'

function emptyForm() {
  return { branchName: '', branchCode: '', branchLocation: '', branchDescription: '' }
}

function validationText(err) {
  const details = err?.data?.errors
  if (details && typeof details === 'object') {
    const entries = Object.values(details).flat().join(' ')
    return entries ? `${err.message || 'Validation failed'}. ${entries}` : (err?.message || 'Request failed')
  }
  return err?.message || 'Request failed'
}

export default function Branches() {
  const [rows, setRows] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [search, setSearch] = useState('')

  const [showCreate, setShowCreate] = useState(false)
  const [createForm, setCreateForm] = useState(emptyForm())
  const [editing, setEditing] = useState(null)
  const [saving, setSaving] = useState(false)

  const showNotice = (msg) => {
    setNotice(msg)
    window.setTimeout(() => setNotice(''), 3500)
  }

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await branchesApi.list()
      setRows(data ?? [])
    } catch (err) {
      setError(validationText(err))
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  const filtered = rows.filter((r) => {
    const term = search.trim().toLowerCase()
    if (!term) return true
    return (
      (r.branchName || '').toLowerCase().includes(term) ||
      (r.branchCode || '').toLowerCase().includes(term) ||
      (r.branchLocation || '').toLowerCase().includes(term)
    )
  })

  const handleCreate = async (e) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      await branchesApi.create({
        branchName: createForm.branchName.trim(),
        branchCode: createForm.branchCode.trim().toUpperCase(),
        branchLocation: createForm.branchLocation.trim() || null,
        branchDescription: createForm.branchDescription.trim() || null,
      })
      setShowCreate(false)
      setCreateForm(emptyForm())
      showNotice('Branch created.')
      await load()
    } catch (err) {
      setError(validationText(err))
    } finally {
      setSaving(false)
    }
  }

  const handleEditSave = async (e) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      await branchesApi.update(editing.id, {
        branchName: editing.branchName,
        branchCode: editing.branchCode.trim().toUpperCase(),
        branchLocation: editing.branchLocation?.trim() || null,
        branchDescription: editing.branchDescription?.trim() || null,
      })
      setEditing(null)
      showNotice('Branch updated.')
      await load()
    } catch (err) {
      setError(validationText(err))
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (row) => {
    if (!window.confirm(`Delete branch "${row.branchName}"?`)) return
    try {
      await branchesApi.remove(row.id)
      showNotice('Branch deleted.')
      await load()
    } catch (err) {
      setError(validationText(err))
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Branches</h1>
          <p className="text-gray-500 text-sm mt-1">Manage institution branches and campus locations.</p>
        </div>
        <button
          onClick={() => { setError(''); setCreateForm(emptyForm()); setShowCreate(true) }}
          className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 transition"
        >
          New Branch
        </button>
      </div>

      {notice && <div className="mb-4 rounded-lg bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm">{notice}</div>}
      {error && <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm whitespace-pre-wrap">{error}</div>}

      <div className="mb-6 bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search by name, code or location"
          className="w-full rounded-lg border border-gray-300 px-4 py-2 text-gray-900 focus:ring-2 focus:ring-indigo-500 outline-none"
        />
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <p className="p-6 text-gray-500">Loading branches...</p>
        ) : filtered.length === 0 ? (
          <p className="p-6 text-gray-500">No branches found.</p>
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Name</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Code</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Location</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Description</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {filtered.map((row) => (
                <tr key={row.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{row.branchName}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{row.branchCode || '—'}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{row.branchLocation || '—'}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{row.branchDescription || '—'}</td>
                  <td className="px-6 py-4">
                    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${row.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                      {row.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-right whitespace-nowrap">
                    <button onClick={() => { setError(''); setEditing({ ...row }) }} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">
                      Edit
                    </button>
                    <button onClick={() => handleDelete(row)} className="text-red-600 hover:text-red-800 text-sm font-medium">
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && (
        <Modal title="New Branch" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate} className="space-y-4">
            <Field label="Name *">
              <input value={createForm.branchName} onChange={(e) => setCreateForm({ ...createForm, branchName: e.target.value })} required className={inputCls} />
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Code *">
                <input value={createForm.branchCode} onChange={(e) => setCreateForm({ ...createForm, branchCode: e.target.value })} placeholder="MAIN, GAZA, BR-02" required className={inputCls} />
              </Field>
              <Field label="Location *">
                <input value={createForm.branchLocation} onChange={(e) => setCreateForm({ ...createForm, branchLocation: e.target.value })} placeholder="Main Campus or street address" required className={inputCls} />
              </Field>
            </div>
            <Field label="Description">
              <textarea value={createForm.branchDescription} onChange={(e) => setCreateForm({ ...createForm, branchDescription: e.target.value })} rows={3} className={inputCls} />
            </Field>
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
        <Modal title={`Edit — ${editing.branchName}`} onClose={() => setEditing(null)}>
          <form onSubmit={handleEditSave} className="space-y-4">
            <Field label="Name *">
              <input value={editing.branchName || ''} onChange={(e) => setEditing({ ...editing, branchName: e.target.value })} required className={inputCls} />
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Code *">
                <input value={editing.branchCode || ''} onChange={(e) => setEditing({ ...editing, branchCode: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Location *">
                <input value={editing.branchLocation || ''} onChange={(e) => setEditing({ ...editing, branchLocation: e.target.value })} required className={inputCls} />
              </Field>
            </div>
            <Field label="Description">
              <textarea value={editing.branchDescription || ''} onChange={(e) => setEditing({ ...editing, branchDescription: e.target.value })} rows={3} className={inputCls} />
            </Field>
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