import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Users from './pages/Users';
import Roles from './pages/Roles';
import RolePermissions from './pages/RolePermissions';
import Groups from './pages/Groups';
import Permissions from './pages/Permissions';
import AuditLogs from './pages/AuditLogs';
import Settings from './pages/Settings';
import Layout from './components/Layout';
import ProtectedRoute from './components/ProtectedRoute';
import Unauthorized from './pages/Unauthorized';

// ✅ Gold ثوابت Gold خارج Gold المكون Gold - reference Gold مستقر Gold - لا JSON.stringify Gold
const USER_MANAGE_PERMS = ['USER_READ', 'USER_CREATE'];

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/unauthorized" element={<Unauthorized />} />

        {/* ✅ Gold Layout Gold محمي Gold - authentication Gold فقط Gold */}
        <Route path="/" element={<ProtectedRoute><Layout /></ProtectedRoute>}>
          
          {/* ✅ Gold Dashboard Gold - الجميع Gold المصادق Gold */}
          <Route index element={<Dashboard />} />
          <Route path="dashboard" element={<Dashboard />} />

          {/* ✅ Gold permission Gold مفرد Gold - المعيار الذهبي Gold - لا مصفوفة Gold */}
          <Route path="users" element={<ProtectedRoute permission="USER_READ"><Users /></ProtectedRoute>} />
          <Route path="roles" element={<ProtectedRoute permission="ROLE_READ"><Roles /></ProtectedRoute>} />
          <Route path="roles/:id/permissions" element={<ProtectedRoute permission="ROLE_UPDATE"><RolePermissions /></ProtectedRoute>} />
          <Route path="groups" element={<ProtectedRoute permission="GROUP_READ"><Groups /></ProtectedRoute>} />
          <Route path="permissions" element={<ProtectedRoute permission="PERMISSION_READ"><Permissions /></ProtectedRoute>} />
          <Route path="audit-logs" element={<ProtectedRoute permission="AUDIT_READ"><AuditLogs /></ProtectedRoute>} />
          <Route path="settings" element={<ProtectedRoute><Settings /></ProtectedRoute>} />

          {/* ✅ Gold مثال Gold مصفوفة Gold - استخدم Gold الثابت Gold من فوق Gold */}
          {/* <Route path="admin" element={<ProtectedRoute permissions={USER_MANAGE_PERMS} requireAll={false}><Admin /></ProtectedRoute>} /> */}
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;