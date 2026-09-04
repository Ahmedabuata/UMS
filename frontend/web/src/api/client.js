const BASE_URL = 'http://localhost:8080/api'

const TOKEN_KEY = 'ums_token'
const REFRESH_KEY = 'ums_refresh_token'
const USER_KEY = 'ums_user'

export function getToken() {
  return localStorage.getItem(TOKEN_KEY)
}

export function setSession(auth) {
  localStorage.setItem(TOKEN_KEY, auth.token)
  if (auth.refreshToken) localStorage.setItem(REFRESH_KEY, auth.refreshToken)
  localStorage.setItem(USER_KEY, JSON.stringify(auth.user || {}))
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(REFRESH_KEY)
  localStorage.removeItem(USER_KEY)
}

export function getStoredUser() {
  try {
    return JSON.parse(localStorage.getItem(USER_KEY) || 'null')
  } catch {
    return null
  }
}

export async function api(path, { method = 'GET', body, headers = {}, auth = true } = {}) {
  const h = { 'Content-Type': 'application/json', ...headers }
  if (auth) {
    const token = getToken()
    if (token) h.Authorization = `Bearer ${token}`
  }

  const res = await fetch(`${BASE_URL}${path}`, {
    method,
    headers: h,
    body: body ? JSON.stringify(body) : undefined,
  })

  if (res.status === 401) {
    clearSession()
    if (typeof window !== 'undefined' && window.location.pathname !== '/login') {
      window.location.href = '/login'
    }
    throw new ApiError('Unauthorized', 401, null)
  }

  let data = null
  const text = await res.text()
  if (text) {
    try {
      data = JSON.parse(text)
    } catch {
      data = text
    }
  }

  if (!res.ok) {
    throw new ApiError(data?.message || data?.error || data?.title || 'Request failed', res.status, data)
  }

  return data
}

export class ApiError extends Error {
  constructor(message, status, data) {
    super(message)
    this.status = status
    this.data = data
  }
}

export const authApi = {
  login: (username, password) => api('/auth/login', { method: 'POST', body: { username, password }, auth: false }),
  changePassword: (userId, oldPassword, newPassword) =>
    api(`/auth/change-password?userId=${encodeURIComponent(userId)}&oldPassword=${encodeURIComponent(oldPassword)}&newPassword=${encodeURIComponent(newPassword)}`, { method: 'POST' }),
}

export const rolesApi = {
  list: () => api('/security/roles'),
  get: (id) => api(`/security/roles/${id}`),
  create: (dto) => api('/security/roles', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/security/roles/${id}`, { method: 'PUT', body: dto }),
  remove: (id) => api(`/security/roles/${id}`, { method: 'DELETE' }),
  getPermissionIds: (id) => api(`/security/roles/${id}/permissions`),
  setPermissions: (id, permissionIds) => api(`/security/roles/${id}/permissions`, { method: 'PUT', body: { permissionIds } }),
}

export const usersApi = {
  list: (params = {}) => api(`/users${qs(params)}`),
  get: (id) => api(`/users/${id}`),
  setActive: (id, active) => api(`/security/users/${id}/activate?activate=${active}`, { method: 'PUT' }),
  createForEmployee: (employeeId, dto) => api(`/users/create-for-employee/${employeeId}`, { method: 'POST', body: dto }),
  update: (id, dto) => api(`/security/users/${id}`, { method: 'PUT', body: dto }),
  resetPassword: (id, newPassword) => api(`/security/users/${id}/reset-password`, { method: 'PUT', body: { newPassword } }),
}

export const groupsApi = {
  list: (params = {}) => api(`/security/groups${qs(params)}`),
  get: (id) => api(`/security/groups/${id}`),
  create: (dto) => api('/security/groups', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/security/groups/${id}`, { method: 'PUT', body: dto }),
  remove: (id) => api(`/security/groups/${id}`, { method: 'DELETE' }),
  getMembers: (id) => api(`/security/groups/${id}/members`),
  addMember: (id, userId) => api(`/security/groups/${id}/members/${userId}`, { method: 'POST' }),
  removeMember: (id, userId) => api(`/security/groups/${id}/members/${userId}`, { method: 'DELETE' }),
}

export const permissionsApi = {
  grouped: () => api('/security/permissions/grouped'),
  modules: () => api('/security/permissions/modules'),
  list: (module) => api(`/security/permissions${module ? `?module=${encodeURIComponent(module)}` : ''}`),
}

export const auditApi = {
  query: (params = {}) => api(`/security/audit-logs${qs(params)}`),
}

export const policiesApi = {
  get: () => api('/security/policies'),
  update: (settings) => api('/security/policies', { method: 'PUT', body: settings }),
}

export const departmentsApi = {
  list: (params = {}) => api(`/AdministrativeDepartments${qs(params)}`),
  get: (id) => api(`/AdministrativeDepartments/${id}`),
  create: (dto) => api('/AdministrativeDepartments', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/AdministrativeDepartments/${id}`, { method: 'PUT', body: dto }),
  remove: (id) => api(`/AdministrativeDepartments/${id}`, { method: 'DELETE' }),
  restore: (id) => api(`/AdministrativeDepartments/${id}/restore`, { method: 'PUT' }),
}

export const adminDepartmentsApi = departmentsApi

export const employeesApi = {
  list: (params = {}) => api(`/Employees${qs(params)}`),
  get: (id) => api(`/Employees/${id}`),
  academicTitles: (academicTitle) => api(`/Employees/academic-titles${academicTitle ? `?academicTitle=${encodeURIComponent(academicTitle)}` : ''}`),
  create: (dto) => api('/Employees', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/Employees/${id}`, { method: 'PUT', body: dto }),
  remove: (id) => api(`/Employees/${id}`, { method: 'DELETE' }),
  deactivate: (id) => api(`/Employees/${id}/deactivate`, { method: 'PUT' }),
  activate: (id) => api(`/Employees/${id}/activate`, { method: 'PUT' }),
  restore: (id) => api(`/Employees/${id}/restore`, { method: 'PUT' }),
  previewNumber: (departmentId) => api(`/Employees/number-preview?departmentId=${encodeURIComponent(departmentId)}`),
  withoutUserAccount: () => api('/Employees/without-user-account'),
}

export const studentsApi = {
  list: (params = {}) => api(`/Students${qs(params)}`),
  get: (id) => api(`/Students/${id}`),
  create: (dto) => api('/Students', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/Students/${id}`, { method: 'PUT', body: dto }),
  getGpa: (id) => api(`/Students/${id}/gpa`),
  previewNumber: (facultyId, academicDepartmentId) => api(`/Students/number-preview?facultyId=${encodeURIComponent(facultyId)}&academicDepartmentId=${encodeURIComponent(academicDepartmentId)}`),
}

export const coursesApi = {
  list: (params = {}) => api(`/Courses${qs(params)}`),
  get: (id) => api(`/Courses/${id}`),
  create: (dto) => api('/Courses', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/Courses/${id}`, { method: 'PUT', body: dto }),
  remove: (id) => api(`/Courses/${id}`, { method: 'DELETE' }),
}

export const branchesApi = {
  list: () => api('/Branches'),
  get: (id) => api(`/Branches/${id}`),
  create: (dto) => api('/Branches', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/Branches/${id}`, { method: 'PUT', body: dto }),
  remove: (id) => api(`/Branches/${id}`, { method: 'DELETE' }),
}

export const facultiesApi = {
  list: () => api('/Faculties'),
  get: (id) => api(`/Faculties/${id}`),
  academicDepartments: (id) => api(`/AcademicDepartments?facultyId=${id}`),
  create: (dto) => api('/Faculties', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/Faculties/${id}`, { method: 'PUT', body: dto }),
  remove: (id) => api(`/Faculties/${id}`, { method: 'DELETE' }),
}

export const academicDepartmentsApi = {
  list: (facultyId) => api(`/AcademicDepartments${facultyId ? `?facultyId=${facultyId}` : ''}`),
  create: (dto) => api('/AcademicDepartments', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/AcademicDepartments/${id}`, { method: 'PUT', body: dto }),
  remove: (id) => api(`/AcademicDepartments/${id}`, { method: 'DELETE' }),
}

export const buildingsApi = {
  list: () => api('/Buildings'),
  get: (id) => api(`/Buildings/${id}`),
  classrooms: (id) => api(`/Buildings/${id}/classrooms`),
  create: (dto) => api('/Buildings', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/Buildings/${id}`, { method: 'PUT', body: dto }),
  remove: (id) => api(`/Buildings/${id}`, { method: 'DELETE' }),
}

export const classroomsApi = {
  get: (id) => api(`/Classrooms/${id}`),
  create: (dto) => api('/Classrooms', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/Classrooms/${id}`, { method: 'PUT', body: dto }),
  remove: (id) => api(`/Classrooms/${id}`, { method: 'DELETE' }),
}

export const semestersApi = {
  list: () => api('/Semesters'),
  get: (id) => api(`/Semesters/${id}`),
  create: (dto) => api('/Semesters', { method: 'POST', body: dto }),
  update: (id, dto) => api(`/Semesters/${id}`, { method: 'PUT', body: dto }),
  open: (id) => api(`/Semesters/${id}/open`, { method: 'PUT' }),
  close: (id) => api(`/Semesters/${id}/close`, { method: 'PUT' }),
  setCurrent: (id) => api(`/Semesters/${id}/set-current`, { method: 'PUT' }),
  remove: (id) => api(`/Semesters/${id}`, { method: 'DELETE' }),
}

function qs(params) {
  const parts = Object.entries(params)
    .filter(([, v]) => v !== undefined && v !== null && v !== '')
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
  return parts.length ? `?${parts.join('&')}` : ''
}
