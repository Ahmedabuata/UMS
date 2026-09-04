import { useCallback, useEffect, useState } from 'react'
import { auditApi } from '../api/client'
import { Badge, Field, Modal, inputCls } from '../components/ui'

const ACTION_FILTERS = ['', 'CREATE', 'UPDATE', 'DELETE', 'LOGIN']
const PAGE_SIZE = 25

export default function AuditLogs() {
  const [logs, setLogs] = useState([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const [userSearch, setUserSearch] = useState('')
  const [action, setAction] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')

  const [detail, setDetail] = useState(null)

  const runQuery = useCallback(async (pageOverride) => {
    const p = pageOverride ?? page
    setLoading(true)
    setError('')
    try {
      const data = await auditApi.query({
        search: userSearch.trim() || undefined,
        action: action || undefined,
        from: from || undefined,
        to: to || undefined,
        page: p,
        pageSize: PAGE_SIZE,
      })
      setLogs(data?.items ?? [])
      setPage(data?.page ?? 1)
      setTotalCount(data?.totalCount ?? 0)
      setTotalPages(data?.totalPages ?? 1)
    } catch (err) {
      setError(err.message || 'Failed to load audit logs')
    } finally {
      setLoading(false)
    }
  }, [userSearch, action, from, to, page])

  useEffect(() => {
    runQuery(page)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userSearch, action, from, to])

  const goTo = (p) => {
    if (p < 1 || p > totalPages) return
    setPage(p)
    runQuery(p)
  }

  const resetFilters = () => {
    setUserSearch('')
    setAction('')
    setFrom('')
    setTo('')
  }

  const hasFilters = userSearch || action || from || to

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Audit Logs</h1>
        <p className="text-gray-500 text-sm mt-1">Track user activity: sign-ins and security record changes.</p>
      </div>

      {error && <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>}

      <div className="mb-6 bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          <Field label="User / entity search">
            <input
              value={userSearch}
              onChange={(e) => { setPage(1); setUserSearch(e.target.value) }}
              placeholder="Username, entity, IP..."
              className={inputCls}
            />
          </Field>
          <Field label="Action">
            <select value={action} onChange={(e) => { setPage(1); setAction(e.target.value) }} className={inputCls}>
              {ACTION_FILTERS.map((a) => (
                <option key={a} value={a}>{a || 'All actions'}</option>
              ))}
            </select>
          </Field>
          <Field label="From">
            <input type="date" value={from} onChange={(e) => { setPage(1); setFrom(e.target.value) }} className={inputCls} />
          </Field>
          <Field label="To">
            <input type="date" value={to} onChange={(e) => { setPage(1); setTo(e.target.value) }} className={inputCls} />
          </Field>
        </div>
        <div className="flex items-center justify-between mt-3">
          <p className="text-sm text-gray-500">{totalCount} {totalCount === 1 ? 'entry' : 'entries'}</p>
          {hasFilters && (
            <button onClick={resetFilters} className="text-sm font-medium text-indigo-600 hover:text-indigo-800">
              Clear filters
            </button>
          )}
        </div>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        {loading ? (
          <p className="p-6 text-gray-500">Loading audit logs...</p>
        ) : logs.length === 0 ? (
          <p className="p-6 text-gray-500">No audit records found.</p>
        ) : (
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Timestamp</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">User</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Action</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Entity</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Entity ID</th>
                <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">IP Address</th>
                <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Details</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {logs.map((log) => (
                <tr key={log.id} className="hover:bg-gray-50">
                  <td className="px-6 py-3.5 text-sm text-gray-700 whitespace-nowrap">{formatTimestamp(log.timestamp)}</td>
                  <td className="px-6 py-3.5 text-sm font-medium text-gray-900">{log.userName || '—'}</td>
                  <td className="px-6 py-3.5"><ActionBadge action={log.action} /></td>
                  <td className="px-6 py-3.5 text-sm text-gray-700">{log.entity}</td>
                  <td className="px-6 py-3.5 text-sm text-gray-500">{log.entityId || '—'}</td>
                  <td className="px-6 py-3.5 text-sm text-gray-500">{log.ipAddress || '—'}</td>
                  <td className="px-6 py-3.5 text-right whitespace-nowrap">
                    <button onClick={() => setDetail(log)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium">
                      View
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {logs.length > 0 && (
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm text-gray-500">
            Page {page} of {totalPages}
          </p>
          <div className="flex gap-2">
            <button
              onClick={() => goTo(page - 1)}
              disabled={page <= 1}
              className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-40"
            >
              Previous
            </button>
            <button
              onClick={() => goTo(page + 1)}
              disabled={page >= totalPages}
              className="rounded-lg border border-gray-300 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-40"
            >
              Next
            </button>
          </div>
        </div>
      )}

      {detail && (
        <Modal title={`Details — ${detail.action} ${detail.entity}`} onClose={() => setDetail(null)} wide>
          <div className="space-y-5 text-sm">
            <div className="grid grid-cols-2 gap-4">
              <Meta label="Timestamp" value={formatTimestamp(detail.timestamp)} />
              <Meta label="User" value={detail.userName || '—'} />
              <Meta label="Action" value={detail.action} />
              <Meta label="Entity" value={`${detail.entity}${detail.entityId ? ` (${detail.entityId})` : ''}`} />
              <Meta label="IP address" value={detail.ipAddress || '—'} />
              {detail.userAgent && <Meta label="User agent" value={detail.userAgent} />}
            </div>

            <JsonBlock title="Old values" value={detail.oldValues} />
            <JsonBlock title="New values" value={detail.newValues} />
          </div>
        </Modal>
      )}
    </div>
  )
}

function Meta({ label, value }) {
  return (
    <div>
      <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider">{label}</p>
      <p className="mt-0.5 text-gray-900 break-words">{value}</p>
    </div>
  )
}

function JsonBlock({ title, value }) {
  if (!value) {
    return (
      <div>
        <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider">{title}</p>
        <p className="mt-1 text-gray-400 italic">No change recorded.</p>
      </div>
    )
  }
  let pretty = value
  try {
    const parsed = typeof value === 'string' ? JSON.parse(value) : value
    pretty = JSON.stringify(parsed, null, 2)
  } catch {
    pretty = String(value)
  }
  return (
    <div>
      <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider">{title}</p>
      <pre className="mt-1 rounded-lg bg-gray-900 text-gray-100 p-4 overflow-x-auto text-xs leading-relaxed">{pretty}</pre>
    </div>
  )
}

function ActionBadge({ action }) {
  const tone = action === 'CREATE' ? 'green' : action === 'DELETE' ? 'red' : action === 'LOGIN' ? 'amber' : 'indigo'
  return <Badge tone={tone}>{action}</Badge>
}

function formatTimestamp(value) {
  if (!value) return '—'
  const d = new Date(value)
  return isNaN(d.getTime()) ? '—' : d.toLocaleString()
}