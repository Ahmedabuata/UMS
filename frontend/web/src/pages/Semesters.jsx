import { useCallback, useEffect, useMemo, useState } from 'react'
import { semestersApi } from '../api/client'
import { Badge, Field, Modal, inputCls } from '../components/ui'

const STATUS = {
  1: 'Upcoming',
  2: 'Open',
  3: 'Closed',
}

const STATUS_TONE = {
  1: 'amber',
  2: 'green',
  3: 'red',
}

function emptyForm() {
  return {
    semesterName: '',
    semesterCode: '',
    academicYear: '',
    startDate: '',
    endDate: '',
    registrationStart: '',
    registrationEnd: '',
    status: 1,
  }
}

export default function Semesters() {
  const [semesters, setSemesters] = useState([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState(emptyForm())
  const [editing, setEditing] = useState(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await semestersApi.list()
      setSemesters((data ?? []).sort((a, b) => (a.startDate > b.startDate ? 1 : -1)))
    } catch (err) {
      setError(err.message || 'Failed to load semesters')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { load() }, [load])

  const stats = useMemo(() => {
    return {
      total: semesters.length,
      current: semesters.filter((s) => s.isCurrent).length,
      open: semesters.filter((s) => s.status === 2).length,
      upcoming: semesters.filter((s) => s.status === 1).length,
    }
  }, [semesters])

  const flash = (msg) => { setNotice(msg); setTimeout(() => setNotice(''), 3000) }

  const fmt = (d) => (d ? String(d).slice(0, 10) : '')

  const handleCreate = async (e) => {
    e.preventDefault()
    setError('')
    setSaving(true)
    try {
      await semestersApi.create({
        semesterName: form.semesterName.trim(),
        semesterCode: form.semesterCode.trim(),
        academicYear: form.academicYear.trim(),
        startDate: form.startDate,
        endDate: form.endDate,
        registrationStart: form.registrationStart || null,
        registrationEnd: form.registrationEnd || null,
        status: Number(form.status) || 1,
      })
      setShowCreate(false)
      setForm(emptyForm())
      await load()
      flash('Semester created successfully.')
    } catch (err) {
      setError(err.message || 'Failed to create semester')
    } finally {
      setSaving(false)
    }
  }

  const handleUpdate = async (e) => {
    e.preventDefault()
    if (!editing) return
    setError('')
    setSaving(true)
    try {
      await semestersApi.update(editing.id, {
        semesterName: editing.semesterName,
        semesterCode: editing.semesterCode,
        academicYear: editing.academicYear,
        startDate: editing.startDate,
        endDate: editing.endDate,
        registrationStart: editing.registrationStart || null,
        registrationEnd: editing.registrationEnd || null,
        status: Number(editing.status) || 1,
      })
      setEditing(null)
      await load()
      flash('Semester updated successfully.')
    } catch (err) {
      setError(err.message || 'Failed to update semester')
    } finally {
      setSaving(false)
    }
  }

  const act = async (sem, fn, msg) => {
    setError('')
    try {
      await fn(sem.id)
      await load()
      flash(msg)
    } catch (err) {
      setError(err.message || msg)
    }
  }

  const handleDelete = async (sem) => {
    if (!window.confirm(`Delete semester "${sem.semesterName}"?`)) return
    await act(sem, semestersApi.remove, 'Semester deleted.')
  }

  const openEdit = (sem) => setEditing({ ...sem })
  const openCreate = () => { setError(''); setForm(emptyForm()); setShowCreate(true) }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Semesters</h1>
          <p className="text-gray-500 text-sm mt-1">Manage academic periods, openings and the current semester.</p>
        </div>
        <button onClick={openCreate} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 transition">
          Add Semester
        </button>
      </div>

      {notice && <div className="mb-4 rounded-lg bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm">{notice}</div>}
      {error && <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>}

      <div className="grid grid-cols-1 sm:grid-cols-4 gap-4 mb-6">
        <StatCard label="Total" value={stats.total} />
        <StatCard label="Current" value={stats.current} />
        <StatCard label="Open" value={stats.open} />
        <StatCard label="Upcoming" value={stats.upcoming} />
      </div>

      {/* Timeline */}
      {semesters.length > 0 && (
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
          <h2 className="text-sm font-semibold text-gray-700 mb-4">Timeline</h2>
          <div className="flex flex-wrap items-center gap-3">
            {semesters.map((s, idx) => (
              <div key={s.id} className="flex items-center gap-3">
                <div className="flex flex-col items-center">
                  <Badge tone={STATUS_TONE[s.status]}>{STATUS[s.status]}</Badge>
                  <span className="mt-1 text-sm font-medium text-gray-900">{s.semesterName}</span>
                  <span className="text-xs text-gray-500">{fmt(s.startDate)}</span>
                </div>
                {idx < semesters.length - 1 && <span className="text-gray-300">→</span>}
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <p className="p-6 text-gray-500">Loading semesters...</p>
        ) : semesters.length === 0 ? (
          <p className="p-6 text-gray-500">No semesters yet. Use "Add Semester" to create one.</p>
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Name</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Code</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Start – End</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Registration Period</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Current</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {semesters.map((sem) => (
                <tr key={sem.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{sem.semesterName}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{sem.semesterCode || '—'}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{fmt(sem.startDate)} – {fmt(sem.endDate)}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    {sem.registrationStart ? `${fmt(sem.registrationStart)} – ${fmt(sem.registrationEnd)}` : '—'}
                  </td>
                  <td className="px-6 py-4">{sem.isCurrent ? <Badge tone="green">Current</Badge> : <span className="text-gray-300">—</span>}</td>
                  <td className="px-6 py-4"><Badge tone={STATUS_TONE[sem.status]}>{STATUS[sem.status]}</Badge></td>
                  <td className="px-6 py-4 text-right whitespace-nowrap">
                    <button onClick={() => openEdit(sem)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">Edit</button>
                    {sem.status !== 2 && (
                      <button onClick={() => act(sem, semestersApi.open, 'Semester opened.')} className="text-green-600 hover:text-green-800 text-sm font-medium mr-3">Open</button>
                    )}
                    {sem.status !== 3 && (
                      <button onClick={() => act(sem, semestersApi.close, 'Semester closed.')} className="text-amber-600 hover:text-amber-800 text-sm font-medium mr-3">Close</button>
                    )}
                    <button onClick={() => act(sem, semestersApi.setCurrent, 'Semester set as current.')} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">Set Current</button>
                    <button onClick={() => handleDelete(sem)} className="text-red-600 hover:text-red-800 text-sm font-medium">Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && (
        <Modal title="Add Semester" onClose={() => setShowCreate(false)}>
          <form onSubmit={handleCreate} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Name *">
                <input value={form.semesterName} onChange={(e) => setForm({ ...form, semesterName: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Code *">
                <input value={form.semesterCode} onChange={(e) => setForm({ ...form, semesterCode: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Academic Year">
                <input value={form.academicYear} onChange={(e) => setForm({ ...form, academicYear: e.target.value })} placeholder="2025-2026" className={inputCls} />
              </Field>
              <Field label="Status *">
                <select value={form.status} onChange={(e) => setForm({ ...form, status: Number(e.target.value) })} required className={inputCls}>
                  <option value={1}>Upcoming</option>
                  <option value={2}>Open</option>
                  <option value={3}>Closed</option>
                </select>
              </Field>
              <Field label="Start date *">
                <input type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="End date *">
                <input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Registration start">
                <input type="date" value={form.registrationStart} onChange={(e) => setForm({ ...form, registrationStart: e.target.value })} className={inputCls} />
              </Field>
              <Field label="Registration end">
                <input type="date" value={form.registrationEnd} onChange={(e) => setForm({ ...form, registrationEnd: e.target.value })} className={inputCls} />
              </Field>
            </div>
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
        <Modal title={`Edit — ${editing.semesterName}`} onClose={() => setEditing(null)}>
          <form onSubmit={handleUpdate} className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Name *">
                <input value={editing.semesterName || ''} onChange={(e) => setEditing({ ...editing, semesterName: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Code *">
                <input value={editing.semesterCode || ''} onChange={(e) => setEditing({ ...editing, semesterCode: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Academic Year">
                <input value={editing.academicYear || ''} onChange={(e) => setEditing({ ...editing, academicYear: e.target.value })} className={inputCls} />
              </Field>
              <Field label="Status *">
                <select value={editing.status} onChange={(e) => setEditing({ ...editing, status: Number(e.target.value) })} required className={inputCls}>
                  <option value={1}>Upcoming</option>
                  <option value={2}>Open</option>
                  <option value={3}>Closed</option>
                </select>
              </Field>
              <Field label="Start date *">
                <input type="date" value={fmt(editing.startDate)} onChange={(e) => setEditing({ ...editing, startDate: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="End date *">
                <input type="date" value={fmt(editing.endDate)} onChange={(e) => setEditing({ ...editing, endDate: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Registration start">
                <input type="date" value={fmt(editing.registrationStart)} onChange={(e) => setEditing({ ...editing, registrationStart: e.target.value })} className={inputCls} />
              </Field>
              <Field label="Registration end">
                <input type="date" value={fmt(editing.registrationEnd)} onChange={(e) => setEditing({ ...editing, registrationEnd: e.target.value })} className={inputCls} />
              </Field>
            </div>
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