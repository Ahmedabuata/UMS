import { useEffect, useMemo, useState } from 'react'
import { permissionsApi } from '../api/client'
import { Badge, inputCls } from '../components/ui'

export default function Permissions() {
  const [groups, setGroups] = useState([])
  const [modules, setModules] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [search, setSearch] = useState('')
  const [moduleFilter, setModuleFilter] = useState('')

  useEffect(() => {
    let cancelled = false
    ;(async () => {
      setLoading(true)
      setError('')
      try {
        const [grouped, mods] = await Promise.all([permissionsApi.grouped(), permissionsApi.modules()])
        if (cancelled) return
        setGroups(grouped ?? [])
        setModules(mods ?? [])
      } catch (err) {
        if (!cancelled) setError(err.message || 'Failed to load permissions')
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    const keyOf = (g) => g.moduleCode || g.module
    return groups
      .filter((g) => !moduleFilter || keyOf(g) === moduleFilter)
      .map((g) => ({
        ...g,
        permissions: g.permissions.filter((p) => {
          const haystack = [p.code, p.name, p.permissionName].filter(Boolean).join(' ').toLowerCase()
          return term ? haystack.includes(term) : true
        }),
      }))
      .filter((g) => g.permissions.length > 0)
  }, [groups, search, moduleFilter])

  const total = useMemo(() => groups.reduce((sum, g) => sum + (g.permissions?.length ?? 0), 0), [groups])

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Permissions</h1>
        <p className="text-gray-500 text-sm mt-1">
          Read-only catalog of {total} application permissions grouped by module.
        </p>
      </div>

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>
      )}

      <div className="mb-6 bg-white rounded-xl shadow-sm border border-gray-200 p-4 flex flex-col sm:flex-row gap-3">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search by permission name..."
          className="flex-1 rounded-lg border border-gray-300 px-4 py-2 text-gray-900 focus:ring-2 focus:ring-indigo-500 outline-none"
        />
        <select value={moduleFilter} onChange={(e) => setModuleFilter(e.target.value)} className={inputCls + ' sm:w-56'}>
          <option value="">All modules</option>
          {modules.map((m) => (
            <option key={m.code || m.id} value={m.code}>{m.name} ({m.code})</option>
          ))}
        </select>
      </div>

      {loading ? (
        <p className="text-gray-500">Loading permissions...</p>
      ) : filtered.length === 0 ? (
        <p className="text-gray-500">No permissions match your filters.</p>
      ) : (
        <div className="space-y-6">
          {filtered.map((group) => {
            const code = group.moduleCode || group.module
            const moduleInfo = modules.find((m) => m.code === code) || code
            return (
            <div key={code} className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
              <div className="px-6 py-4 bg-gray-50 border-b border-gray-200 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <h2 className="text-base font-semibold text-gray-900">{typeof moduleInfo === 'string' ? moduleInfo : `${moduleInfo.name} (${moduleInfo.code})`}</h2>
                  {code === 'HR' ? <Badge tone="indigo">HR</Badge> : null}
                </div>
                <Badge tone="indigo">{group.permissions.length} {group.permissions.length === 1 ? 'permission' : 'permissions'}</Badge>
              </div>
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-white">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Permission</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Module</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Description</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {group.permissions.map((p) => (
                    <tr key={p.id} className="hover:bg-gray-50">
                      <td className="px-6 py-3.5">
                        <div className="flex items-center gap-2">
                          <code className="text-sm font-medium text-indigo-700">{p.code || p.name || p.permissionName}</code>
                          {p.isSensitive ? (
                            <span title="Sensitive permission" className="inline-flex items-center justify-center text-amber-500">🔒</span>
                          ) : null}
                        </div>
                      </td>
                      <td className="px-6 py-3.5">
                        <Badge tone="gray">{p.moduleCode || p.module || code}</Badge>
                      </td>
                      <td className="px-6 py-3.5 text-sm text-gray-600">{p.description || '—'}</td>
                      <td className="px-6 py-3.5">
                        {p.isActive ? <Badge tone="green">Active</Badge> : <Badge tone="red">Inactive</Badge>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            )
          })}
        </div>
      )}
    </div>
  )
}