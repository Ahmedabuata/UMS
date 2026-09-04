import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function ForceChangePassword() {
  const { changePassword, user } = useAuth()
  const navigate = useNavigate()
  const [oldPassword, setOldPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [changing, setChanging] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    if (newPassword !== confirmPassword) { setError('Passwords do not match.'); return }
    if (newPassword.length < 12) { setError('New password must be at least 12 characters.'); return }
    setChanging(true)
    try {
      await changePassword(oldPassword, newPassword)
      navigate('/dashboard', { replace: true })
    } catch (err) {
      setError(err.message || 'Failed to change password')
    } finally {
      setChanging(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-900 p-4">
      <div className="w-full max-w-md bg-white rounded-2xl shadow-2xl p-8">
        <h1 className="text-2xl font-bold text-slate-900">Change Password</h1>
        <p className="text-sm text-slate-500 mt-1 mb-6">
          {user?.username ? `Account ${user.username}: ` : ''}Your password must be changed before you can continue.
        </p>
        {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-700 px-4 py-2 rounded-lg text-sm">{error}</div>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <input type="password" required value={oldPassword} onChange={(e) => setOldPassword(e.target.value)} placeholder="Current (temp) password" className="w-full rounded-lg border border-slate-300 px-4 py-2.5 outline-none focus:ring-2 focus:ring-indigo-500" />
          <input type="password" required value={newPassword} onChange={(e) => setNewPassword(e.target.value)} placeholder="New password (12+ chars)" className="w-full rounded-lg border border-slate-300 px-4 py-2.5 outline-none focus:ring-2 focus:ring-indigo-500" />
          <input type="password" required value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} placeholder="Confirm new password" className="w-full rounded-lg border border-slate-300 px-4 py-2.5 outline-none focus:ring-2 focus:ring-indigo-500" />
          <button disabled={changing} className="w-full bg-indigo-600 text-white rounded-lg py-2.5 font-semibold hover:bg-indigo-700 disabled:opacity-50">
            {changing ? 'Updating...' : 'Change password & continue'}
          </button>
        </form>
      </div>
    </div>
  )
}
