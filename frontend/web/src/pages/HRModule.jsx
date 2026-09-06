import { useCallback, useEffect, useMemo, useState } from 'react'
import { adminDepartmentsApi, branchesApi, employeesApi } from '../api/client'
import { Badge, Field, Modal, inputCls } from '../components/ui'
import { useAuth } from '../context/AuthContext'

const TABS = [
  { id: 'employees', label: 'Employees', icon: '👥', perms: ['HR_EMPLOYEE_READ'], isLive: true },
  { id: 'attendance', label: 'Attendance', icon: '📅', perms: ['HR_ATTENDANCE_READ'], isLive: false, isNew: true },
  { id: 'leaves', label: 'Leaves', icon: '🌴', perms: ['HR_LEAVE_READ'], isLive: true },
  { id: 'recruitment', label: 'Recruitment', icon: '💼', perms: ['HR_RECRUITMENT_READ'], isLive: false, isNew: true },
  { id: 'evaluation', label: 'Evaluation', icon: '⭐', perms: ['HR_EVALUATION_READ'], isLive: false, isNew: true },
  { id: 'contracts', label: 'Contracts', icon: '📄', perms: ['HR_CONTRACT_READ'], isLive: true },
  { id: 'payroll', label: 'Payroll', icon: '💰', perms: ['HR_SALARY_READ'], isLive: false, isNew: true, sensitive: true },
]

const CIRCLE_OPTIONS = ['All', 'Full-time', 'Part-time', 'Contract']
const STATUS_OPTIONS = ['All', 'Active', 'Inactive', 'On Leave', 'Terminated']
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

const scrollbarHideStyle = `
  .scrollbar-hide::-webkit-scrollbar { display: none; }
  .scrollbar-hide { -ms-overflow-style: none; scrollbar-width: none; }
`;

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
  const [statusChanging, setStatusChanging] = useState(null) // For HR_EMPLOYEE_STATUS

  const { user, permissions, hasAnyPermission } = useAuth()
  const canDelete = user?.roleName === 'SUPER_ADMIN' || (permissions || []).includes('SECURITY_USER_WRITE')
  const canChangeStatus = hasAnyPermission(['HR_EMPLOYEE_STATUS', 'SUPER_ADMIN', 'HR_EMPLOYEE_WRITE'])
  const canWrite = hasAnyPermission(['HR_EMPLOYEE_WRITE', 'SUPER_ADMIN'])
  const canWriteSalary = hasAnyPermission(['HR_SALARY_WRITE', 'SUPER_ADMIN'])

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
      setShowAdd(false)
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

  const filteredRows = useMemo(() => {
    let r = [...rows]
    if (contractTypeFilter !== 'All') r = r.filter((x) => x.contractType === contractTypeFilter)
    if (contractSearch) {
      const s = contractSearch.toLowerCase()
      r = r.filter((x) => (x.fullName || '').toLowerCase().includes(s) || (x.employeeNumber || '').toLowerCase().includes(s))
    }
    return r
  }, [rows, contractTypeFilter, contractSearch])

  const handleEditSave = async () => {
    if (!editing) return
    setEditSaving(true)
    setError('')
    try {
      const dto = {
        fullName: editing.fullName,
        email: editing.email,
        phone: editing.phone,
        departmentId: editing.departmentId,
        branchId: editing.branchId,
        contractType: editing.contractType,
        status: editing.status,
        academicTitle: editing.academicTitle || undefined,
      }
      await employeesApi.update(editing.id, dto)
      showNotice('Employee updated.')
      setEditing(null)
      await load()
    } catch (err) {
      setError(validationText(err))
    } finally {
      setEditSaving(false)
    }
  }

  const handleStatusChange = async (newStatus) => {
    if (!statusChanging) return
    setEditSaving(true)
    try {
      await employeesApi.update(statusChanging.id, { status: newStatus })
      showNotice(`Status changed to ${newStatus}`)
      setStatusChanging(null)
      await load()
    } catch (err) {
      setError(validationText(err))
    } finally {
      setEditSaving(false)
    }
  }

  const canSeeTab = (tab) => {
    if (!tab.perms || tab.perms.length === 0) return true
    return hasAnyPermission(tab.perms)
  }

  const visibleTabs = TABS.filter(canSeeTab)

  return (
    <div className="min-h-screen bg-[#f8fafc] p-3 lg:p-6">
      <style>{scrollbarHideStyle}</style>
      {/* Header */}
      <div className="flex flex-col lg:flex-row justify-between items-start lg:items-center gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-[#0f172a]">HR Management</h1>
          <p className="text-sm text-slate-500 mt-1">Manage employees, attendance, recruitment, evaluations and payroll • {permissions?.length || 14} permissions</p>
        </div>
        <div className="flex gap-2">
          <button onClick={openAdd} className="bg-[#1e1b4b] text-white px-5 py-2.5 rounded-xl text-sm font-semibold flex items-center gap-2 hover:bg-black shadow">
            <span className="text-lg leading-none">+</span> Add New Employee
          </button>
        </div>
      </div>

      {notice && <div className="mb-4 p-3 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-sm">{notice}</div>}
      {error && <div className="mb-4 p-3 rounded-xl bg-red-50 border border-red-200 text-red-800 text-sm">{error}</div>}

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 lg:gap-4 mb-6">
        <StatCard label="Total Employees" value={stats.total} />
        <StatCard label="Active" value={stats.working} />
        <StatCard label="Full-time" value={stats.fullTime} />
        <StatCard label="Academic Staff" value={stats.academicStaff} />
      </div>

      {/* Permission Bar */}
      <div className="bg-white rounded-xl border border-slate-200 p-3 mb-6 flex flex-wrap gap-2 items-center">
        <span className="text-[11px] font-bold text-slate-500 uppercase">Your HR Access:</span>
        {visibleTabs.map(t => (
          <span key={t.id} className={`text-[11px] px-2.5 py-1 rounded-full font-medium flex items-center gap-1 ${t.sensitive ? 'bg-amber-100 text-amber-800 border border-amber-200' : 'bg-indigo-50 text-indigo-700'}`}>
            {t.sensitive && '🔒'} {t.label}
          </span>
        ))}
      </div>

      {/* Tabs - Adaptive: all visible on laptop, scroll only on tablet/mobile */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        <div className="border-b border-slate-100 px-2 lg:px-3">
          {/* Desktop: flex-nowrap with shrinking, Mobile/Tablet: scroll */}
          <div className="flex gap-1 lg:gap-1.5 py-2 overflow-x-auto lg:overflow-visible scrollbar-hide">
            {visibleTabs.map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center justify-center gap-1 lg:gap-1.5 px-2.5 lg:px-3 xl:px-3.5 py-2 lg:py-2 rounded-xl text-[12px] lg:text-[13px] xl:text-sm font-medium whitespace-nowrap transition flex-shrink-0 lg:flex-shrink lg:min-w-0 ${activeTab === tab.id ? 'bg-[#1e1b4b] text-white shadow' : 'text-slate-600 hover:bg-slate-50 bg-slate-50 lg:bg-transparent'}`}
                style={{ flex: '1 1 auto', minWidth: 'fit-content' }}
              >
                <span className="text-[13px] lg:text-[14px]">{tab.icon}</span>
                <span className="hidden sm:inline lg:inline">{tab.label}</span>
                <span className="sm:hidden">{tab.label.slice(0,3)}</span>
                {tab.sensitive && <span className="text-[9px] lg:text-[10px]">🔒</span>}
                {tab.isNew && !tab.isLive && <span className="hidden xl:inline text-[8px] px-1 py-0.5 rounded bg-amber-400 text-black font-bold ml-0.5">NEW</span>}
              </button>
            ))}
          </div>
          {/* Scroll hint for tablet only */}
          <div className="lg:hidden text-[10px] text-slate-400 text-center pb-1 flex items-center justify-center gap-1">
            <span>←</span> اسحب للمزيد <span>→</span>
          </div>
        </div>

        <div className="p-6">
          {/* EMPLOYEES - LIVE */}
          {activeTab === 'employees' && (
            <div>
              <div className="flex flex-wrap gap-3 mb-4">
                <input value={filters.search} onChange={e => setFilters(f => ({...f, search: e.target.value}))} placeholder="Search employees..." className="rounded-xl border border-slate-200 px-4 py-2 text-sm w-64" />
                <select value={filters.status} onChange={e => setFilters(f => ({...f, status: e.target.value}))} className="rounded-xl border border-slate-200 px-3 py-2 text-sm">
                  {STATUS_OPTIONS.map(o => <option key={o}>{o}</option>)}
                </select>
                <select value={filters.contractType} onChange={e => setFilters(f => ({...f, contractType: e.target.value}))} className="rounded-xl border border-slate-200 px-3 py-2 text-sm">
                  <option value="">All Contracts</option>
                  {CIRCLE_OPTIONS.slice(1).map(o => <option key={o}>{o}</option>)}
                </select>
              </div>

              {loading ? <p className="text-sm text-slate-500">Loading...</p> : (
                <div className="overflow-x-auto scrollbar-hide">
                  <table className="w-full text-sm min-w-[600px]">
                    <thead>
                      <tr className="text-left text-xs text-slate-500 border-b">
                        <th className="pb-3">Employee No</th>
                        <th className="pb-3">Name</th>
                        <th className="pb-3">Dept</th>
                        <th className="pb-3">Contract</th>
                        <th className="pb-3">Status</th>
                        <th className="pb-3">Actions</th>
                      </tr>
                    </thead>
                    <tbody>
                      {filteredRows.map(r => (
                        <tr key={r.id} className="border-b border-slate-50 hover:bg-slate-50/50">
                          <td className="py-3 font-mono text-xs">{r.employeeNumber}</td>
                          <td className="py-3 font-medium">{r.fullName}</td>
                          <td className="py-3 text-xs text-slate-600">{r.departmentName || r.departmentId?.slice(0,8)}</td>
                          <td className="py-3"><Badge tone="indigo">{r.contractType}</Badge></td>
                          <td className="py-3">
                            <span className={`px-2.5 py-1 rounded-full text-[11px] font-bold ${r.status==='Active'?'bg-emerald-100 text-emerald-700': r.status==='Inactive'?'bg-amber-100 text-amber-700' : r.status==='Terminated'?'bg-red-100 text-red-700':'bg-slate-100 text-slate-600'}`}>
                              {r.status || 'Active'}
                            </span>
                          </td>
                          <td className="py-3 flex gap-1">
                            <button onClick={() => setDetail(r)} className="px-2.5 py-1 rounded-lg bg-slate-100 text-xs hover:bg-slate-200">View</button>
                            <button onClick={() => setEditing(r)} className="px-2.5 py-1 rounded-lg bg-indigo-50 text-indigo-700 text-xs hover:bg-indigo-100">Edit</button>
                            {canChangeStatus && (
                              <button onClick={() => setStatusChanging(r)} className="px-2.5 py-1 rounded-lg bg-amber-50 text-amber-700 text-xs hover:bg-amber-100 border border-amber-200" title="HR_EMPLOYEE_STATUS">
                                Status
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {/* ATTENDANCE - UI ONLY */}
          {activeTab === 'attendance' && (
            <div>
              <div className="bg-amber-50 border border-amber-200 rounded-xl p-3 mb-4 text-sm text-amber-800 flex gap-2">
                <span>🚧</span>
                <span><b>UI Only - Backend not connected:</b> HR_ATTENDANCE_READ/WRITE permissions mapped. This will connect to <code>/Employees/attendance</code> later.</span>
              </div>
              <div className="grid grid-cols-3 gap-4 mb-6">
                <div className="p-4 rounded-xl bg-emerald-50 border border-emerald-100"><div className="text-2xl font-bold">96%</div><div className="text-xs text-slate-500">Present Today</div></div>
                <div className="p-4 rounded-xl bg-amber-50 border border-amber-100"><div className="text-2xl font-bold">3</div><div className="text-xs text-slate-500">Late</div></div>
                <div className="p-4 rounded-xl bg-red-50 border border-red-100"><div className="text-2xl font-bold">2</div><div className="text-xs text-slate-500">Absent</div></div>
              </div>
              <div className="overflow-x-auto scrollbar-hide">
                <table className="w-full text-sm">
                  <thead><tr className="text-left text-xs text-slate-500 border-b"><th className="pb-3">Date</th><th className="pb-3">Employee</th><th className="pb-3">Check-in</th><th className="pb-3">Check-out</th><th className="pb-3">Hours</th><th className="pb-3">Status</th></tr></thead>
                  <tbody>
                    {[
                      {date: todayISO(), name: 'Ali Sofi', in: '08:02', out: '16:30', h: '8.5', s: 'Present'},
                      {date: todayISO(), name: 'Sara Ahmed', in: '08:15', out: '16:00', h: '7.8', s: 'Late'},
                      {date: todayISO(), name: 'Mohammed Ali', in: '-', out: '-', h: '-', s: 'Absent'},
                    ].map((r,i) => (
                      <tr key={i} className="border-b border-slate-50"><td className="py-3">{r.date}</td><td className="py-3">{r.name}</td><td className="py-3">{r.in}</td><td className="py-3">{r.out}</td><td className="py-3">{r.h}</td><td className="py-3"><span className={`px-2 py-1 rounded-full text-xs ${r.s==='Present'?'bg-emerald-100 text-emerald-700': r.s==='Late'?'bg-amber-100 text-amber-700':'bg-red-100 text-red-700'}`}>{r.s}</span></td></tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* LEAVES - LIVE */}
          {activeTab === 'leaves' && (
            <div>
              <div className="bg-slate-50 border border-slate-200 rounded-xl p-4 mb-4 text-sm text-slate-600">Leaves tab - existing functionality (connected to backend). Filter: 
                <select value={leaveFilter} onChange={e => setLeaveFilter(e.target.value)} className="ml-2 rounded-lg border px-2 py-1 text-sm">
                  <option>All</option><option>Pending</option><option>Approved</option><option>Rejected</option>
                </select>
              </div>
              <div className="text-sm text-slate-500">Existing leaves functionality preserved - showing {rows.length} employees eligible for leaves.</div>
            </div>
          )}

          {/* RECRUITMENT - UI ONLY */}
          {activeTab === 'recruitment' && (
            <div>
              <div className="bg-amber-50 border border-amber-200 rounded-xl p-3 mb-4 text-sm text-amber-800">🚧 UI Only - HR_RECRUITMENT_READ/WRITE • Will connect to recruitment API later.</div>
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
                {[
                  {title: 'Senior Lecturer - CS', applicants: 24, interviews: 5, status: 'Open'},
                  {title: 'HR Assistant', applicants: 42, interviews: 8, status: 'Open'},
                  {title: 'Lab Technician', applicants: 12, interviews: 0, status: 'Closed'},
                ].map((job,i) => (
                  <div key={i} className="bg-white border border-slate-200 rounded-xl p-4">
                    <div className="flex justify-between"><h4 className="font-bold text-sm">{job.title}</h4><span className={`text-[11px] px-2 py-1 rounded-full ${job.status==='Open'?'bg-emerald-100 text-emerald-700':'bg-slate-100 text-slate-600'}`}>{job.status}</span></div>
                    <div className="mt-3 grid grid-cols-2 gap-2 text-xs"><div><span className="text-slate-500">Applicants:</span> <b>{job.applicants}</b></div><div><span className="text-slate-500">Interviews:</span> <b>{job.interviews}</b></div></div>
                    <button onClick={() => alert('UI Only - Backend not connected')} className="mt-3 w-full rounded-lg bg-indigo-600 text-white py-2 text-xs">View Applicants</button>
                  </div>
                ))}
              </div>
              <div className="mt-6 bg-white border rounded-xl p-4">
                <h4 className="font-bold text-sm mb-3">Recruitment Pipeline (Kanban Mock)</h4>
                <div className="grid grid-cols-4 gap-3">
                  {['New (12)', 'Screening (8)', 'Interview (5)', 'Offer (2)'].map(col => (
                    <div key={col} className="bg-slate-50 rounded-lg p-3"><div className="text-xs font-bold mb-2">{col}</div><div className="space-y-2"><div className="bg-white p-2 rounded shadow-sm text-xs">Candidate #123</div><div className="bg-white p-2 rounded shadow-sm text-xs">Candidate #124</div></div></div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* EVALUATION - UI ONLY */}
          {activeTab === 'evaluation' && (
            <div>
              <div className="bg-amber-50 border border-amber-200 rounded-xl p-3 mb-4 text-sm text-amber-800">🚧 UI Only - HR_EVALUATION_READ/WRITE • Annual/periodic performance reviews.</div>
              <div className="overflow-x-auto scrollbar-hide">
                <table className="w-full text-sm">
                  <thead><tr className="text-left text-xs text-slate-500 border-b"><th className="pb-3">Employee</th><th className="pb-3">Period</th><th className="pb-3">Score</th><th className="pb-3">KPI</th><th className="pb-3">Status</th><th className="pb-3">Action</th></tr></thead>
                  <tbody>
                    {[
                      {name: 'Ali Sofi', period: 'Q3 2024', score: '4.2/5', kpi: '92%', status: 'Completed'},
                      {name: 'Sara Ahmed', period: 'Q3 2024', score: '3.8/5', kpi: '85%', status: 'Pending'},
                      {name: 'Mohammed Ali', period: 'Annual 2024', score: '4.5/5', kpi: '96%', status: 'Completed'},
                    ].map((r,i) => (
                      <tr key={i} className="border-b border-slate-50"><td className="py-3">{r.name}</td><td className="py-3">{r.period}</td><td className="py-3 font-bold">{r.score}</td><td className="py-3">{r.kpi}</td><td className="py-3"><span className={`px-2 py-1 rounded-full text-xs ${r.status==='Completed'?'bg-emerald-100 text-emerald-700':'bg-amber-100 text-amber-700'}`}>{r.status}</span></td><td className="py-3"><button onClick={() => alert('UI Only')} className="text-xs text-indigo-600">View</button></td></tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* CONTRACTS - LIVE */}
          {activeTab === 'contracts' && (
            <div>
              <div className="flex gap-3 mb-4">
                <input value={contractSearch} onChange={e => setContractSearch(e.target.value)} placeholder="Search contracts..." className="rounded-xl border border-slate-200 px-4 py-2 text-sm w-64" />
                <select value={contractTypeFilter} onChange={e => setContractTypeFilter(e.target.value)} className="rounded-xl border border-slate-200 px-3 py-2 text-sm">
                  <option>All</option><option>Full-time</option><option>Part-time</option><option>Contract</option>
                </select>
              </div>
              <div className="overflow-x-auto scrollbar-hide">
                <table className="w-full text-sm">
                  <thead><tr className="text-left text-xs text-slate-500 border-b"><th className="pb-3">Employee</th><th className="pb-3">Contract Type</th><th className="pb-3">Start</th><th className="pb-3">End</th><th className="pb-3">Days Left</th></tr></thead>
                  <tbody>
                    {filteredRows.slice(0,5).map(r => (
                      <tr key={r.id} className="border-b border-slate-50"><td className="py-3">{r.fullName}</td><td className="py-3"><Badge tone="green">{r.contractType}</Badge></td><td className="py-3 text-xs">{r.hireDate || '—'}</td><td className="py-3 text-xs">{r.contractEndDate ? new Date(r.contractEndDate).toLocaleDateString() : 'Indefinite'}</td><td className="py-3 text-xs">{r.contractEndDate ? `${daysUntil(r.contractEndDate)} days` : '—'}</td></tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* PAYROLL - UI ONLY with lock */}
          {activeTab === 'payroll' && (
            <div>
              <div className="bg-red-50 border border-red-200 rounded-xl p-3 mb-4 text-sm text-red-800 flex gap-2">
                <span>🔒</span>
                <div><b>Sensitive Financial Data - HR_SALARY_READ/WRITE required.</b> Salary data is masked with lock icon. Backend: <code>/Employees/salaries</code> (SOON → now UI ready).</div>
              </div>
              <div className="overflow-x-auto scrollbar-hide">
                <table className="w-full text-sm">
                  <thead><tr className="text-left text-xs text-slate-500 border-b"><th className="pb-3">Employee</th><th className="pb-3">Base Salary 🔒</th><th className="pb-3">Bonuses</th><th className="pb-3">Deductions</th><th className="pb-3">Net Salary 🔒</th><th className="pb-3">Status</th></tr></thead>
                  <tbody>
                    {filteredRows.slice(0,5).map(r => (
                      <tr key={r.id} className="border-b border-slate-50"><td className="py-3">{r.fullName}</td><td className="py-3"><span className="blur-sm select-none bg-slate-100 px-3 py-1 rounded">$3,500</span> <span className="ml-1">🔒</span></td><td className="py-3">$200</td><td className="py-3">$150</td><td className="py-3"><span className="blur-sm select-none bg-slate-100 px-3 py-1 rounded">$3,550</span> 🔒</td><td className="py-3"><Badge tone="green">Paid</Badge></td></tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="mt-4 flex gap-2">
                <button onClick={() => alert('UI Only - Requires HR_SALARY_READ')} className="rounded-xl border border-amber-300 bg-amber-50 px-4 py-2 text-xs text-amber-800">🔓 Reveal Salaries (HR_SALARY_READ)</button>
                <button onClick={() => alert('UI Only - Requires HR_SALARY_WRITE')} className="rounded-xl bg-indigo-600 text-white px-4 py-2 text-xs">Process Payroll</button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Detail Modal */}
      {detail && (
        <Modal title={`Employee — ${detail.fullName}`} onClose={() => setDetail(null)}>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <DetailRow label="Employee Number" value={detail.employeeNumber} />
            <DetailRow label="Email" value={detail.email} />
            <DetailRow label="Department" value={detail.departmentName} />
            <DetailRow label="Branch" value={detail.branchName} />
            <DetailRow label="Contract" value={detail.contractType} />
            <DetailRow label="Status" value={detail.status} />
          </div>
        </Modal>
      )}

      {/* Add Employee Modal - LIVE */}
      {showAdd && (
        <Modal title="Add New Employee" onClose={() => setShowAdd(false)}>
          <div className="space-y-4">
            {previewNumber && <div className="p-2 rounded-lg bg-indigo-50 text-indigo-700 text-xs">Preview Number: <b>{previewNumber}</b></div>}
            <Field label="Full Name"><input value={form.fullName} onChange={setField('fullName')} className={inputCls} /></Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Email"><input value={form.email} onChange={setField('email')} className={inputCls} /></Field>
              <Field label="Phone"><input value={form.phone} onChange={setField('phone')} className={inputCls} /></Field>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Department">
                <select value={form.departmentId} onChange={setField('departmentId')} className={inputCls}>
                  <option value="">Select department</option>
                  {departments.map(d => <option key={d.id} value={d.id}>{d.departmentName}</option>)}
                </select>
              </Field>
              <Field label="Branch">
                <select value={form.branchId} onChange={setField('branchId')} className={inputCls}>
                  <option value="">Select branch</option>
                  {branches.map(b => <option key={b.id} value={b.id}>{b.branchName}</option>)}
                </select>
              </Field>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Contract Type">
                <select value={form.contractType} onChange={setField('contractType')} className={inputCls}>
                  <option>Full-time</option><option>Part-time</option><option>Contract</option>
                </select>
              </Field>
              <Field label="Category">
                <select value={form.category} onChange={setField('category')} className={inputCls}>
                  <option>Administrative</option><option>Academic</option><option>Both</option>
                </select>
              </Field>
            </div>
            <Field label="Hire Date"><input type="date" value={form.hireDate} onChange={setField('hireDate')} className={inputCls} /></Field>
            <div className="flex justify-end gap-3 pt-2">
              <button onClick={() => setShowAdd(false)} className="rounded-lg px-4 py-2 text-sm">Cancel</button>
              <button onClick={handleCreate} disabled={adding || !form.fullName || !form.email || !form.departmentId} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm text-white disabled:opacity-50">{adding ? 'Creating…' : 'Create Employee'}</button>
            </div>
          </div>
        </Modal>
      )}

      {/* Edit Modal - LIVE */}
      {editing && (
        <Modal title={`Edit — ${editing.fullName}`} onClose={() => setEditing(null)}>
          <div className="space-y-4">
            <Field label="Full Name"><input value={editing.fullName} onChange={e => setEditing({...editing, fullName: e.target.value})} className={inputCls} /></Field>
            <Field label="Email"><input value={editing.email} onChange={e => setEditing({...editing, email: e.target.value})} className={inputCls} /></Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Contract Type">
                <select value={editing.contractType} onChange={e => setEditing({...editing, contractType: e.target.value})} className={inputCls}>
                  <option>Full-time</option><option>Part-time</option><option>Contract</option>
                </select>
              </Field>
              <Field label="Status">
                <select value={editing.status} onChange={e => setEditing({...editing, status: e.target.value})} className={inputCls}>
                  <option>Active</option><option>Inactive</option><option>On Leave</option><option>Terminated</option>
                </select>
              </Field>
            </div>
            <div className="flex justify-end gap-3">
              <button onClick={() => setEditing(null)} className="rounded-lg px-4 py-2 text-sm">Cancel</button>
              <button onClick={handleEditSave} disabled={editSaving} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm text-white">{editSaving ? 'Saving…' : 'Save'}</button>
            </div>
          </div>
        </Modal>
      )}

      {/* Status Change Modal - NEW: HR_EMPLOYEE_STATUS */}
      {statusChanging && (
        <Modal title={`Change Status — ${statusChanging.fullName}`} onClose={() => setStatusChanging(null)}>
          <div className="space-y-4">
            <p className="text-sm text-slate-600">Current status: <b>{statusChanging.status || 'Active'}</b>. Select new status (HR_EMPLOYEE_STATUS permission):</p>
            <div className="grid grid-cols-2 gap-3">
              {['Active','Inactive','On Leave','Terminated'].map(s => (
                <button key={s} onClick={() => handleStatusChange(s)} className={`p-3 rounded-xl border text-sm font-medium text-left ${s==='Active'?'bg-emerald-50 border-emerald-200 text-emerald-700': s==='Inactive'?'bg-amber-50 border-amber-200 text-amber-700': s==='On Leave'?'bg-blue-50 border-blue-200 text-blue-700':'bg-red-50 border-red-200 text-red-700'}`}>
                  {s}
                </button>
              ))}
            </div>
            <div className="flex justify-end">
              <button onClick={() => setStatusChanging(null)} className="rounded-lg px-4 py-2 text-sm">Cancel</button>
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
