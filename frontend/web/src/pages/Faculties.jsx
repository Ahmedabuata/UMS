import { Fragment, useCallback, useEffect, useMemo, useState } from 'react'
import { branchesApi, facultiesApi, academicDepartmentsApi } from '../api/client'
import { Badge, Field, Modal, inputCls } from '../components/ui'

function emptyFaculty() {
  return { facultyName: '', facultyCode: '', deanName: '', branchId: '', location: '', description: '' }
}

function emptyDepartment(facultyId = '') {
  return { facultyId, departmentName: '', departmentCode: '', headName: '', description: '' }
}

export default function Faculties() {
  const [rows, setRows] = useState([])
  const [branches, setBranches] = useState([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [expanded, setExpanded] = useState({})
  const [deptMap, setDeptMap] = useState({})

  const [showFaculty, setShowFaculty] = useState(false)
  const [facultyForm, setFacultyForm] = useState(emptyFaculty())
  const [editingFaculty, setEditingFaculty] = useState(null)

  const [showDept, setShowDept] = useState(false)
  const [deptForm, setDeptForm] = useState(emptyDepartment())
  const [editingDept, setEditingDept] = useState(null)
  const [deptLoading, setDeptLoading] = useState(false)

  const showNotice = (msg) => { setNotice(msg); setTimeout(() => setNotice(''), 3000) }

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await facultiesApi.list()
      setRows(data ?? [])
    } catch (err) {
      setError(err.message || 'Failed to load faculties')
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
    const departments = rows.reduce((s, r) => s + (r.departmentsCount || 0), 0)
    return { total, active, departments }
  }, [rows])

  const defaultBranchId = branches.length === 1 ? branches[0].id : ''
  const branchNameOf = (id) => branches.find((b) => b.id === id)?.branchName || '—'

  const toggleExpand = async (facultyId) => {
    setExpanded((prev) => ({ ...prev, [facultyId]: !prev[facultyId] }))
    if (!deptMap[facultyId]) {
      setDeptLoading(true)
      try {
        const data = await facultiesApi.academicDepartments(facultyId)
        setDeptMap((prev) => ({ ...prev, [facultyId]: data ?? [] }))
      } catch {
        setDeptMap((prev) => ({ ...prev, [facultyId]: [] }))
      } finally {
        setDeptLoading(false)
      }
    }
  }

  const openCreateFaculty = () => {
    setError('')
    setFacultyForm(emptyFaculty())
    if (branches.length === 1) setFacultyForm((f) => ({ ...f, branchId: branches[0].id }))
    setShowFaculty(true)
  }

  const handleCreateFaculty = async (e) => {
    e.preventDefault()
    setError('')
    if (!facultyForm.branchId) { setError('Please select a branch.'); return }
    setSaving(true)
    try {
      await facultiesApi.create({
        facultyName: facultyForm.facultyName.trim(),
        facultyCode: facultyForm.facultyCode.trim().toUpperCase(),
        deanName: facultyForm.deanName?.trim() || null,
        branchId: facultyForm.branchId,
        location: facultyForm.location?.trim() || null,
        description: facultyForm.description?.trim() || null,
      })
      setShowFaculty(false)
      setFacultyForm(emptyFaculty())
      showNotice('Faculty created successfully.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to create faculty')
    } finally {
      setSaving(false)
    }
  }

  const openEditFaculty = (row) => {
    setError('')
    setEditingFaculty({
      ...row,
      deanName: row.deanName || '',
      location: row.location || '',
      description: row.description || '',
      branchId: row.branchId || '',
    })
  }

  const handleUpdateFaculty = async (e) => {
    e.preventDefault()
    if (!editingFaculty) return
    setError('')
    if (!editingFaculty.branchId) { setError('Please select a branch.'); return }
    setSaving(true)
    try {
      await facultiesApi.update(editingFaculty.id, {
        facultyName: editingFaculty.facultyName.trim(),
        facultyCode: editingFaculty.facultyCode.trim().toUpperCase(),
        deanName: editingFaculty.deanName?.trim() || null,
        branchId: editingFaculty.branchId,
        location: editingFaculty.location?.trim() || null,
        description: editingFaculty.description?.trim() || null,
      })
      setEditingFaculty(null)
      showNotice('Faculty updated successfully.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to update faculty')
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteFaculty = async (row) => {
    if (!window.confirm(`Delete faculty "${row.facultyName}" and its departments?`)) return
    setError('')
    try {
      await facultiesApi.remove(row.id)
      showNotice('Faculty deleted.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to delete faculty')
    }
  }

  const openCreateDept = (facultyId) => {
    setError('')
    setDeptForm(emptyDepartment(facultyId))
    setShowDept(true)
  }

  const handleCreateDept = async (e) => {
    e.preventDefault()
    setError('')
    if (!deptForm.facultyId) { setError('Please select a faculty.'); return }
    setSaving(true)
    try {
      const created = await academicDepartmentsApi.create({
        facultyId: deptForm.facultyId,
        departmentName: deptForm.departmentName.trim(),
        departmentCode: deptForm.departmentCode.trim().toUpperCase(),
        headName: deptForm.headName?.trim() || null,
        description: deptForm.description?.trim() || null,
      })
      setShowDept(false)
      setDeptForm(emptyDepartment())
      showNotice('Department created.')
      await load()
      setDeptMap((prev) => ({
        ...prev,
        [created.facultyId]: [...(prev[created.facultyId] || []), created],
      }))
    } catch (err) {
      setError(err.message || 'Failed to create department')
    } finally {
      setSaving(false)
    }
  }

  const openEditDept = (dept) => {
    setError('')
    setEditingDept({
      ...dept,
      headName: dept.headName || '',
      description: dept.description || '',
      facultyId: dept.facultyId || '',
    })
  }

  const handleUpdateDept = async (e) => {
    e.preventDefault()
    if (!editingDept) return
    setError('')
    setSaving(true)
    try {
      const updated = await academicDepartmentsApi.update(editingDept.id, {
        facultyId: editingDept.facultyId,
        departmentName: editingDept.departmentName.trim(),
        departmentCode: editingDept.departmentCode.trim().toUpperCase(),
        headName: editingDept.headName?.trim() || null,
        description: editingDept.description?.trim() || null,
      })
      setEditingDept(null)
      showNotice('Department updated.')
      setDeptMap((prev) => ({
        ...prev,
        [updated.facultyId]: (prev[updated.facultyId] || []).map((d) => (d.id === updated.id ? updated : d)),
      }))
      await load()
    } catch (err) {
      setError(err.message || 'Failed to update department')
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteDept = async (dept) => {
    if (!window.confirm(`Delete department "${dept.departmentName}"?`)) return
    setError('')
    try {
      await academicDepartmentsApi.remove(dept.id)
      showNotice('Department deleted.')
      setDeptMap((prev) => ({
        ...prev,
        [dept.facultyId]: (prev[dept.facultyId] || []).filter((d) => d.id !== dept.id),
      }))
      await load()
    } catch (err) {
      setError(err.message || 'Failed to delete department')
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Faculties &amp; Academic Departments</h1>
          <p className="text-gray-500 text-sm mt-1">Manage faculties and their academic departments mapped to campus branches.</p>
        </div>
        <button onClick={openCreateFaculty} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 transition">
          Add Faculty
        </button>
      </div>

      {notice && <div className="mb-4 rounded-lg bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm">{notice}</div>}
      {error && <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>}

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <StatCard label="Total Faculties" value={stats.total} />
        <StatCard label="Active" value={stats.active} />
        <StatCard label="Academic Departments" value={stats.departments} />
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <p className="p-6 text-gray-500">Loading faculties...</p>
        ) : rows.length === 0 ? (
          <p className="p-6 text-gray-500">No faculties configured yet. Use "Add Faculty" to create one.</p>
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider"></th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Name</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Code</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Dean</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Location</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Departments</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {rows.map((row) => {
                const isOpen = !!expanded[row.id]
                const depts = deptMap[row.id]
                return (
                  <Fragment key={row.id}>
                    <tr
                      className="hover:bg-gray-50 cursor-pointer"
                      onClick={() => toggleExpand(row.id)}
                    >
                      <td className="px-4 py-4 text-gray-400">
                        <span className={`inline-block transition-transform ${isOpen ? 'rotate-90' : ''}`}>▶</span>
                      </td>
                      <td className="px-6 py-4 text-sm font-medium text-gray-900">{row.facultyName}</td>
                      <td className="px-6 py-4 text-sm text-gray-500">{row.facultyCode || '—'}</td>
                      <td className="px-6 py-4 text-sm text-gray-500">{row.deanName || '—'}</td>
                      <td className="px-6 py-4 text-sm text-gray-500">{row.branchName || '—'}</td>
                      <td className="px-6 py-4 text-sm">
                        <span className="inline-flex items-center rounded-full bg-indigo-100 text-indigo-700 px-2 py-0.5 text-xs font-medium">{row.departmentsCount || 0}</span>
                      </td>
                      <td className="px-6 py-4">
                        {row.isActive ? <Badge tone="green">Active</Badge> : <Badge tone="red">Inactive</Badge>}
                      </td>
                      <td className="px-6 py-4 text-right whitespace-nowrap">
                        <button
                          onClick={(e) => { e.stopPropagation(); setError(''); openCreateDept(row.id) }}
                          className="text-gray-600 hover:text-gray-800 text-sm font-medium mr-3"
                        >
                          + Dept
                        </button>
                        <button onClick={(e) => { e.stopPropagation(); openEditFaculty(row) }} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">
                          Edit
                        </button>
                        <button
                          onClick={(e) => { e.stopPropagation(); handleDeleteFaculty(row) }}
                          className="text-red-600 hover:text-red-800 text-sm font-medium"
                        >
                          Delete
                        </button>
                      </td>
                    </tr>
                    {isOpen && (
                      <tr className="bg-gray-50">
                        <td colSpan={8} className="px-6 py-4">
                          <div className="mb-2 flex items-center justify-between">
                            <p className="text-sm font-semibold text-gray-700">Academic Departments</p>
                            <button onClick={() => openCreateDept(row.id)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium">
                              + Add Department
                            </button>
                          </div>
                          {deptLoading && !depts ? (
                            <p className="text-sm text-gray-500">Loading departments...</p>
                          ) : !depts || depts.length === 0 ? (
                            <p className="text-sm text-gray-500">No departments yet.</p>
                          ) : (
                            <table className="min-w-full divide-y divide-gray-200 border border-gray-200 rounded-lg overflow-hidden">
                              <thead className="bg-gray-100">
                                <tr>
                                  <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Code</th>
                                  <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Name</th>
                                  <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Head</th>
                                  <th className="px-4 py-2 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Description</th>
                                  <th className="px-4 py-2 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
                                </tr>
                              </thead>
                              <tbody className="bg-white divide-y divide-gray-200">
                                {depts.map((d) => (
                                  <tr key={d.id} className="hover:bg-gray-50">
                                    <td className="px-4 py-3 text-sm font-medium text-gray-900">{d.departmentCode}</td>
                                    <td className="px-4 py-3 text-sm text-gray-700">{d.departmentName}</td>
                                    <td className="px-4 py-3 text-sm text-gray-500">{d.headName || '—'}</td>
                                    <td className="px-4 py-3 text-sm text-gray-500 max-w-xs">{d.description || '—'}</td>
                                    <td className="px-4 py-3 text-right whitespace-nowrap">
                                      <button onClick={() => openEditDept(d)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">Edit</button>
                                      <button onClick={() => handleDeleteDept(d)} className="text-red-600 hover:text-red-800 text-sm font-medium">Delete</button>
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          )}
                        </td>
                      </tr>
                    )}
                  </Fragment>
                )
              })}
            </tbody>
          </table>
        )}
      </div>

      {showFaculty && (
        <Modal title="Add Faculty" onClose={() => setShowFaculty(false)}>
          <form onSubmit={handleCreateFaculty} className="space-y-4">
            <Field label="Name *">
              <input value={facultyForm.facultyName} onChange={(e) => setFacultyForm({ ...facultyForm, facultyName: e.target.value })} required className={inputCls} />
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Code *">
                <input value={facultyForm.facultyCode} onChange={(e) => setFacultyForm({ ...facultyForm, facultyCode: e.target.value })} placeholder="ENG" required className={inputCls} />
              </Field>
              <Field label="Branch *">
                <select value={facultyForm.branchId} onChange={(e) => setFacultyForm({ ...facultyForm, branchId: e.target.value })} className={inputCls} required>
                  <option value="">Select branch</option>
                  {branches.map((b) => <option key={b.id} value={b.id}>{b.branchName} ({b.branchCode})</option>)}
                </select>
              </Field>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Dean">
                <input value={facultyForm.deanName} onChange={(e) => setFacultyForm({ ...facultyForm, deanName: e.target.value })} className={inputCls} />
              </Field>
              <Field label="Location">
                <input value={facultyForm.location} onChange={(e) => setFacultyForm({ ...facultyForm, location: e.target.value })} className={inputCls} />
              </Field>
            </div>
            <Field label="Description">
              <textarea value={facultyForm.description} onChange={(e) => setFacultyForm({ ...facultyForm, description: e.target.value })} rows={3} className={inputCls} />
            </Field>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setShowFaculty(false)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">Cancel</button>
              <button type="submit" disabled={saving} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                {saving ? 'Creating...' : 'Create'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {editingFaculty && (
        <Modal title={`Edit — ${editingFaculty.facultyName}`} onClose={() => setEditingFaculty(null)}>
          <form onSubmit={handleUpdateFaculty} className="space-y-4">
            <Field label="Name *">
              <input value={editingFaculty.facultyName || ''} onChange={(e) => setEditingFaculty({ ...editingFaculty, facultyName: e.target.value })} required className={inputCls} />
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Code *">
                <input value={editingFaculty.facultyCode || ''} onChange={(e) => setEditingFaculty({ ...editingFaculty, facultyCode: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Branch *">
                <select value={editingFaculty.branchId || ''} onChange={(e) => setEditingFaculty({ ...editingFaculty, branchId: e.target.value })} className={inputCls} required>
                  <option value="">Select branch</option>
                  {branches.map((b) => <option key={b.id} value={b.id}>{b.branchName} ({b.branchCode})</option>)}
                </select>
              </Field>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Dean">
                <input value={editingFaculty.deanName || ''} onChange={(e) => setEditingFaculty({ ...editingFaculty, deanName: e.target.value })} className={inputCls} />
              </Field>
              <Field label="Location">
                <input value={editingFaculty.location || ''} onChange={(e) => setEditingFaculty({ ...editingFaculty, location: e.target.value })} className={inputCls} />
              </Field>
            </div>
            <Field label="Description">
              <textarea value={editingFaculty.description || ''} onChange={(e) => setEditingFaculty({ ...editingFaculty, description: e.target.value })} rows={3} className={inputCls} />
            </Field>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setEditingFaculty(null)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">Cancel</button>
              <button type="submit" disabled={saving} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                {saving ? 'Saving...' : 'Save'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {showDept && (
        <Modal title="Add Department" onClose={() => setShowDept(false)}>
          <form onSubmit={handleCreateDept} className="space-y-4">
            <Field label="Faculty *">
              <select value={deptForm.facultyId} onChange={(e) => setDeptForm({ ...deptForm, facultyId: e.target.value })} className={inputCls} required>
                <option value="">Select faculty</option>
                {rows.map((f) => <option key={f.id} value={f.id}>{f.facultyName} ({f.facultyCode})</option>)}
              </select>
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Name *">
                <input value={deptForm.departmentName} onChange={(e) => setDeptForm({ ...deptForm, departmentName: e.target.value })} placeholder="Department of Computer Science" required className={inputCls} />
              </Field>
              <Field label="Code *">
                <input value={deptForm.departmentCode} onChange={(e) => setDeptForm({ ...deptForm, departmentCode: e.target.value })} placeholder="CS" required className={inputCls} />
              </Field>
            </div>
            <Field label="Description">
              <textarea value={deptForm.description} onChange={(e) => setDeptForm({ ...deptForm, description: e.target.value })} rows={3} className={inputCls} />
            </Field>
            <Field label="Head">
              <input value={deptForm.headName} onChange={(e) => setDeptForm({ ...deptForm, headName: e.target.value })} className={inputCls} />
            </Field>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setShowDept(false)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">Cancel</button>
              <button type="submit" disabled={saving} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50">
                {saving ? 'Creating...' : 'Create'}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {editingDept && (
        <Modal title={`Edit — ${editingDept.departmentName}`} onClose={() => setEditingDept(null)}>
          <form onSubmit={handleUpdateDept} className="space-y-4">
            <Field label="Faculty *">
              <select value={editingDept.facultyId || ''} onChange={(e) => setEditingDept({ ...editingDept, facultyId: e.target.value })} className={inputCls} required>
                <option value="">Select faculty</option>
                {rows.map((f) => <option key={f.id} value={f.id}>{f.facultyName} ({f.facultyCode})</option>)}
              </select>
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Name *">
                <input value={editingDept.departmentName || ''} onChange={(e) => setEditingDept({ ...editingDept, departmentName: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Code *">
                <input value={editingDept.departmentCode || ''} onChange={(e) => setEditingDept({ ...editingDept, departmentCode: e.target.value })} required className={inputCls} />
              </Field>
            </div>
            <Field label="Description">
              <textarea value={editingDept.description || ''} onChange={(e) => setEditingDept({ ...editingDept, description: e.target.value })} rows={3} className={inputCls} />
            </Field>
            <Field label="Head">
              <input value={editingDept.headName || ''} onChange={(e) => setEditingDept({ ...editingDept, headName: e.target.value })} className={inputCls} />
            </Field>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setEditingDept(null)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">Cancel</button>
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