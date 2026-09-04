import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import { AuthProvider, useAuth } from './context/AuthContext'
import AdministrativeDepartments from './pages/AdministrativeDepartments'
import AuditLogs from './pages/AuditLogs'
import Branches from './pages/Branches'
import Buildings from './pages/Buildings'
import Dashboard from './pages/Dashboard'
import Faculties from './pages/Faculties'
import ForceChangePassword from './pages/ForceChangePassword'
import Groups from './pages/Groups'
import HRModule from './pages/HRModule'
import Login from './pages/Login'
import Modules from './pages/Modules'
import Permissions from './pages/Permissions'
import Roles from './pages/Roles'
import SecurityPolicies from './pages/SecurityPolicies'
import Semesters from './pages/Semesters'
import Users from './pages/Users'

function RequireAuth({ children }) {
  const { isAuthenticated } = useAuth()
  return isAuthenticated ? children : <Navigate to="/login" replace />
}

// V6: users with mustChangePassword=true are forced to change their password
// before they can access any module. Redirects guarded pages to /force-change-password.
function MustChangeGuard({ children }) {
  const { isAuthenticated, mustChangePassword } = useAuth()
  if (isAuthenticated && mustChangePassword) return <Navigate to="/force-change-password" replace />
  return children
}

function RedirectIfAuthed({ children }) {
  const { isAuthenticated } = useAuth()
  return isAuthenticated ? <Navigate to="/dashboard" replace /> : children
}

function Gate({ perms, children }) {
  const { permissions } = useAuth()
  const ok = perms.some((p) => permissions.includes(p))
  return ok ? children : <Navigate to="/dashboard" replace />
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<RequireAuth><MustChangeGuard><Layout /></MustChangeGuard></RequireAuth>}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/users" element={<Gate perms={['USER_READ']}><Users /></Gate>} />
            <Route path="/roles" element={<Gate perms={['USER_READ']}><Roles /></Gate>} />
            <Route path="/groups" element={<Gate perms={['USER_READ']}><Groups /></Gate>} />
            <Route path="/permissions" element={<Gate perms={['USER_READ']}><Permissions /></Gate>} />
            <Route path="/audit-logs" element={<Gate perms={['USER_READ']}><AuditLogs /></Gate>} />
            <Route path="/security-policies" element={<Gate perms={['USER_READ']}><SecurityPolicies /></Gate>} />
            <Route path="/branches" element={<Branches />} />
            <Route path="/modules" element={<Modules />} />
            <Route path="/administrative-departments" element={<AdministrativeDepartments />} />
            <Route path="/hr" element={<HRModule />} />
            <Route path="/faculties" element={<Faculties />} />
            <Route path="/buildings" element={<Buildings />} />
            <Route path="/semesters" element={<Semesters />} />
          </Route>
          <Route
            path="/login"
            element={
              <RedirectIfAuthed>
                <Login />
              </RedirectIfAuthed>
            }
          />
          <Route path="/force-change-password" element={<RequireAuth><ForceChangePassword /></RequireAuth>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}