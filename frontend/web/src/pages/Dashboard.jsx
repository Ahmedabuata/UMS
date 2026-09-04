import { useState } from 'react'
import { useAuth } from '../context/AuthContext'
import AddUserAccountModal from '../components/AddUserAccountModal'

export default function Dashboard() {
  const { user, permissions } = useAuth()
  const [showAddAccount, setShowAddAccount] = useState(false)
  // user قد يكون { email, role, userId } أو منفصل، نتعامل مع الحالتين
  const email = user?.email || 'admin@ums.com'
  const role = user?.role || user?.roleName || 'ADMIN'
  const employeeNumber = user?.employeeNumber || user?.identifierNumber || '—'

  return (
    <div className="min-h-screen bg-[#f8fafc] p-6 lg:p-8">
      {/* Welcome Banner */}
      <div className="relative overflow-hidden rounded-[20px] bg-gradient-to-br from-[#dbeafe] via-[#bfdbfe] to-[#4338ca] p-6 lg:p-8 mb-6">
        <div className="absolute top-0 right-0 w-64 h-64 bg-white/10 rounded-full blur-3xl -mr-20 -mt-20" />
        <div className="absolute bottom-0 right-20 w-40 h-40 bg-indigo-600/20 rounded-full blur-2xl" />
        <div className="relative flex flex-col lg:flex-row justify-between items-start lg:items-center gap-4">
          <div>
            <h1 className="text-2xl lg:text-3xl font-bold text-[#1e1b4b]">Welcome, {email}</h1>
            <p className="text-[#3730a3]/80 mt-1 text-sm">Here is your system overview for today — {new Date().toLocaleDateString('en-US', { weekday: 'long', day: 'numeric', month: 'short', year: 'numeric' })}</p>
          </div>
          <button
            onClick={() => setShowAddAccount(true)}
            className="bg-[#1e1b4b] text-white px-5 py-2.5 rounded-xl text-sm font-semibold flex items-center gap-2 hover:bg-black transition shadow-lg">
            <span className="text-lg leading-none">+</span> Add New User Account
          </button>
        </div>
      </div>

      {/* Top 3 Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mb-6">
        <div className="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <div className="flex items-center gap-3 mb-6">
            <div className="w-11 h-11 rounded-xl bg-gradient-to-br from-[#3b82f6] to-[#1e1b4b] flex items-center justify-center text-white">🛡️</div>
            <span className="text-sm text-slate-500 font-medium">Role</span>
          </div>
          <div className="text-3xl font-extrabold text-[#0f172a]">{role}</div>
          <div className="text-xs text-slate-500 mt-2">Super Administrator • Full privileges</div>
        </div>

        <div className="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <div className="flex items-center gap-3 mb-6">
            <div className="w-11 h-11 rounded-xl bg-gradient-to-br from-[#3b82f6] to-[#1e1b4b] flex items-center justify-center text-white">👤</div>
            <span className="text-sm text-slate-500 font-medium">Employee Number</span>
          </div>
          <div className="text-2xl font-extrabold text-[#0f172a] truncate">{employeeNumber}</div>
          <div className="text-xs text-slate-500 mt-2">{user?.fullName || '—'}</div>
        </div>

        <div className="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <div className="flex items-center gap-3 mb-4">
            <div className="w-11 h-11 rounded-xl bg-gradient-to-br from-[#3b82f6] to-[#1e1b4b] flex items-center justify-center text-white">🔑</div>
            <span className="text-sm text-slate-500 font-medium">Permissions</span>
          </div>
          <div className="text-3xl font-extrabold text-[#0f172a] mb-3">{permissions?.length || 11}</div>
          <div className="flex flex-wrap gap-1.5">
            {(permissions || []).slice(0,6).map(p => (
              <span key={p} className="text-[10px] px-2.5 py-1 rounded-full bg-[#e0e7ff] text-[#3730a3] font-medium">{p}</span>
            ))}
            {permissions?.length > 6 && <span className="text-[10px] px-2.5 py-1 rounded-full bg-slate-100 text-slate-600">+{permissions.length-6} more</span>}
          </div>
        </div>
      </div>

      {/* Permissions Full + Overview */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
        <div className="lg:col-span-2 bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <h3 className="font-bold text-[#0f172a] mb-4">Your Permissions</h3>
          <div className="flex flex-wrap gap-2">
            {(permissions || [
              'AUTH_LOGIN','ENROLLMENT_READ','ENROLLMENT_WRITE','FINANCE_READ','FINANCE_WRITE','GRADE_READ','GRADE_WRITE','STUDENT_READ','STUDENT_WRITE','USER_READ','USER_WRITE'
            ]).map(p => (
              <span key={p} className="px-3 py-1.5 rounded-full bg-[#eef2ff] text-[#4338ca] text-xs font-semibold border border-[#e0e7ff]">{p}</span>
            ))}
          </div>
          
          <div className="mt-8 grid grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="p-4 rounded-xl bg-slate-50 border border-slate-100">
              <div className="text-2xl font-bold">1,248</div>
              <div className="text-xs text-slate-500">Total Users</div>
              <div className="text-[11px] text-emerald-600 mt-1">+24 this month ↗</div>
            </div>
            <div className="p-4 rounded-xl bg-slate-50 border border-slate-100">
              <div className="text-2xl font-bold">12</div>
              <div className="text-xs text-slate-500">Faculties</div>
              <div className="text-[11px] text-emerald-600 mt-1">+1 new</div>
            </div>
            <div className="p-4 rounded-xl bg-slate-50 border border-slate-100">
              <div className="text-2xl font-bold">84</div>
              <div className="text-xs text-slate-500">Classrooms</div>
              <div className="text-[11px] text-amber-600 mt-1">• 92% occupied</div>
            </div>
            <div className="p-4 rounded-xl bg-slate-50 border border-slate-100">
              <div className="text-2xl font-bold">Fall 2024</div>
              <div className="text-xs text-slate-500">Current Semester</div>
              <div className="text-[11px] text-slate-500 mt-1">Ends 15 Dec 2024</div>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-2xl p-6 shadow-sm border border-slate-100">
          <h3 className="font-bold text-[#0f172a] mb-5">Recent Activity</h3>
          <div className="space-y-5">
            <div className="flex gap-3">
              <div className="w-2 h-2 rounded-full bg-blue-600 mt-2" />
              <div>
                <div className="text-sm font-medium text-slate-800">New faculty added: Engineering Dept</div>
                <div className="text-xs text-slate-500">2h ago</div>
              </div>
            </div>
            <div className="flex gap-3">
              <div className="w-2 h-2 rounded-full bg-emerald-500 mt-2" />
              <div>
                <div className="text-sm font-medium text-slate-800">User permissions updated for staff_012</div>
                <div className="text-xs text-slate-500">5h ago</div>
              </div>
            </div>
            <div className="flex gap-3">
              <div className="w-2 h-2 rounded-full bg-violet-500 mt-2" />
              <div>
                <div className="text-sm font-medium text-slate-800">Semester schedule published</div>
                <div className="text-xs text-slate-500">1d ago</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {showAddAccount && <AddUserAccountModal onClose={() => setShowAddAccount(false)} />}
    </div>
  )
}
