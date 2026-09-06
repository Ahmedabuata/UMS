import { ChevronDown } from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { NAVIGATION } from '../config/navigation'
import { useAuth } from '../context/AuthContext'

export default function Layout() {
  const { user, permissions, logout, hasAnyPermission, isSuperAdmin } = useAuth()
  const navigate = useNavigate()
  const [open, setOpen] = useState({})

  const canSee = (item) => {
    if (!item.perms || item.perms.length === 0) return true
    return hasAnyPermission(item.perms)
  }

  const canSeeGroup = (group) => {
    if (isSuperAdmin()) return true
    if (!group.requiredPermissions || group.requiredPermissions.length === 0) {
      return group.items.some(canSee)
    }
    return hasAnyPermission(group.requiredPermissions) && group.items.some(canSee)
  }

  const visibleGroups = NAVIGATION.filter(canSeeGroup)

  return (
    <div className="min-h-screen flex bg-gray-100">
      <aside className="w-64 bg-gray-900 text-gray-300 flex flex-col shrink-0">
        <div className="px-6 py-6">
          <h1 className="text-xl font-bold text-white">UMS</h1>
          <p className="text-xs text-gray-400 mt-1">University Management System</p>
          <div className="mt-3 px-2 py-1.5 rounded-lg bg-gray-800/60 border border-gray-700/50">
            <p className="text-[11px] font-bold text-emerald-400 truncate">{user?.roleName || user?.role || 'HR_MANAGER'}</p>
            <p className="text-[10px] text-gray-400">{permissions?.length || 0} permissions</p>
          </div>
        </div>

        <nav className="flex-1 px-3 space-y-4 overflow-y-auto pb-4">
          {visibleGroups.map((group) => {
            const isOpen = open[group.title] ?? group.expandedByDefault
            const items = group.items.filter(canSee)
            if (items.length === 0) return null
            const GroupIcon = group.icon

            // Dashboard / Overview - تظهر كعنصر منفصل بدون عنوان مجموعة
            if (group.isStandalone) {
              return (
                <div key={group.title} className="mb-2">
                  {items.map((item) => {
                    const Icon = item.icon
                    return (
                      <NavLink
                        key={item.to}
                        to={item.to}
                        className={({ isActive }) =>
                          `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-semibold transition ${
                            isActive ? 'bg-indigo-600 text-white shadow-lg' : 'text-gray-300 hover:bg-gray-800 hover:text-white'
                          }`
                        }
                      >
                        <Icon size={18} />
                        <span>{item.label}</span>
                      </NavLink>
                    )
                  })}
                </div>
              )
            }

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
                              isActive ? 'bg-indigo-600 text-white' : 'text-gray-300 hover:bg-gray-800 hover:text-white'
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
            onClick={() => { logout(); navigate('/login', { replace: true }) }}
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
