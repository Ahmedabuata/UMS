import { useCallback, useEffect, useMemo, useState } from 'react'
import { adminDepartmentsApi, branchesApi, employeesApi } from '../api/client'
import { Badge, Field, Modal, inputCls } from '../components/ui'
import { useAuth } from '../context/AuthContext'

const TABS = [
  { id: 'employees', label: 'Employees', isLive: true },
  { id: 'contracts', label: 'Contracts', isLive: true },
  { id: 'payroll', label: 'Payroll', isLive: false },
  { id: 'leaves', label: 'Leaves', isLive: true },
]

const CIRCLE_OPTIONS = ['All', 'Full-time', 'Part-time', 'Contract']
const STATUS_OPTIONS = ['All', 'Active', 'Inactive', 'On Leave']
const ACADEMIC_TITLES = ['All', 'محاضر', 'أستاذ مساعد', 'أستاذ مشارك', 'أستاذ دكتور']

function validationText(err) {
  const details = err?.data?.errors
  if (details && typeof details === 'object') {
    const entries = Object.values(details).flat().join(' ')
    return entries ? `${err.message || 'Validation failed.'} ${entries}` : err?.message || 'Request failed'
  }
  return err?.message || 'Request failed'
}

const isAcademic = (r) => !!r.academicTitle || !!r.specialization || !!r.facultyName

function todayISO() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

function addYearsISO(dateStr, years) {
  const d = dateStr ? new Date(`${dateStr}T00:00:00`) : new Date()
  if (isNaN(d.getTime())) return ''
  d.setFullYear(d.getFullYear() + years)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

function daysUntil(dateStr) {
  if (!dateStr) return null
  const target = new Date(`${dateStr}T00:00:00`)
  if (isNaN(target.getTime())) return null
  const today = new Date(`${todayISO()}T00:00:00`)
  return Math.round((target.getTime() - today.getTime()) / 86400000)
}

export default function HRModule() {
  const [activeTab, setActiveTab] = useState('employees')
  const [rows, setRows] = useState([])
  const [departments, setDepartments] = useState([])
  const [branches, setBranches] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [filters, setFilters] = useState({
    department: '', branch: '', contractType: '', status: '', academic: 'All', search: '',
  })
  const [contractTypeFilter, setContractTypeFilter] = useState('All')
  const [contractSearch, setContractSearch] = useState('')
  const [leaveFilter, setLeaveFilter] = useState('All')
  const [contractLoading, setContractLoading] = useState(false)
  const [detail, setDetail] = useState(null)
  const [showAdd, setShowAdd] = useState(false)
  const [adding, setAdding] = useState(false)
  const [previewNumber, setPreviewNumber] = useState('')
  const [form, setForm] = useState({
    fullName: '', email: '', phone: '', departmentId: '', branchId: '', contractType: 'Full-time', hireDate: '',
    category: 'Administrative', academicTitle: '',
  })
  const [editing, setEditing] = useState(null)
  const [editSaving, setEditSaving] = useState(false)

  const { user, permissions } = useAuth()
  const canDelete = user?.roleName === 'SUPER_ADMIN' || (permissions || []).includes('SECURITY_USER_WRITE')

  const setField = (k) => (e) => setForm((f) => ({ ...f, [k]: e.target.value }))

  useEffect(() => {
    if (!showAdd) return
    if (form.departmentId) {
      employeesApi.previewNumber(form.departmentId).then(setPreviewNumber).catch(() => setPreviewNumber(''))
    } else {
      setPreviewNumber('')
    }
  }, [form.departmentId, showAdd])

  const openAdd = () => {
    setForm({ fullName: '', email: '', phone: '', departmentId: '', branchId: '', contractType: 'Full-time', hireDate: '', category: 'Administrative', academicTitle: '' })
    setPreviewNumber('')
    setShowAdd(true)
  }

  const handleCreate = async () => {
    setAdding(true)
    setError('')
    try {
      const payload = {
        fullName: form.fullName,
        email: form.email,
        phone: form.phone || undefined,
        departmentId: form.departmentId,
        branchId: form.branchId || undefined,
        contractType: form.contractType || undefined,
        hireDate: form.hireDate || undefined,
        academicTitle: form.category !== 'Administrative' && form.academicTitle ? form.academicTitle : undefined,
      }
      const created = await employeesApi.create(payload)
      showNotice(`Employee ${created.employeeNumber} created.`)
      await load()
    } catch (err) {
      setError(validationText(err))
    } finally {
      setAdding(false)
    }
  }

  const showNotice = (msg) => { setNotice(msg); setTimeout(() => setNotice(''), 3000) }

  const load = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const params = {}
      if (filters.department) params.department = filters.department
      if (filters.branch) params.branch = filters.branch
      if (filters.contractType && filters.contractType !== 'All') params.contractType = filters.contractType
      if (filters.status && filters.status !== 'All') params.status = filters.status
      if (filters.search) params.search = filters.search

      let data
      if (filters.academic !== 'All') {
        data = await employeesApi.academicTitles(filters.academic)
      } else {
        data = await employeesApi.list(params)
      }
      setRows(data ?? [])
    } catch (err) {
      setError(validationText(err))
    } finally {
      setLoading(false)
    }
  }, [filters])

  const loadMeta = useCallback(async () => {
    try { setDepartments((await adminDepartmentsApi.list()) ?? []) } catch { setDepartments([]) }
    try { setBranches((await branchesApi.list()) ?? []) } catch { setBranches([]) }
  }, [])

  useEffect(() => { load() }, [load])
  useEffect(() => { loadMeta() }, [loadMeta])

  const stats = useMemo(() => {
    const total = rows.length
    const working = rows.filter((r) => r.status === 'Active' || r.status === 'On Leave').length
    const fullTime = rows.filter((r) => r.contractType === 'Full-time').length
    const academicStaff = rows.filter(isAcademic).length
    return { total, working, fullTime, academicStaff }
  }, [rows])

  const openDetail = async (id) => {
    setError('')
    setDetail({ loading: true })
    try {
      setDetail(await employeesApi.get(id))
    } catch (err) {
      setDetail(null)
      setError(err.message || 'Failed to load employee')
    }
  }

  const handleDelete = async (row) => {
    if (!window.confirm(`Delete (soft) employee "${row.fullName}"? This deactivates the record.`)) return
    setError('')
    try {
      await employeesApi.remove(row.id)
      showNotice('Employee deleted (inactive).')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to delete employee')
    }
  }

  const handleDeactivate = async (row) => {
    if (!window.confirm(`Deactivate employee "${row.fullName}"?`)) return
    setError('')
    try {
      await employeesApi.deactivate(row.id)
      showNotice('Employee deactivated.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to deactivate employee')
    }
  }

  const handleActivate = async (row) => {
    setError('')
    try {
      await employeesApi.activate(row.id)
      showNotice('Employee activated.')
      await load()
    } catch (err) {
      setError(err.message || 'Failed to activate employee')
    }
  }

  const openEdit = (row) => {
    setError('')
    setEditing({
      id: row.id,
      employeeNumber: row.employeeNumber,
      fullName: row.fullName || '',
      email: row.email || '',
      phone: row.phone || '',
      departmentId: row.departmentId || '',
      branchId: row.branchId || '',
      contractType: row.contractType || 'Full-time',
      status: row.status || 'Active',
      academicTitle: row.academicTitle || '',
      category: row.category || '',
    })
  }

  const handleEditSave = async () => {
    if (!editing) return
    setEditSaving(true)
    setError('')
    try {
      await employeesApi.update(editing.id, {
        fullName: editing.fullName,
        email: editing.email,
        phone: editing.phone || undefined,
        departmentId: editing.departmentId || undefined,
        branchId: editing.branchId || undefined,
        contractType: editing.contractType || undefined,
        status: editing.status || undefined,
        academicTitle: editing.academicTitle || undefined,
      })
      setEditing(null)
      showNotice('Employee updated.')
      await load()
    } catch (err) {
      setError(validationText(err))
    } finally {
      setEditSaving(false)
    }
  }

  const renderEmployees = () => (
    <div>
      <div className="mb-6 grid grid-cols-2 md:grid-cols-3 xl:grid-cols-6 gap-3">
        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">Department</label>
          <select value={filters.department} onChange={(e) => setFilters((f) => ({ ...f, department: e.target.value }))} className={inputCls}>
            <option value="">All</option>
            {[...departments].sort((a, b) => (a.isActive === b.isActive ? 0 : a.isActive ? -1 : 1)).map((d) => (
              <option key={d.id} value={d.departmentCode}>{d.departmentName} ({d.departmentCode})</option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">Branch</label>
          <select value={filters.branch} onChange={(e) => setFilters((f) => ({ ...f, branch: e.target.value }))} className={inputCls}>
            <option value="">All</option>
            {branches.map((b) => <option key={b.id} value={b.branchName}>{b.branchName}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">Contract Type</label>
          <select value={filters.contractType} onChange={(e) => setFilters((f) => ({ ...f, contractType: e.target.value }))} className={inputCls}>
            {CIRCLE_OPTIONS.map((o) => <option key={o} value={o}>{o}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">Status</label>
          <select value={filters.status} onChange={(e) => setFilters((f) => ({ ...f, status: e.target.value }))} className={inputCls}>
            {STATUS_OPTIONS.map((o) => <option key={o} value={o}>{o}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">Academic Title</label>
          <select value={filters.academic} onChange={(e) => setFilters((f) => ({ ...f, academic: e.target.value }))} className={inputCls}>
            {ACADEMIC_TITLES.map((o) => <option key={o} value={o}>{o}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-500 mb-1">Search</label>
          <input
            value={filters.search}
            onChange={(e) => setFilters((f) => ({ ...f, search: e.target.value }))}
            placeholder="Name, number, email, phone"
            className={inputCls}
          />
        </div>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <p className="p-6 text-gray-500">Loading employees...</p>
        ) : rows.length === 0 ? (
          <p className="p-6 text-gray-500">No employees match the current filters.</p>
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Employee #</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Name</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Department</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Branch</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Contract</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Category</th>
                <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {rows.map((row) => (
                <tr key={row.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-900">{row.employeeNumber}</td>
                  <td className="px-6 py-4 text-sm font-semibold text-gray-900">{row.fullName}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{row.departmentName || '—'}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{row.branchName || '—'}</td>
                  <td className="px-6 py-4 text-sm text-gray-500">{row.contractType || '—'}</td>
                  <td className="px-6 py-4">
                    {row.isActive
                      ? <Badge tone={row.status === 'On Leave' ? 'amber' : 'green'}>{row.status || 'Active'}</Badge>
                      : <Badge tone="red">Inactive</Badge>}
                  </td>
                  <td className="px-6 py-4">
                    {row.category
                      ? <Badge tone={row.category === 'Both' ? 'amber' : row.category === 'Academic' ? 'indigo' : 'green'}>{row.category}</Badge>
                      : <span className="text-sm text-gray-300">—</span>}
                  </td>
                  <td className="px-6 py-4 text-right whitespace-nowrap">
                    <button onClick={() => openDetail(row.id)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">
                      View
                    </button>
                    <button onClick={() => openEdit(row)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">
                      Edit
                    </button>
                    {user?.id && row.id === user.id ? (
                      <span className="inline-flex"><Badge tone="red">Protected</Badge></span>
                    ) : canDelete ? (
                      <>
                        {row.isActive ? (
                          <button onClick={() => handleDeactivate(row)} className="text-amber-600 hover:text-amber-800 text-sm font-medium mr-3">
                            Deactivate
                          </button>
                        ) : (
                          <button onClick={() => handleActivate(row)} className="text-green-600 hover:text-green-800 text-sm font-medium mr-3">
                            Activate
                          </button>
                        )}
                        {row.category !== 'Academic' && row.category !== 'Both' && (
                          <button onClick={() => handleDelete(row)} className="text-red-600 hover:text-red-800 text-sm font-medium">
                            Delete
                          </button>
                        )}
                      </>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )

  const renderContracts = () => {
    const q = contractSearch.trim().toLowerCase()
    const filtered = rows.filter((r) => {
      const typeOk = contractTypeFilter === 'All' || (r.contractType || '') === contractTypeFilter
      const searchOk = !q || `${r.employeeNumber} ${r.fullName} ${r.email}`.toLowerCase().includes(q)
      return typeOk && searchOk
    })
    return (
      <div>
        <div className="mb-6 flex flex-wrap items-end gap-3">
          <div className="min-w-[200px]">
            <label className="block text-xs font-medium text-gray-500 mb-1">Contract Type</label>
            <select value={contractTypeFilter} onChange={(e) => setContractTypeFilter(e.target.value)} className={inputCls}>
              {CIRCLE_OPTIONS.map((o) => <option key={o} value={o}>{o}</option>)}
            </select>
          </div>
          <div className="flex-1 min-w-[220px]">
            <label className="block text-xs font-medium text-gray-500 mb-1">Search</label>
            <input value={contractSearch} onChange={(e) => setContractSearch(e.target.value)} placeholder="Name, number, email" className={inputCls} />
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          {filtered.length === 0 ? (
            <p className="p-6 text-gray-500">No contracts match the current filters.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Employee #</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Name</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Type</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Start (Hire)</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Expiry</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Days Left</th>
                    <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {filtered.map((row) => {
                    const expiry = addYearsISO(row.hireDate, 1)
                    const days = daysUntil(expiry)
                    const expired = days !== null && days <= 0
                    const expiring = !expired && days !== null && days <= 90
                    return (
                      <tr key={row.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 text-sm font-medium text-gray-900">{row.employeeNumber}</td>
                        <td className="px-6 py-4 text-sm font-semibold text-gray-900">{row.fullName}</td>
                        <td className="px-6 py-4"><Badge tone={row.contractType === 'Contract' ? 'amber' : row.contractType === 'Part-time' ? 'indigo' : 'green'}>{row.contractType || '—'}</Badge></td>
                        <td className="px-6 py-4 text-sm text-gray-500">{row.hireDate || '—'}</td>
                        <td className="px-6 py-4 text-sm text-gray-500">{expiry || '—'}</td>
                        <td className="px-6 py-4 text-sm">
                          {days === null ? <span className="text-gray-400">—</span> : expired
                            ? <Badge tone="red">Expired {Math.abs(days)}d ago</Badge>
                            : expiring ? <Badge tone="amber">Expiring {days}d</Badge> : <span className="text-gray-700">{days}d</span>}
                        </td>
                        <td className="px-6 py-4 text-right whitespace-nowrap">
                          <button
                            onClick={() => window.confirm(`Renew contract for ${row.fullName} by 1 year (reset hire date)?`) && handleRenewContract(row)}
                            className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3"
                          >
                            Renew (1 yr)
                          </button>
                          <button onClick={() => openEdit(row)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium">
                            Change Type
                          </button>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    )
  }

  const renderPayroll = () => (
    <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-10 text-center">
      <p className="text-sm text-gray-400 mb-2">Not yet enabled</p>
      <h3 className="text-2xl font-bold text-gray-900">Payroll</h3>
      <p className="mt-2 text-gray-500 max-w-xl mx-auto">{stats.total} employee records exist, but payroll processing requires separate salary structures and approval.</p>
      <span className="inline-flex mt-6 items-center gap-2 rounded-full bg-indigo-100 text-indigo-700 px-4 py-1.5 text-sm font-semibold">
        <span className="h-2 w-2 rounded-full bg-indigo-500" /> TBD — Awaiting Payroll Module Approval
      </span>
    </div>
  )

  const renderLeaves = () => {
    const q = filters.search.trim().toLowerCase()
    const base = rows.filter((r) => !q || `${r.fullName} ${r.employeeNumber}`.toLowerCase().includes(q))
    const onLeave = base.filter((r) => r.isActive && r.status === 'On Leave')
    const filtered = leaveFilter === 'All' ? base : leaveFilter === 'On Leave' ? onLeave : base.filter((r) => (leaveFilter === 'Inactive' ? !r.isActive : r.isActive && r.status === leaveFilter))
    return (
      <div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
          <StatCard label="On Leave" value={onLeave.length} />
          <StatCard label="Active" value={rows.filter((r) => r.isActive && r.status !== 'On Leave').length} />
          <StatCard label="Inactive" value={rows.filter((r) => !r.isActive).length} />
        </div>
        <div className="mb-4 flex flex-wrap items-end gap-3">
          <div className="min-w-[200px]">
            <label className="block text-xs font-medium text-gray-500 mb-1">Leave Status</label>
            <select value={leaveFilter} onChange={(e) => setLeaveFilter(e.target.value)} className={inputCls}>
              {['All', 'Active', 'On Leave', 'Inactive'].map((o) => <option key={o} value={o}>{o}</option>)}
            </select>
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          {filtered.length === 0 ? (
            <p className="p-6 text-gray-500">No employees match the current filters.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Employee #</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Name</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Department</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                    <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {filtered.map((row) => (
                    <tr key={row.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4 text-sm font-medium text-gray-900">{row.employeeNumber}</td>
                      <td className="px-6 py-4 text-sm font-semibold text-gray-900">{row.fullName}</td>
                      <td className="px-6 py-4 text-sm text-gray-500">{row.departmentName || '—'}</td>
                      <td className="px-6 py-4">
                        {row.isActive
                          ? <Badge tone={row.status === 'On Leave' ? 'amber' : 'green'}>{row.status || 'Active'}</Badge>
                          : <Badge tone="red">Inactive</Badge>}
                      </td>
                      <td className="px-6 py-4 text-right whitespace-nowrap">
                        {row.isActive && row.status === 'On Leave' ? (
                          <button
                            onClick={() => handleReturnFromLeave(row)}
                            className="text-indigo-600 hover:text-indigo-800 text-sm font-medium"
                          >
                            Return from Leave
                          </button>
                        ) : <span className="text-sm text-gray-300">—</span>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    )
  }

  const handleRenewContract = async (row) => {
    if (contractLoading) return
    setContractLoading(true)
    setError('')
    try {
      await employeesApi.update(row.id, { hireDate: todayISO() })
      showNotice(`Contract renewed for ${row.fullName}.`)
      await load()
    } catch (err) {
      setError(err.message || 'Failed to renew contract')
    } finally {
      setContractLoading(false)
    }
  }

  const handleReturnFromLeave = async (row) => {
    setError('')
    try {
      await employeesApi.update(row.id, { status: 'Active', isActive: true })
      showNotice(`${row.fullName} returned from leave.`)
      await load()
    } catch (err) {
      setError(err.message || 'Failed to update leave status')
    }
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">HR Management</h1>
        <p className="text-gray-500 text-sm mt-1">Human Resources module. Employees, Contracts and Leaves are functional against real employee data. Payroll is pending module approval.</p>
      </div>

      <div className="mb-6 flex items-center justify-between">
        <div />
        <button onClick={openAdd} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
          + Add Employee
        </button>
      </div>

      <div className="mb-6 inline-flex rounded-lg bg-gray-100 p-1">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`px-5 py-2 text-sm font-semibold rounded-md transition ${
              activeTab === tab.id ? 'bg-white text-indigo-700 shadow' : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            {tab.label}
            {!tab.isLive && <span className="ml-2 text-[10px] font-bold text-amber-600">SOON</span>}
          </button>
        ))}
      </div>

      {notice && <div className="mb-4 rounded-lg bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm">{notice}</div>}
      {error && <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm whitespace-pre-wrap">{error}</div>}

      {activeTab === 'employees' && (
        <>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            <StatCard label="Total Employees" value={stats.total} />
            <StatCard label="Working (Active / On Leave)" value={stats.working} />
            <StatCard label="Full-time" value={stats.fullTime} />
            <StatCard label="Academic Staff (w/ Instructor)" value={stats.academicStaff} />
          </div>
          {renderEmployees()}
        </>
      )}

      {activeTab === 'contracts' && renderContracts()}
      {activeTab === 'payroll' && renderPayroll()}
      {activeTab === 'leaves' && renderLeaves()}

      {detail && detail.loading && <Modal title="Employee" onClose={() => setDetail(null)} wide><p className="text-gray-500 py-4">Loading employee...</p></Modal>}
      {detail && !detail.loading && (
        <Modal title={`Employee — ${detail.employeeNumber}`} onClose={() => setDetail(null)} wide>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-2">
              <SectionLabel title="Personal Information" />
              <DetailRow label="Full Name" value={detail.fullName} />
              <DetailRow label="Email" value={detail.email} />
              <DetailRow label="Phone" value={detail.phone} />
              <DetailRow label="Hire Date" value={detail.hireDate} />
            </div>
            <div className="space-y-2">
              <SectionLabel title="Work Information" />
              <DetailRow label="Employee Number" value={detail.employeeNumber} />
              <DetailRow label="Department" value={detail.departmentName} />
              <DetailRow label="Branch" value={detail.branchName} />
              <DetailRow label="Contract Type" value={detail.contractType} />
              <DetailRow label="Status" value={detail.status} />
              <DetailRow label="Category" value={detail.category} />
            </div>
            <div className="space-y-2">
              <SectionLabel title="Academic Title (via Instructor)" />
              <DetailRow label="Academic Title" value={detail.academicTitle} />
              <DetailRow label="Specialization" value={detail.specialization} />
              <DetailRow label="Faculty" value={detail.facultyName} />
            </div>
            <div className="space-y-2">
              <SectionLabel title="Linked User Account" />
              <DetailRow label="Username" value={detail.username} />
              <DetailRow label="User Status" value={detail.userIsActive ? 'Active' : null} />
            </div>
          </div>
        </Modal>
      )}

      {showAdd && (
        <Modal title="Add Employee" onClose={() => setShowAdd(false)}>
          <div className="space-y-4">
            <Field label="Department">
              <select value={form.departmentId} onChange={setField('departmentId')} className={inputCls}>
                <option value="">Select department</option>
                {[...departments].sort((a, b) => (a.isActive === b.isActive ? 0 : a.isActive ? -1 : 1)).map((d) => (
                  <option key={d.id} value={d.id}>{d.departmentName} ({d.departmentCode})</option>
                ))}
              </select>
            </Field>
            <Field label="Employee Number (auto-generated)">
              <input value={previewNumber || '— pick a department —'} readOnly className={`${inputCls} bg-gray-50 text-gray-700`} />
            </Field>
            <Field label="Full Name">
              <input value={form.fullName} onChange={setField('fullName')} placeholder="e.g. Sara Ahmed" className={inputCls} />
            </Field>
            <Field label="Email">
              <input value={form.email} onChange={setField('email')} type="email" placeholder="name@uni.edu" className={inputCls} />
            </Field>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Field label="Phone">
                <input value={form.phone} onChange={setField('phone')} placeholder="0555 000 000" className={inputCls} />
              </Field>
              <Field label="Branch">
                <select value={form.branchId} onChange={setField('branchId')} className={inputCls}>
                  <option value="">Select branch</option>
                  {branches.map((b) => <option key={b.id} value={b.id}>{b.branchName}</option>)}
                </select>
              </Field>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Field label="Contract Type">
                <select value={form.contractType} onChange={setField('contractType')} className={inputCls}>
                  <option>Full-time</option>
                  <option>Part-time</option>
                  <option>Contract</option>
                </select>
              </Field>
              <Field label="Hire Date">
                <input value={form.hireDate} onChange={setField('hireDate')} type="date" className={inputCls} />
              </Field>
            </div>
            <Field label="Category">
              <div className="flex gap-4 text-sm font-medium text-gray-700">
                {['Administrative', 'Academic', 'Both'].map((c) => (
                  <label key={c} className="flex items-center gap-2 cursor-pointer">
                    <input type="radio" name="category" checked={form.category === c} onChange={setField('category')} value={c} className="h-4 w-4 text-indigo-600" />
                    {c}
                  </label>
                ))}
              </div>
            </Field>
            {form.category !== 'Administrative' && (
              <Field label="Academic Title">
                <select value={form.academicTitle} onChange={setField('academicTitle')} className={inputCls}>
                  <option value="">Select academic title</option>
                  <option>معيد</option>
                  <option>محاضر</option>
                  <option>أستاذ مساعد</option>
                  <option>أستاذ مشارك</option>
                  <option>أستاذ دكتور</option>
                </select>
              </Field>
            )}
            <p className="text-xs text-gray-400">HR creates employees only. Login accounts are created from the Security Manager.</p>
            <div className="flex justify-end gap-3 pt-2">
              <button onClick={() => setShowAdd(false)} className="rounded-lg px-4 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-100">
                Cancel
              </button>
              <button
                onClick={handleCreate}
                disabled={adding || !form.fullName || !form.email || !form.departmentId}
                className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
              >
                {adding ? 'Creating…' : 'Create Employee'}
              </button>
            </div>
          </div>
        </Modal>
      )}

      {editing && (
        <Modal title={`Edit — ${editing.fullName}`} onClose={() => setEditing(null)}>
          <div className="space-y-4">
            <Field label="Employee Number">
              <input value={editing.employeeNumber || ''} readOnly disabled className={`${inputCls} bg-gray-50 text-gray-700`} />
            </Field>
            {editing.category && (
              <div className="flex items-center gap-2 text-sm">
                <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">Category</span>
                <Badge tone={editing.category === 'Both' ? 'amber' : editing.category === 'Academic' ? 'indigo' : 'green'}>
                  {editing.category}
                </Badge>
              </div>
            )}
            <Field label="Administrative Department">
              <select value={editing.departmentId} onChange={(e) => setEditing({ ...editing, departmentId: e.target.value })} className={inputCls}>
                <option value="">Select department</option>
                {[...departments].sort((a, b) => (a.isActive === b.isActive ? 0 : a.isActive ? -1 : 1)).map((d) => (
                  <option key={d.id} value={d.id}>{d.departmentName} ({d.departmentCode})</option>
                ))}
              </select>
            </Field>
            <Field label="Full Name">
              <input value={editing.fullName} onChange={(e) => setEditing({ ...editing, fullName: e.target.value })} className={inputCls} />
            </Field>
            <Field label="Email">
              <input value={editing.email} onChange={(e) => setEditing({ ...editing, email: e.target.value })} type="email" className={inputCls} />
            </Field>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Field label="Phone">
                <input value={editing.phone} onChange={(e) => setEditing({ ...editing, phone: e.target.value })} className={inputCls} />
              </Field>
              <Field label="Branch">
                <select value={editing.branchId} onChange={(e) => setEditing({ ...editing, branchId: e.target.value })} className={inputCls}>
                  <option value="">Select branch</option>
                  {branches.map((b) => <option key={b.id} value={b.id}>{b.branchName}</option>)}
                </select>
              </Field>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Field label="Contract Type">
                <select value={editing.contractType} onChange={(e) => setEditing({ ...editing, contractType: e.target.value })} className={inputCls}>
                  <option>Full-time</option>
                  <option>Part-time</option>
                  <option>Contract</option>
                </select>
              </Field>
              <Field label="Status">
                <select value={editing.status} onChange={(e) => setEditing({ ...editing, status: e.target.value })} className={inputCls}>
                  <option>Active</option>
                  <option>Inactive</option>
                  <option>On Leave</option>
                </select>
              </Field>
            </div>
            <Field label="Academic Title (only if academic)">
              <select value={editing.academicTitle} onChange={(e) => setEditing({ ...editing, academicTitle: e.target.value })} className={inputCls}>
                <option value="">—</option>
                <option>معيد</option>
                <option>محاضر</option>
                <option>أستاذ مساعد</option>
                <option>أستاذ مشارك</option>
                <option>أستاذ دكتور</option>
              </select>
            </Field>
            <div className="flex justify-end gap-3 pt-2">
              <button onClick={() => setEditing(null)} className="rounded-lg px-4 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-100">
                Cancel
              </button>
              <button
                onClick={handleEditSave}
                disabled={editSaving}
                className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
              >
                {editSaving ? 'Saving…' : 'Save'}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  )
}

function SectionLabel({ title }) {
  return <h3 className="text-sm font-bold text-indigo-700 uppercase tracking-wide">{title}</h3>
}

function DetailRow({ label, value }) {
  return (
    <div className="pt-1">
      <dt className="text-xs font-medium text-gray-500 uppercase tracking-wide">{label}</dt>
      <dd className="mt-0.5 text-sm text-gray-900">{value || '—'}</dd>
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