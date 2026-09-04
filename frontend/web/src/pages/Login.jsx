import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function Login() {
  const { login, changePassword } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [forceChange, setForceChange] = useState(false)
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [changing, setChanging] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const auth = await login(username, password)
      if (auth.mustChangePassword) setForceChange(true)
      else navigate('/dashboard', { replace: true })
    } catch (err) {
      setError(err.message || 'Invalid credentials')
    } finally { setLoading(false) }
  }

  const handleChangePassword = async (e) => {
    e.preventDefault()
    if (newPassword !== confirmPassword) { setError('Passwords do not match.'); return }
    setChanging(true)
    try {
      await changePassword(password, newPassword)
      navigate('/dashboard', { replace: true })
    } catch (err) { setError(err.message || 'Failed to change password') }
    finally { setChanging(false) }
  }

  if (forceChange) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-900 p-4">
        <div className="w-full max-w-md bg-white rounded-2xl shadow-2xl p-8">
          <h1 className="text-2xl font-bold text-slate-900">Change Password</h1>
          <p className="text-sm text-slate-500 mt-1 mb-6">Your password must be changed before you can continue.</p>
          {error && <div className="mb-4 bg-red-50 border border-red-200 text-red-700 px-4 py-2 rounded-lg text-sm">{error}</div>}
          <form onSubmit={handleChangePassword} className="space-y-4">
            <input type="password" required value={newPassword} onChange={e=>setNewPassword(e.target.value)} placeholder="New password (12+ chars)" className="w-full rounded-lg border border-slate-300 px-4 py-2.5 outline-none focus:ring-2 focus:ring-indigo-500" />
            <input type="password" required value={confirmPassword} onChange={e=>setConfirmPassword(e.target.value)} placeholder="Confirm new password" className="w-full rounded-lg border border-slate-300 px-4 py-2.5 outline-none focus:ring-2 focus:ring-indigo-500" />
            <button disabled={changing} className="w-full bg-indigo-600 text-white rounded-lg py-2.5 font-semibold hover:bg-indigo-700 disabled:opacity-50">{changing?'Updating...':'Change password & continue'}</button>
          </form>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen w-full flex bg-white">
      {/* LEFT - FIXED DARK PANEL */}
      <div className="hidden lg:flex lg:w-[48%] bg-slate-900 relative flex-col justify-between p-12 overflow-hidden">
        {/* Gradient Orbs */}
        <div className="absolute -top-32 -left-32 w-[500px] h-[500px] bg-indigo-600 rounded-full blur-[100px] opacity-40"></div>
        <div className="absolute -bottom-32 -right-20 w-[400px] h-[400px] bg-violet-600 rounded-full blur-[100px] opacity-30"></div>
        
        <div className="relative z-10">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-white rounded-lg flex items-center justify-center">
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none"><path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5" stroke="#4338ca" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/></svg>
            </div>
            <div>
              <div className="text-white font-bold">UMS Portal</div>
              <div className="text-white/50 text-xs -mt-1">University Management System</div>
            </div>
          </div>
        </div>

        <div className="relative z-10">
          <div className="bg-white/10 backdrop-blur-xl border border-white/10 rounded-3xl p-8 text-center">
            <div className="text-5xl mb-4">🏛️</div>
            <h2 className="text-white text-xl font-bold">University Management</h2>
            <p className="text-white/60 text-sm mt-2 leading-relaxed">Secure, modern and reliable platform for managing your entire university ecosystem in one place.</p>
            <div className="mt-6 flex justify-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-white/10 flex items-center justify-center text-white">🎓</div>
              <div className="w-10 h-10 rounded-xl bg-white/10 flex items-center justify-center text-white">📚</div>
              <div className="w-10 h-10 rounded-xl bg-white/10 flex items-center justify-center text-white">👥</div>
            </div>
          </div>
        </div>

        <div className="relative z-10 space-y-1">
          <div className="text-white/90 text-sm font-medium flex items-center gap-2"><span className="w-7 h-7 bg-white/10 rounded-lg flex items-center justify-center">🏫</span> Campus Management</div>
          <div className="text-white/60 text-xs ml-9">All-in-one infrastructure control</div>
          <div className="pt-3 text-white/90 text-sm font-medium flex items-center gap-2"><span className="w-7 h-7 bg-white/10 rounded-lg flex items-center justify-center">👥</span> Student & Faculty Portal</div>
          <div className="text-white/60 text-xs ml-9">Secure & Reliable • 99.9% Uptime</div>
        </div>
      </div>

      {/* RIGHT - FORM */}
      <div className="flex-1 flex items-center justify-center p-6 lg:p-12 bg-gray-50 lg:bg-white">
        <div className="w-full max-w-[400px] bg-white lg:bg-transparent rounded-2xl lg:rounded-none p-8 lg:p-0 shadow-xl lg:shadow-none border lg:border-0 border-slate-100">
          <div className="lg:hidden flex items-center gap-2 mb-8">
            <div className="w-9 h-9 bg-indigo-600 rounded-lg flex items-center justify-center text-white">🎓</div>
            <span className="font-bold text-slate-900">UMS Portal</span>
          </div>

          <h1 className="text-2xl font-bold text-slate-900">UMS Portal</h1>
          <p className="text-slate-500 text-sm mt-1 mb-8">Sign in to your account</p>

          {error && <div className="mb-5 bg-red-50 border border-red-200 text-red-700 px-4 py-2.5 rounded-xl text-sm">{error}</div>}

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">Username</label>
              <input type="text" required value={username} onChange={e=>setUsername(e.target.value)} placeholder="e.g., EMP-ADM-001" className="w-full h-[46px] rounded-xl border border-slate-300 px-4 text-slate-900 outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20" />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">Password</label>
              <input type="password" required value={password} onChange={e=>setPassword(e.target.value)} placeholder="••••••••" className="w-full h-[46px] rounded-xl border border-slate-300 px-4 text-slate-900 outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/20" />
              <div className="flex justify-between items-center mt-3">
                <label className="flex items-center gap-2 text-sm text-slate-600"><input type="checkbox" className="rounded" /> Remember me</label>
                <button type="button" className="text-sm text-indigo-600 font-medium hover:underline">Forgot password?</button>
              </div>
            </div>
            <button type="submit" disabled={loading} className="w-full h-[46px] rounded-xl bg-indigo-600 text-white font-semibold hover:bg-indigo-700 disabled:opacity-50 transition flex items-center justify-center">{loading ? 'Signing in...' : 'Sign in'}</button>
          </form>

          <div className="flex items-center gap-3 my-6">
            <div className="flex-1 h-px bg-slate-200"></div>
            <span className="text-xs text-slate-400">or continue with</span>
            <div className="flex-1 h-px bg-slate-200"></div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <button type="button" className="h-11 rounded-xl border border-slate-200 hover:bg-slate-50 font-bold text-slate-700">G</button>
            <button type="button" className="h-11 rounded-xl border border-slate-200 hover:bg-slate-50 font-bold text-slate-700">M</button>
            <button type="button" className="h-11 rounded-xl border border-slate-200 hover:bg-slate-50 font-bold text-slate-700">A</button>
          </div>

          <div className="text-center mt-8 text-sm">
            <span className="text-slate-500">Don't have an account? </span>
            <button className="text-indigo-600 font-semibold hover:underline">Contact Administrator</button>
          </div>
        </div>
      </div>
    </div>
  )
}
