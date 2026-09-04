import { useCallback, useEffect, useMemo, useState } from 'react'
import { buildingsApi, branchesApi, classroomsApi } from '../api/client'
import { Badge, Field, Modal, inputCls } from '../components/ui'

const ROOM_TYPES = [
  { value: 1, label: 'Classroom' },
  { value: 2, label: 'Lab' },
  { value: 3, label: 'Seminar' },
  { value: 4, label: 'Auditorium' },
  { value: 5, label: 'Computer Lab' },
]

const ROOM_TYPE_LABEL = Object.fromEntries(ROOM_TYPES.map((t) => [t.value, t.label]))

function emptyBuilding() {
  return { name: '', code: '', branchId: '', floors: 1, address: '' }
}

function emptyClassroom(buildingId = '') {
  return { buildingId, roomNumber: '', capacity: 30, floor: 1, roomType: 1 }
}

export default function Buildings() {
  const [buildings, setBuildings] = useState([])
  const [branches, setBranches] = useState([])
  const [selectedId, setSelectedId] = useState(null)
  const [classrooms, setClassrooms] = useState([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [roomsLoading, setRoomsLoading] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [showBuilding, setShowBuilding] = useState(false)
  const [buildingForm, setBuildingForm] = useState(emptyBuilding())
  const [editingBuilding, setEditingBuilding] = useState(null)
  const [showClassroom, setShowClassroom] = useState(false)
  const [classroomForm, setClassroomForm] = useState(emptyClassroom())
  const [editingClassroom, setEditingClassroom] = useState(null)

  const loadBuildings = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await buildingsApi.list()
      setBuildings(data ?? [])
      setSelectedId((prev) => prev ?? (data && data[0]?.id) ?? null)
    } catch (err) {
      setError(err.message || 'Failed to load buildings')
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

  const loadClassrooms = useCallback(async (id) => {
    if (!id) { setClassrooms([]); return }
    setRoomsLoading(true)
    try {
      const data = await buildingsApi.classrooms(id)
      setClassrooms(data ?? [])
    } catch (err) {
      setClassrooms([])
    } finally {
      setRoomsLoading(false)
    }
  }, [])

  useEffect(() => { loadBuildings() }, [loadBuildings])
  useEffect(() => { loadBranches() }, [loadBranches])
  useEffect(() => { loadClassrooms(selectedId) }, [selectedId, loadClassrooms])

  const selected = useMemo(() => buildings.find((b) => b.id === selectedId), [buildings, selectedId])

  const filtered = buildings.filter((b) => {
    const term = search.trim().toLowerCase()
    if (!term) return true
    return (
      (b.name || '').toLowerCase().includes(term) ||
      (b.code || '').toLowerCase().includes(term) ||
      (b.address || '').toLowerCase().includes(term)
    )
  })

  const flash = (msg) => { setNotice(msg); setTimeout(() => setNotice(''), 3000) }

  const handleCreateBuilding = async (e) => {
    e.preventDefault()
    setError('')
    if (!buildingForm.branchId) {
      setError('Please select a branch.')
      return
    }
    try {
      const created = await buildingsApi.create({
        name: buildingForm.name.trim(),
        code: buildingForm.code.trim().toUpperCase(),
        branchId: buildingForm.branchId || null,
        address: buildingForm.address?.trim() || null,
        floors: Number(buildingForm.floors) || 1,
      })
      setShowBuilding(false)
      setBuildingForm(emptyBuilding())
      await loadBuildings()
      setSelectedId(created.id)
      flash('Building created successfully.')
    } catch (err) {
      setError(err.message || 'Failed to create building')
    }
  }

  const handleUpdateBuilding = async (e) => {
    e.preventDefault()
    if (!editingBuilding) return
    setError('')
    if (!editingBuilding.branchId) {
      setError('Please select a branch.')
      return
    }
    try {
      await buildingsApi.update(editingBuilding.id, {
        name: editingBuilding.name.trim(),
        code: editingBuilding.code.trim().toUpperCase(),
        branchId: editingBuilding.branchId,
        address: editingBuilding.address?.trim() || null,
        floors: Number(editingBuilding.floors) || 1,
      })
      setEditingBuilding(null)
      await loadBuildings()
      flash('Building updated successfully.')
    } catch (err) {
      setError(err.message || 'Failed to update building')
    }
  }

  const handleDeleteBuilding = async (building) => {
    if (!window.confirm(`Delete building "${building.name}"? This also removes its classrooms.`)) return
    setError('')
    try {
      await buildingsApi.remove(building.id)
      await loadBuildings()
      flash('Building deleted.')
    } catch (err) {
      setError(err.message || 'Failed to delete building')
    }
  }

  const openCreateClassroom = () => {
    setError('')
    setClassroomForm(emptyClassroom(selectedId || ''))
    setShowClassroom(true)
  }

  const handleCreateClassroom = async (e) => {
    e.preventDefault()
    setError('')
    try {
      const buildingId = classroomForm.buildingId || null
      const building = buildings.find((b) => b.id === buildingId)
      await classroomsApi.create({
        buildingId,
        branchId: building?.branchId || null,
        roomNumber: classroomForm.roomNumber.trim(),
        capacity: parseInt(classroomForm.capacity, 10) || 0,
        floor: parseInt(classroomForm.floor, 10) || 1,
        roomType: Number(classroomForm.roomType),
      })
      setShowClassroom(false)
      await loadClassrooms(selectedId)
      await loadBuildings()
      flash('Classroom created successfully.')
    } catch (err) {
      setError(err.message || 'Failed to create classroom')
    }
  }

  const openEditClassroom = (room) => {
    setError('')
    setEditingClassroom({
      ...room,
      buildingId: selectedId || room.buildingId || '',
      roomType: room.roomType ?? 1,
    })
  }

  const handleUpdateClassroom = async (e) => {
    e.preventDefault()
    if (!editingClassroom) return
    setError('')
    try {
      await classroomsApi.update(editingClassroom.id, {
        buildingId: editingClassroom.buildingId || null,
        roomNumber: editingClassroom.roomNumber,
        capacity: Number(editingClassroom.capacity) || 0,
        floor: Number(editingClassroom.floor) || 1,
        roomType: Number(editingClassroom.roomType),
      })
      setEditingClassroom(null)
      await loadClassrooms(selectedId)
      await loadBuildings()
      flash('Classroom updated successfully.')
    } catch (err) {
      setError(err.message || 'Failed to update classroom')
    }
  }

  const handleDeleteClassroom = async (room) => {
    if (!window.confirm(`Delete classroom "${room.roomNumber}"?`)) return
    setError('')
    try {
      await classroomsApi.remove(room.id)
      await loadClassrooms(selectedId)
      await loadBuildings()
      flash('Classroom deleted.')
    } catch (err) {
      setError(err.message || 'Failed to delete classroom')
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Buildings &amp; Classrooms</h1>
          <p className="text-gray-500 text-sm mt-1">Manage campus buildings and their classrooms.</p>
        </div>
        <button onClick={() => { setError(''); setBuildingForm({ ...emptyBuilding(), branchId: branches.length === 1 ? branches[0].id : '' }); setShowBuilding(true) }} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 transition">
          Add Building
        </button>
      </div>

      {notice && <div className="mb-4 rounded-lg bg-green-50 border border-green-200 text-green-700 px-4 py-3 text-sm">{notice}</div>}
      {error && <div className="mb-4 rounded-lg bg-red-50 border border-red-200 text-red-700 px-4 py-3 text-sm">{error}</div>}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left: buildings list */}
        <div className="lg:col-span-1 space-y-4">
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search buildings..."
              className="w-full rounded-lg border border-gray-300 px-4 py-2 text-gray-900 focus:ring-2 focus:ring-indigo-500 outline-none"
            />
          </div>
          {loading ? (
            <p className="text-gray-500">Loading buildings...</p>
          ) : filtered.length === 0 ? (
            <div className="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
              <p className="text-gray-500">No buildings found.</p>
            </div>
          ) : (
            <div className="space-y-3">
              {filtered.map((b) => (
                <div
                  key={b.id}
                  onClick={() => setSelectedId(b.id)}
                  className={`bg-white rounded-xl shadow-sm border cursor-pointer p-4 transition ${
                    b.id === selectedId ? 'border-indigo-500 ring-2 ring-indigo-200' : 'border-gray-200 hover:border-indigo-300'
                  }`}
                >
                  <div className="flex items-start justify-between">
                    <div>
                    <p className="text-sm font-semibold text-gray-900">{b.name}</p>
                    <p className="text-xs text-gray-500 mt-0.5">
                      {b.code || '—'}
                      {b.branchName && <span className="ml-1.5 inline-flex items-center rounded-full bg-indigo-100 text-indigo-700 px-2 py-0.5 text-xs font-medium">{b.branchName}</span>}
                    </p>
                  </div>
                    {b.isActive ? <Badge tone="green">Active</Badge> : <Badge tone="red">Inactive</Badge>}
                  </div>
                  <p className="mt-2 text-xs text-gray-500 truncate">{b.address || 'No address'}</p>
                  <p className="mt-1 text-xs text-gray-500">{b.floors} floor(s) · {b.classroomCount} classroom(s)</p>
                  <div className="mt-3 flex justify-end gap-3 text-sm">
                    <button onClick={(e) => { e.stopPropagation(); setError(''); setEditingBuilding({ ...b, address: b.address || '', branchId: b.branchId || '' }); }} className="text-indigo-600 hover:text-indigo-800 font-medium">
                      Edit
                    </button>
                    <button onClick={(e) => { e.stopPropagation(); handleDeleteBuilding(b) }} className="text-red-600 hover:text-red-800 font-medium">
                      Delete
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Right: classrooms of selected building */}
        <div className="lg:col-span-2">
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
              <div>
                <h2 className="text-base font-semibold text-gray-900">
                  Classrooms in {selected?.name || '—'}
                  {selected?.branchName && <span className="ml-2 text-sm font-normal text-gray-500">({selected.branchName})</span>}
                </h2>
                <p className="text-xs text-gray-500">{selected ? `${selected.code} · ${selected.floors} floors` : 'Select a building'}</p>
              </div>
              {selected && (
                <button onClick={openCreateClassroom} className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white hover:bg-indigo-700">
                  Add Classroom
                </button>
              )}
            </div>
            {roomsLoading ? (
              <p className="p-6 text-gray-500">Loading classrooms...</p>
            ) : !selected ? (
              <p className="p-6 text-gray-500">Select a building to view its classrooms.</p>
            ) : classrooms.length === 0 ? (
              <p className="p-6 text-gray-500">No classrooms in this building yet.</p>
            ) : (
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Room</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Capacity</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Type</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Floor</th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
                    <th className="px-6 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {classrooms.map((room) => (
                    <tr key={room.id} className="hover:bg-gray-50">
                      <td className="px-6 py-4 text-sm font-medium text-gray-900">{room.roomNumber}</td>
                      <td className="px-6 py-4 text-sm text-gray-500">{room.capacity}</td>
                      <td className="px-6 py-4"><RoomBadge type={room.roomType} /></td>
                      <td className="px-6 py-4 text-sm text-gray-500">{room.floor}</td>
                      <td className="px-6 py-4">{room.isActive ? <Badge tone="green">Active</Badge> : <Badge tone="red">Inactive</Badge>}</td>
                      <td className="px-6 py-4 text-right whitespace-nowrap">
                        <button onClick={() => openEditClassroom(room)} className="text-indigo-600 hover:text-indigo-800 text-sm font-medium mr-3">Edit</button>
                        <button onClick={() => handleDeleteClassroom(room)} className="text-red-600 hover:text-red-800 text-sm font-medium">Delete</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      </div>

      {/* Add Building modal */}
      {showBuilding && (
        <Modal title="Add Building" onClose={() => setShowBuilding(false)}>
          <form onSubmit={handleCreateBuilding} className="space-y-4">
            {branches.length === 0 && (
              <div className="rounded-lg bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 text-sm">
                No branches found. Please create a branch first in Infrastructure &gt; Branches.
              </div>
            )}
            <Field label="Name *">
              <input value={buildingForm.name} onChange={(e) => setBuildingForm({ ...buildingForm, name: e.target.value })} required className={inputCls} />
            </Field>
            <Field label="Code *">
              <input value={buildingForm.code} onChange={(e) => setBuildingForm({ ...buildingForm, code: e.target.value })} placeholder="B1" required className={inputCls} />
            </Field>
            <Field label="Branch *">
              <select
                value={buildingForm.branchId || ''}
                onChange={(e) => setBuildingForm({ ...buildingForm, branchId: e.target.value })}
                className={inputCls}
                required
                disabled={branches.length === 0}
              >
                <option value="">Select Branch</option>
                {branches.map((br) => (
                  <option key={br.id} value={br.id}>{br.branchName} ({br.branchCode}) - {br.branchLocation || '—'}</option>
                ))}
              </select>
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Floors *">
                <input type="number" min={1} max={20} value={buildingForm.floors} onChange={(e) => setBuildingForm({ ...buildingForm, floors: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Address">
                <input value={buildingForm.address || ''} onChange={(e) => setBuildingForm({ ...buildingForm, address: e.target.value })} placeholder="Rodestraat 14, 2000 Antwerp" className={inputCls} />
              </Field>
            </div>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setShowBuilding(false)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">Cancel</button>
              <button type="submit" disabled={branches.length === 0} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed">Create</button>
            </div>
          </form>
        </Modal>
      )}

      {/* Edit Building modal */}
      {editingBuilding && (
        <Modal title={`Edit — ${editingBuilding.name}`} onClose={() => setEditingBuilding(null)}>
          <form onSubmit={handleUpdateBuilding} className="space-y-4">
            {branches.length === 0 && (
              <div className="rounded-lg bg-amber-50 border border-amber-200 text-amber-800 px-4 py-3 text-sm">
                No branches found. Please create a branch first in Infrastructure &gt; Branches.
              </div>
            )}
            <Field label="Name *">
              <input value={editingBuilding.name || ''} onChange={(e) => setEditingBuilding({ ...editingBuilding, name: e.target.value })} required className={inputCls} />
            </Field>
            <Field label="Code *">
              <input value={editingBuilding.code || ''} onChange={(e) => setEditingBuilding({ ...editingBuilding, code: e.target.value })} placeholder="B1" required className={inputCls} />
            </Field>
            <Field label="Branch *">
              <select
                value={editingBuilding.branchId || ''}
                onChange={(e) => setEditingBuilding({ ...editingBuilding, branchId: e.target.value })}
                className={inputCls}
                required
                disabled={branches.length === 0}
              >
                <option value="">Select Branch</option>
                {branches.map((br) => (
                  <option key={br.id} value={br.id}>{br.branchName} ({br.branchCode}) - {br.branchLocation || '—'}</option>
                ))}
              </select>
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Floors *">
                <input type="number" min={1} max={20} value={editingBuilding.floors} onChange={(e) => setEditingBuilding({ ...editingBuilding, floors: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Address">
                <input value={editingBuilding.address || ''} onChange={(e) => setEditingBuilding({ ...editingBuilding, address: e.target.value })} placeholder="Rodestraat 14, 2000 Antwerp" className={inputCls} />
              </Field>
            </div>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setEditingBuilding(null)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">Cancel</button>
              <button type="submit" disabled={branches.length === 0} className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed">Save</button>
            </div>
          </form>
        </Modal>
      )}

      {/* Add Classroom modal */}
      {showClassroom && (
        <Modal title="Add Classroom" onClose={() => setShowClassroom(false)}>
          <form onSubmit={handleCreateClassroom} className="space-y-4">
            <Field label="Building *">
              <select
                value={classroomForm.buildingId || ''}
                onChange={(e) => setClassroomForm({ ...classroomForm, buildingId: e.target.value })}
                className={inputCls}
                required
              >
                <option value="">Select Building</option>
                {buildings.map((b) => (
                  <option key={b.id} value={b.id}>{b.name} ({b.code})</option>
                ))}
              </select>
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Room number *">
                <input value={classroomForm.roomNumber} onChange={(e) => setClassroomForm({ ...classroomForm, roomNumber: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Capacity *">
                <input type="number" min={1} value={classroomForm.capacity} onChange={(e) => setClassroomForm({ ...classroomForm, capacity: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Floor *">
                <input type="number" min={0} value={classroomForm.floor} onChange={(e) => setClassroomForm({ ...classroomForm, floor: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Type *">
                <select value={classroomForm.roomType} onChange={(e) => setClassroomForm({ ...classroomForm, roomType: Number(e.target.value) })} required className={inputCls}>
                  {ROOM_TYPES.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </Field>
            </div>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setShowClassroom(false)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">Cancel</button>
              <button type="submit" className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700">Create</button>
            </div>
          </form>
        </Modal>
      )}

      {/* Edit Classroom modal */}
      {editingClassroom && (
        <Modal title={`Edit — ${editingClassroom.roomNumber}`} onClose={() => setEditingClassroom(null)}>
          <form onSubmit={handleUpdateClassroom} className="space-y-4">
            <Field label="Building *">
              <select value={editingClassroom.buildingId || ''} onChange={(e) => setEditingClassroom({ ...editingClassroom, buildingId: e.target.value })} className={inputCls} required>
                <option value="">Select building</option>
                {buildings.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}
              </select>
            </Field>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <Field label="Room number *">
                <input value={editingClassroom.roomNumber || ''} onChange={(e) => setEditingClassroom({ ...editingClassroom, roomNumber: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Capacity *">
                <input type="number" min={1} value={editingClassroom.capacity} onChange={(e) => setEditingClassroom({ ...editingClassroom, capacity: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Floor *">
                <input type="number" min={0} value={editingClassroom.floor} onChange={(e) => setEditingClassroom({ ...editingClassroom, floor: e.target.value })} required className={inputCls} />
              </Field>
              <Field label="Type *">
                <select value={editingClassroom.roomType} onChange={(e) => setEditingClassroom({ ...editingClassroom, roomType: Number(e.target.value) })} required className={inputCls}>
                  {ROOM_TYPES.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
                </select>
              </Field>
            </div>
            <div className="flex justify-end gap-3">
              <button type="button" onClick={() => setEditingClassroom(null)} className="rounded-lg border border-gray-300 px-4 py-2 font-medium text-gray-700 hover:bg-gray-50">Cancel</button>
              <button type="submit" className="rounded-lg bg-indigo-600 px-4 py-2 font-semibold text-white hover:bg-indigo-700">Save</button>
            </div>
          </form>
        </Modal>
      )}
    </div>
  )
}

function RoomBadge({ type }) {
  const label = ROOM_TYPE_LABEL[type] || 'Classroom'
  const map = {
    1: 'bg-indigo-100 text-indigo-700',
    2: 'bg-purple-100 text-purple-700',
    3: 'bg-cyan-100 text-cyan-700',
    4: 'bg-orange-100 text-orange-700',
    5: 'bg-teal-100 text-teal-700',
  }
  const cls = map[type] || map[1]
  return <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${cls}`}>{label}</span>
}