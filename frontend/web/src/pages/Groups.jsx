import { useCallback, useEffect, useMemo, useState } from 'react'
import { groupsApi, usersApi } from '../api/client'
import { Field, Modal, inputCls } from '../components/ui'

export default function Groups() {
  const [groups, setGroups] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [search, setSearch] = useState('')

  const [showCreate, setShowCreate] = useState(false)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [saving, setSaving] = useState(false)

  const [editing, setEditing] = useState(null)

  const [memberGroup, setMemberGroup] = useState(null)
  const [members, setMembers] = useState([])
  const [allUsers, setAllUsers] = useState([])
  const [addUserId, setAddUserId] = useState('')

  const showNotice = (msg) => {
    setNotice(msg)
    window.setTimeout(() => setNotice(''), 3500)
  }

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = (await groupsApi.list()) ?? []
      const counts = await Promise.all(
        data.map(async (g) => {
          try {
            const mem = (await groupsApi.getMembers(g.id)) ?? []
            return { id: g.id, count: mem.length }
          } catch {
            return { id: g.id, count: 0 }
          }
        })
      )
      const countById = Object.fromEntries(counts.map((c) => [c.id, c.count]))
      setGroups(data.map((g) => ({ ...g, memberCount: countById[g.id] ?? 0 })))
    } catch (err) {
      setError(err.message || 'Failed to load groups')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const loadUsers = useCallback(async () => {
    try {
      const data = await usersApi.list({ pageSize: 200 })
      setAllUsers(data?.items ?? [])
    } catch {
      setAllUsers([])
    }
  }, [])

  useEffect(() => {
    loadUsers()
  }, [loadUsers])

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return groups
    return groups.filter(
      (g) =>
        (g.name || '').toLowerCase().includes(term) ||
        (g.displayName || '').toLowerCase().includes(term) ||
        (g.description || '').toLowerCase().includes(term)
    )
  }, [groups, search])

  const openCreate = () => {
    setError('')
    setName('')
    setDescription('')
    setShowCreate(true)
  }

  const handleCreate = async (e) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      await groupsApi.create({ name: name.trim(), description: description.trim() || null })
      setShowCreate(false)
      showNotice('Group created.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to create group')
    } finally {
      setSaving(false)
    }
  }

  const handleEditSave = async (e) => {
    e.preventDefault()
    setSaving(true)
    setError('')
    try {
      await groupsApi.update(editing.id, {
        displayName: editing.displayName || null,
        description: editing.description || null,
        isActive: editing.isActive,
      })
      setEditing(null)
      showNotice('Group updated.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to update group')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (group) => {
    if (!window.confirm(`Delete group "${group.name}"?`)) return
    try {
      await groupsApi.remove(group.id)
      showNotice('Group deleted.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to delete group')
    }
  }

  const openMembers = async (group) => {
    setError('')
    setMemberGroup(group)
    setMembers([])
    setAddUserId('')
    try {
      const data = await groupsApi.getMembers(group.id)
      setMembers(data ?? [])
    } catch (err) {
      setError(err.message || 'Failed to load members')
    }
  }

  const handleAddMember = async () => {
    if (!addUserId) return
    setSaving(true)
    setError('')
    try {
      await groupsApi.addMember(memberGroup.id, addUserId)
      setAddUserId('')
      showNotice('Member added.')
      const data = await groupsApi.getMembers(memberGroup.id)
      setMembers(data ?? [])
      await load()
    } catch (err) {
      setError(err.message || 'Failed to add member')
    } finally {
      setSaving(false)
    }
  }

  const handleRemoveMember = async (userId) => {
    if (!window.confirm('Remove this member from the group?')) return
    try {
      await groupsApi.removeMember(memberGroup.id, userId)
      showNotice('Member removed.')
      setMembers((prev) => prev.filter((m) => m.id !== userId))
      await load()
    } catch (err) {
      setError(err.message || 'Failed to remove member')
    }
  }

  const memberIds = new Set(members.map((m) => m.id))
  const addableUsers = allUsers.filter((u) => !memberIds.has(u.id))

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Groups</h1>
          <p className="text-gray-500 text-sm mt-1">Manage security groups and their members.</p>
        </div>
        <button onClick={openCreate} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 transition">
          New Group
        </button>
      </div>

      {notice && (
        <div className="mb-4 rounded-lg bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm">{notice}</div>
      )}
      {error && (
        <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>
      )}

      <div className="mb-6 bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search groups by name or description"
          className="w-full rounded-lg border border-gray-300 px-4 py-2 text-gray-900 focus:ring-2 focus:ring-indigo-500 outline-none"
        />
      </div>

      {showCreate && (
        <form onSubmit={handleCreate} className="mb-6 bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-4">
          <h2 className="text-lg font-semibold text-gray-900">Create Group</h2>
          <Field label="Name *">
            <input value={name} onChange={(e) => setName(e.target.value)} required placeholder="e.g. registrars" className={inputCls} />
          </Field>
          <Field label="Description">
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={2} className={inputCls} />
          </Field>
          <div className="flex justify-end gap-3">
            <button type="button" onClick={() => setShowCreate(false)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">
              Cancel
            </button>
            <button type="submit" disabled={saving} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
              {saving ? 'Creating...' : 'Create'}
            </button>
          </div>
        </form>
      )}

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <p className="p-6 text-gray-500">Loading groups...</p>
        ) : filtered.length === 0 ? (
          <p className="p-6 text-gray-500">No groups found.</p>
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Name</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Description</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Members</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Created</th>
                <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {filtered.map((group) => (
                <tr key={group.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{group.name}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{group.description || '—'}</td>
                  <td className="px-6 py-4">
                    <span className="inline-flex items-center rounded-full bg-indigo-100 px-2.5 py-0.5 text-xs font-medium text-indigo-700">
                      {group.memberCount}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">{group.createdAt ? formatDate(group.createdAt) : '—'}</td>
                  <td className="px-6 py-4 text-right whitespace-nowrap">
                    <button onClick={() => openMembers(group)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">
                      Members
                    </button>
                    <button onClick={() => { setError(''); setEditing({ ...group }) }} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">
                      Edit
                    </button>
                    <button onClick={() => handleDelete(group)} className="text-red-600 hover:text-red-800 text-sm font-medium">
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {editing && (
        <Modal title={`Edit Group — ${editing.name}`} onClose={() => setEditing(null)}>
          <form onSubmit={handleEditSave} className="space-y-4">
            <Field label="Display name">
              <input value={editing.displayName || ''} onChange={(e) => setEditing({ ...editing, displayName: e.target.value })} className={inputCls} />
            </Field>
            <Field label="Description">
              <textarea value={editing.description || ''} onChange={(e) => setEditing({ ...editing, description: e.target.value })} rows={2} className={inputCls} />
            </Field>
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input
                type="checkbox"
                checked={editing.isActive}
                onChange={(e) => setEditing({ ...editing, isActive: e.target.checked })}
                className="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              Active
            </label>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setEditing(null)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">
                Cancel
              </button>
              <button type="submit" disabled={saving} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                {saving ? 'Saving...' : 'Save'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {memberGroup && (
        <Modal title={`Members — ${memberGroup.name}`} onClose={() => setMemberGroup(null)} wide>
          <div className="space-y-4">
            <div className="flex items-end gap-3">
              <div className="flex-1">
                <Field label="Add member">
                  <select value={addUserId} onChange={(e) => setAddUserId(e.target.value)} className={inputCls}>
                    <option value="">Select a user...</option>
                    {addableUsers.map((u) => (
                      <option key={u.id} value={u.id}>{u.username} — {u.email}</option>
                    ))}
                  </select>
                </Field>
              </div>
              <button
                type="button"
                onClick={handleAddMember}
                disabled={!addUserId || saving}
                className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
              >
                Add
              </button>
            </div>

            <div className="rounded-lg border border-gray-200 overflow-hidden">
              {members.length === 0 ? (
                <p className="p-4 text-sm text-gray-400">No members yet.</p>
              ) : (
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Username</th>
                      <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Email</th>
                      <th className="px-4 py-2 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {members.map((m) => (
                      <tr key={m.id}>
                        <td className="px-4 py-3 text-sm font-medium text-gray-900">{m.username}</td>
                        <td className="px-4 py-3 text-sm text-gray-700">{m.email}</td>
                        <td className="px-4 py-3 text-right">
                          <button onClick={() => handleRemoveMember(m.id)} className="text-red-600 hover:text-red-800 text-sm font-medium">
                            Remove
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}

function formatDate(value) {
  const d = new Date(value)
  return isNaN(d.getTime()) ? '—' : d.toLocaleDateString()
}