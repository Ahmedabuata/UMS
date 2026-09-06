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
  const { permissions, hasAnyPermission, isSuperAdmin } = useAuth()
  if (isSuperAdmin()) return children
  const ok = hasAnyPermission(perms)
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
            <Route path="/branches" element={<Gate perms={['BRANCH_READ','USER_READ']}><Branches /></Gate>} />
            <Route path="/modules" element={<Gate perms={['MODULE_READ','USER_READ']}><Modules /></Gate>} />
            <Route path="/administrative-departments" element={<Gate perms={['ADMIN_DEPT_READ','USER_READ']}><AdministrativeDepartments /></Gate>} />
            <Route path="/hr" element={<Gate perms={['HR_EMPLOYEE_READ','HR_ATTENDANCE_READ','HR_RECRUITMENT_READ','HR_LEAVE_READ','HR_CONTRACT_READ','HR_SALARY_READ','HR_EVALUATION_READ']}><HRModule /></Gate>} />
            <Route path="/faculties" element={<Gate perms={['FACULTY_READ','USER_READ']}><Faculties /></Gate>} />
            <Route path="/buildings" element={<Gate perms={['BUILDING_READ','USER_READ']}><Buildings /></Gate>} />
            <Route path="/semesters" element={<Gate perms={['SEMESTER_READ','USER_READ']}><Semesters /></Gate>} />
          </Route>
          <Route path="/login" element={<RedirectIfAuthed><Login /></RedirectIfAuthed>} />
          <Route path="/force-change-password" element={<RequireAuth><ForceChangePassword /></RequireAuth>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
