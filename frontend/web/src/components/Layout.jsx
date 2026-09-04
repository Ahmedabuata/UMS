import {
  Building2,
  Boxes,
  CalendarRange,
  ChevronDown,
  FileClock,
  Flag,
  GraduationCap,
  LockKeyhole,
  Network,
  School,
  Shield,
  ShieldCheck,
  UserRound,
  Users,
  Warehouse,
} from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const groups = [
  {
    title: 'Security Manager',
    icon: Shield,
    expandedByDefault: true,
    items: [
      { to: '/dashboard', label: 'Dashboard', icon: School, perms: null },
      { to: '/users', label: 'Users', icon: Users, perms: ['USER_READ'] },
      { to: '/roles', label: 'Roles', icon: ShieldCheck, perms: ['USER_READ'] },
      { to: '/groups', label: 'Groups', icon: Network, perms: ['USER_READ'] },
      { to: '/permissions', label: 'Permissions', icon: ShieldCheck, perms: ['USER_READ'] },
      { to: '/audit-logs', label: 'Audit Logs', icon: FileClock, perms: ['USER_READ'] },
      { to: '/security-policies', label: 'Security Policies', icon: LockKeyhole, perms: ['USER_READ'] },
    ],
  },
  {
    title: 'Infrastructure Management',
    icon: Building2,
    expandedByDefault: false,
    items: [
      { to: '/branches', label: 'Branches', icon: Building2, perms: null },
      { to: '/modules', label: 'Modules', icon: Boxes, perms: null },
      { to: '/administrative-departments', label: 'Administrative Departments', icon: Building2, perms: null },
      { to: '/hr', label: 'HR Management', icon: UserRound, perms: null },
    ],
  },
  {
    title: 'Core Data Management',
    icon: GraduationCap,
    expandedByDefault: true,
    items: [
      { to: '/faculties', label: 'Faculties & Departments', icon: Flag, perms: null },
      { to: '/buildings', label: 'Classrooms & Buildings', icon: Warehouse, perms: null },
      { to: '/semesters', label: 'Semesters', icon: CalendarRange, perms: null },
    ],
  },
]

export default function Layout() {
  const { user, permissions, logout } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState({})

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  const canSee = (item) => {
    if (!item.perms) return true
    return item.perms.some((p) => permissions.includes(p))
  }

  return (
    <div className="min-h-screen flex bg-gray-100">
      <aside className="w-64 bg-gray-900 text-gray-300 flex flex-col shrink-0">
        <div className="px-6 py-6">
          <h1 className="text-xl font-bold text-white">UMS</h1>
          <p className="text-xs text-gray-400 mt-1">University Management System</p>
        </div>
        <nav className="flex-1 px-3 space-y-1 overflow-y-auto">
          {groups.map((group) => {
            const isOpen = open[group.title] ?? group.expandedByDefault
            const items = group.items.filter(canSee)
            if (items.length === 0) return null
            const GroupIcon = group.icon
            return (
              <div key={group.title} className="mb-1">
                <button
                  onClick={() => setOpen((o) => ({ ...o, [group.title]: !isOpen }))}
                  className="w-full flex items-center justify-between rounded-lg px-3 py-2.5 text-sm font-bold text-gray-100 hover:bg-gray-800 transition"
                >
                  <span className="flex items-center gap-2">
                    <GroupIcon size={17} />
                    {group.title}
                  </span>
                  <ChevronDown size={16} className={`transition-transform ${isOpen ? '' : '-rotate-90'}`} />
                </button>
                {isOpen && (
                  <div className="mt-1 space-y-0.5 pl-2">
                    {items.map((item) => {
                      const Icon = item.icon
                      return (
                        <NavLink
                          key={item.to}
                          to={item.to}
                          className={({ isActive }) =>
                            `flex items-center gap-3 rounded-lg px-4 py-2 text-sm font-medium transition ${
                              isActive
                                ? 'bg-indigo-600 text-white'
                                : 'text-gray-300 hover:bg-gray-800 hover:text-white'
                            }`
                          }
                        >
                          <Icon size={17} />
                          <span className="truncate">{item.label}</span>
                        </NavLink>
                      )
                    })}
                  </div>
                )}
              </div>
            )
          })}
        </nav>
        <div className="p-4 border-t border-gray-800">
          <p className="text-sm font-semibold text-white truncate">{user?.fullName || user?.username}</p>
          <p className="text-xs text-gray-400 truncate">{user?.email}</p>
          <button
            onClick={handleLogout}
            className="mt-3 w-full rounded-lg border border-gray-700 px-4 py-2 text-sm font-medium text-gray-300 hover:bg-gray-800"
          >
            Sign out
          </button>
        </div>
      </aside>

      <main className="flex-1 p-8">
        <Outlet />
      </main>
    </div>
  )
}