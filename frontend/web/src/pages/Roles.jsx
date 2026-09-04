import { useCallback, useEffect, useState } from 'react'
import { permissionsApi, rolesApi } from '../api/client'
import { Field, Modal, inputCls } from '../components/ui'

export default function Roles() {
  const [roles, setRoles] = useState([])
  const [groups, setGroups] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [name, setName] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [description, setDescription] = useState('')
  const [saving, setSaving] = useState(false)

  const [editing, setEditing] = useState(null)
  const [selPermissions, setSelPermissions] = useState([])
  const [editTab, setEditTab] = useState('details')

  const showNotice = (msg) => {
    setNotice(msg)
    window.setTimeout(() => setNotice(''), 3500)
  }

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await rolesApi.list()
      setRoles((data ?? []).map(toRole))
    } catch (err) {
      setError(err.message || 'Failed to load roles')
    } finally {
      setLoading(false)
    }
  }, [])

  const loadGroups = useCallback(async () => {
    try {
      const data = await permissionsApi.grouped()
      setGroups(data ?? [])
    } catch {
      setGroups([])
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  useEffect(() => {
    loadGroups()
  }, [loadGroups])

  const handleCreate = async (e) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      await rolesApi.create({ name: name.trim(), displayName: displayName.trim() || null, description: description.trim() || null })
      setShowCreate(false)
      setName('')
      setDisplayName('')
      setDescription('')
      showNotice('Role created.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to create role')
    } finally {
      setSaving(false)
    }
  }

  const openEdit = async (role) => {
    setError('')
    setEditTab('details')
    setEditing({ ...role })
    setSelPermissions([])
    try {
      const data = await rolesApi.getPermissionIds(role.id)
      setSelPermissions(data ?? [])
    } catch (err) {
      setError(err.message || 'Failed to load role permissions')
    }
  }

  const togglePermission = (id) => {
    setSelPermissions((prev) =>
      prev.includes(id) ? prev.filter((p) => p !== id) : [...prev, id]
    )
  }

  const handleEditSave = async (e) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      await rolesApi.update(editing.id, {
        displayName: editing.displayName?.trim() || null,
        description: editing.description?.trim() || null,
      })
      await rolesApi.setPermissions(editing.id, selPermissions)
      setEditing(null)
      showNotice('Role updated.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to update role')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (role) => {
    if (!window.confirm(`Delete role "${role.displayName}"?`)) return
    try {
      await rolesApi.remove(role.id)
      showNotice('Role deleted.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to delete role')
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Roles</h1>
          <p className="text-gray-500 text-sm mt-1">Manage application roles, permissions and security metadata.</p>
        </div>
        <button
          onClick={() => { setError(''); setShowCreate((v) => !v) }}
          className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 transition"
        >
          New Role
        </button>
      </div>

      {notice && (
        <div className="mb-4 rounded-lg bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm">
          {notice}
        </div>
      )}
      {error && (
        <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">
          {error}
        </div>
      )}

      {showCreate && (
        <form onSubmit={handleCreate} className="mb-6 bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-4">
          <h2 className="text-lg font-semibold text-gray-900">Create Role</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Field label="Name *">
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                placeholder="e.g. REGISTRAR"
                className={inputCls}
              />
            </Field>
            <Field label="Display name">
              <input
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                placeholder="e.g. Registrar"
                className={inputCls}
              />
            </Field>
          </div>
          <Field label="Description">
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={2} className={inputCls} />
          </Field>
          <div className="flex justify-end gap-3">
            <button
              type="button"
              onClick={() => setShowCreate(false)}
              className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={saving}
              className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {saving ? 'Creating...' : 'Create'}
            </button>
          </div>
        </form>
      )}

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <p className="p-6 text-gray-500">Loading roles...</p>
        ) : roles.length === 0 ? (
          <p className="p-6 text-gray-500">No roles found.</p>
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Name</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Display name</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Description</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Permissions</th>
                <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {roles.map((role) => (
                <tr key={role.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{role.name}</td>
                  <td className="px-6 py-4 text-sm text-gray-700">{role.displayName || role.name}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{role.description || '—'}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{role.permissions?.length ?? 0}</td>
                  <td className="px-6 py-4 text-right whitespace-nowrap">
                    <button onClick={() => openEdit(role)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">
                      Edit
                    </button>
                    {role.isSystemRole ? (
                      <span className="text-xs text-gray-400">System</span>
                    ) : (
                      <button
                        onClick={() => handleDelete(role)}
                        className="text-red-600 hover:text-red-800 text-sm font-medium"
                      >
                        Delete
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {editing && (
        <Modal title={`Edit Role — ${editing.name}`} onClose={() => setEditing(null)} wide>
          <div className="mb-4 flex gap-1 rounded-lg bg-gray-100 p-1 w-fit">
            <button
              type="button"
              onClick={() => setEditTab('details')}
              className={`rounded-md px-4 py-2 text-sm font-medium transition ${
                editTab === 'details' ? 'bg-indigo-600 text-white' : 'text-gray-600 hover:bg-gray-200'
              }`}
            >
              Details
            </button>
            <button
              type="button"
              onClick={() => setEditTab('permissions')}
              className={`rounded-md px-4 py-2 text-sm font-medium transition ${
                editTab === 'permissions' ? 'bg-indigo-600 text-white' : 'text-gray-600 hover:bg-gray-200'
              }`}
            >
              Permissions Mapping
            </button>
          </div>

          {editTab === 'details' ? (
            <form onSubmit={handleEditSave} className="space-y-5">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <Field label="Display name">
                  <input
                    value={editing.displayName || ''}
                    onChange={(e) => setEditing({ ...editing, displayName: e.target.value })}
                    className={inputCls}
                  />
                </Field>
                <Field label="Description">
                  <input
                    value={editing.description || ''}
                    onChange={(e) => setEditing({ ...editing, description: e.target.value })}
                    className={inputCls}
                  />
                </Field>
              </div>
              <div className="flex justify-end gap-3">
                <button type="button" onClick={() => setEditing(null)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">
                  Cancel
                </button>
                <button type="submit" disabled={saving} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                  {saving ? 'Saving...' : 'Save'}
                </button>
              </div>
            </form>
          ) : (
            <div className="space-y-5">
              <div className="flex items-center justify-between flex-wrap gap-2">
                <p className="text-sm text-gray-500">
                  {selPermissions.length} permission{selPermissions.length === 1 ? '' : 's'} selected
                </p>
                <button
                  type="button"
                  onClick={() =>
                    setSelPermissions(
                      selPermissions.length === groups.reduce((n, g) => n + g.permissions.length, 0)
                        ? []
                        : groups.flatMap((g) => g.permissions.map((p) => p.id))
                    )
                  }
                  className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
                >
                  {selPermissions.length === groups.reduce((n, g) => n + g.permissions.length, 0) ? 'Clear all' : 'Select all'}
                </button>
              </div>
              {groups.length === 0 ? (
                <p className="text-sm text-gray-400">No permission groups available.</p>
              ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  {groups.map((group) => (
                    <div key={group.moduleCode || group.module} className="rounded-lg border border-gray-200 p-3">
                      <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">{group.module}</p>
                      <div className="space-y-1.5 max-h-56 overflow-y-auto">
                        {group.permissions.map((perm) => (
                          <label key={perm.id} className="flex items-start gap-2 text-sm text-gray-700">
                            <input
                              type="checkbox"
                              checked={selPermissions.includes(perm.id)}
                              onChange={() => togglePermission(perm.id)}
                              className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500 mt-0.5"
                            />
                            <span>
                              <span className="font-medium">{perm.code || perm.name}</span>
                              {perm.description && <span className="block text-xs text-gray-400">{perm.description}</span>}
                            </span>
                          </label>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              )}
              <div className="flex justify-end gap-3">
                <button type="button" onClick={() => setEditing(null)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleEditSave}
                  disabled={saving}
                  className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
                >
                  {saving ? 'Saving...' : 'Save permissions'}
                </button>
              </div>
            </div>
          )}
        </Modal>
      )}
    </div>
  )
}

function toRole(r) {
  return {
    id: r.id,
    name: r.name,
    displayName: r.displayName,
    description: r.description,
    isSystemRole: r.isSystemRole,
    isActive: r.isActive,
    permissions: r.permissions || [],
  }
}