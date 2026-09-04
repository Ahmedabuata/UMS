import { useEffect, useState } from 'react'
import { permissionsApi } from '../api/client'
import { Field, inputCls } from '../components/ui'

const STORAGE_KEY = 'ums_modules_active'

function loadLocal() {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}')
  } catch {
    return {}
  }
}

export default function Modules() {
  const [modules, setModules] = useState([])
  const [localActive, setLocalActive] = useState(loadLocal)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [search, setSearch] = useState('')

  useEffect(() => {
    ;(async () => {
      setLoading(true)
      setError('')
      try {
        const data = await permissionsApi.modules()
        setModules(data ?? [])
      } catch (err) {
        setError(err.message || 'Failed to load modules')
      } finally {
        setLoading(false)
      }
    })()
  }, [])

  const isActive = (m) => {
    if (m.code in localActive) return localActive[m.code]
    return m.isActive ?? true
  }

  const toggle = (m) => {
    setLocalActive((prev) => {
      const next = { ...prev, [m.code]: !isActive(m) }
      localStorage.setItem(STORAGE_KEY, JSON.stringify(next))
      return next
    })
  }

  const filtered = modules.filter((m) => {
    const term = search.trim().toLowerCase()
    if (!term) return true
    return (
      (m.name || '').toLowerCase().includes(term) ||
      (m.code || '').toLowerCase().includes(term) ||
      (m.description || '').toLowerCase().includes(term)
    )
  })

  const activeCount = modules.filter(isActive).length

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Modules</h1>
        <p className="text-gray-500 text-sm mt-1">{modules.length} modules — {activeCount} active. Toggling is local for now.</p>
      </div>

      {error && <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>}

      <div className="mb-6 bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search modules by name, code or description"
          className="w-full rounded-lg border border-gray-300 px-4 py-2 text-gray-900 focus:ring-2 focus:ring-indigo-500 outline-none"
        />
      </div>

      {loading ? (
        <p className="text-gray-500">Loading modules...</p>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {filtered.map((m) => (
            <div key={m.code || m.id} className="bg-white rounded-xl shadow-sm border border-gray-200 p-5 flex flex-col">
              <div className="flex items-center justify-between">
                <h3 className="text-base font-semibold text-gray-900">{m.name}</h3>
                <span className="text-xs font-mono text-gray-400">{m.code}</span>
              </div>
              <p className="text-sm text-gray-500 mt-1 flex-1">{m.description || '—'}</p>
              <div className="mt-4">
                <button
                  type="button"
                  onClick={() => toggle(m)}
                  className={`w-full rounded-lg px-4 py-2 text-sm font-semibold transition ${
                    isActive(m) ? 'bg-green-100 text-green-700 hover:bg-green-200' : 'bg-gray-100 text-gray-500 hover:bg-gray-200'
                  }`}
                >
                  {isActive(m) ? 'Active' : 'Inactive'}
                </button>
              </div>
            </div>
          ))}
          {filtered.length === 0 && <p className="text-gray-500 col-span-full">No modules match your search.</p>}
        </div>
      )}
    </div>
  )
}