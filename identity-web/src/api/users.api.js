import axiosInstance from './axiosInstance';

// ============================================================
// RULE #28 + #51: Normalize - يقرأ كل الاحتمالات - يصلح Phone -
// ============================================================
const normalizeUser = (u) => {
  if (!u) return u;
  return {
    ...u,
    id: u.id || u._id || u.Id || u.userId,
    username: u.username || u.userName || u.UserName || u.name,
    email: u.email || u.Email,
    phone_number: u.phone_number || u.phone || u.phoneNumber || u.Phone || u.PhoneNumber || null,
    phone: u.phone || u.phone_number || u.phoneNumber || u.Phone || u.PhoneNumber || null,
    phoneNumber: u.phoneNumber || u.phone_number || u.phone || u.PhoneNumber || null,
    IsActive: u.IsActive ?? u.isActive ?? u.is_active ?? u.active ?? u.status ?? true,
    isActive: u.IsActive ?? u.isActive ?? u.is_active ?? u.active ?? true,
    is_active: u.IsActive ?? u.isActive ?? u.is_active ?? u.active ?? true,
  };
};

// ============================================================
// RULE #18: لا ترسل phone فارغ - Backend Validator يرفض null/""
// + NEW: حذف password إذا SendPasswordByEmail = true
// ============================================================
const cleanPayload = (data) => {
  const p = { ...data };

  //  تحويل phone_number → phoneNumber
  if (p.phone_number && !p.phoneNumber) {
    p.phoneNumber = p.phone_number;
    delete p.phone_number;
  }

  // Strip empty phone
  const ph = p.phoneNumber || p.phone || p.phone_number;
  if (!ph?.toString().trim()) {
    delete p.phoneNumber;
    delete p.phone;
    delete p.phone_number;
  }

  // Strip empty password
  if (!p.password || !p.password.toString().trim()) {
    delete p.password;
  }

  // If SendPasswordByEmail = true → remove password
  if (p.sendPasswordByEmail === true) {
    delete p.password;
  }

  return p;
};

// ============================================================
// USERS - حسب Swagger
// GET /api/users
// POST /api/users
// GET /api/users/{id}
// PUT /api/users/{id}
// DELETE /api/users/{id}
// GET /api/users/me
// PUT /api/users/me
// PATCH /api/users/{id}/status
// GET /api/users/count
// ============================================================
export const getUsers = async (params) => {
  const res = await axiosInstance.get('/users', { params });
  // Normalize مباشرة لمنع Phone -
  const body = res?.data?.data ?? res?.data;
  const fix = (arr) => arr.filter(Boolean).map(normalizeUser);

  if (Array.isArray(body)) {
    if (res.data?.data && Array.isArray(res.data.data)) {
      res.data.data = fix(res.data.data);
    } else if (Array.isArray(res.data)) {
      res.data = fix(res.data);
    }
  } else if (body?.users && Array.isArray(body.users)) {
    if (res.data?.data?.users) res.data.data.users = fix(body.users);
    else if (res.data?.users) res.data.users = fix(body.users);
  } else if (body?.items && Array.isArray(body.items)) {
    if (res.data?.data?.items) res.data.data.items = fix(body.items);
    else if (res.data?.items) res.data.items = fix(body.items);
  }
  return res;
};

export const getAll = getUsers;
export const getUsersPaginated = getUsers;
export const getUsersCount = () => axiosInstance.get('/users/count');

//  PATCH /users/{id}/status
export const toggleUserStatus = (id, isActive) =>
  axiosInstance.patch(`/users/${id}/status`, {
    isActive: isActive,
    IsActive: isActive,
    is_active: isActive,
    active: isActive,
  });
export const toggleStatus = toggleUserStatus;

//  GET /users/me + PUT /users/me
export const getMe = () => axiosInstance.get('/users/me');
export const updateMe = (data) => axiosInstance.put('/users/me', cleanPayload(data));

export const getById = (id) => axiosInstance.get(`/users/${id}`);
export const getUserById = getById;

//  POST /users - يدعم sendPasswordByEmail
export const create = (data) => axiosInstance.post('/users', cleanPayload(data));
export const createUser = create;

export const update = (id, data) => axiosInstance.put(`/users/${id}`, cleanPayload(data));
export const updateUser = update;

export const deleteUser = (id) => axiosInstance.delete(`/users/${id}`);
export const deleteU = deleteUser;
export const remove = deleteUser;
export const del = deleteUser;
export const deleteApi = deleteUser;

// ============================================================
// ROLES - حسب Swagger
// GET /api/users/{userId}/roles
// POST /api/users/{userId}/roles {roleId}
// DELETE /api/users/{userId}/roles/{roleId}
// GET /api/roles
// ============================================================
export const getRoles = (userId) => axiosInstance.get(`/users/${userId}/roles`);
export const getUserRoles = getRoles;

export const getAllRoles = () => axiosInstance.get('/roles');
export const getRolesList = getAllRoles;

export const addRole = (userId, roleId) =>
  axiosInstance.post(`/users/${userId}/roles`, { roleId, RoleId: roleId });

export const removeRole = (userId, roleId) =>
  axiosInstance.delete(`/users/${userId}/roles/${roleId}`);

//  يحوّل setRoles([ids]) القديم إلى POST/DELETE - يمنع 405
export const setRoles = async (userId, roleIds) => {
  const curRes = await getRoles(userId);
  const curBody = curRes?.data?.data ?? curRes?.data;
  const curList = Array.isArray(curBody) ? curBody : curBody.roles || curBody.items || curBody.data || [];
  const curIds = curList.map((r) => r.id || r.roleId || r.role_id || r);

  const desired = Array.isArray(roleIds) ? roleIds : [];
  const toAdd = desired.filter((id) => !curIds.includes(id));
  const toRem = curIds.filter((id) => !desired.includes(id));

  for (const rid of toAdd) await addRole(userId, rid);
  for (const rid of toRem) await removeRole(userId, rid);
  return { success: true };
};

// ============================================================
// GROUPS - حسب Swagger
// GET /api/users/{userId}/groups
// POST /api/users/{userId}/groups
// DELETE /api/users/{userId}/groups/{groupId}
// ============================================================
export const getGroups = (userId) => axiosInstance.get(`/users/${userId}/groups`);
export const getUserGroups = getGroups;
export const getAllGroups = () => axiosInstance.get('/groups');

export const addGroup = (userId, groupId) =>
  axiosInstance.post(`/users/${userId}/groups`, { groupId, GroupId: groupId });

export const removeGroup = (userId, groupId) =>
  axiosInstance.delete(`/users/${userId}/groups/${groupId}`);

export const setGroups = async (userId, groupIds) => {
  const curRes = await getGroups(userId);
  const curBody = curRes?.data?.data ?? curRes?.data;
  const curList = Array.isArray(curBody) ? curBody : curBody.groups || curBody.items || curBody.data || [];
  const curIds = curList.map((g) => g.id || g.groupId || g.group_id || g);
  const desired = Array.isArray(groupIds) ? groupIds : [];
  const toAdd = desired.filter((id) => !curIds.includes(id));
  const toRem = curIds.filter((id) => !desired.includes(id));
  for (const gid of toAdd) await addGroup(userId, gid);
  for (const gid of toRem) await removeGroup(userId, gid);
  return { success: true };
};

// ============================================================
// default export
// ============================================================
const usersApi = {
  // Users
  getUsers,
  getAll,
  getUsersPaginated,
  getUsersCount,
  getById,
  getUserById,
  getMe,
  updateMe,
  toggleUserStatus,
  toggleStatus,
  create,
  createUser,
  update,
  updateUser,
  delete: deleteUser,
  deleteUser,
  deleteU,
  remove,
  del,
  deleteApi,
  // Roles
  getRoles,
  getUserRoles,
  getAllRoles,
  getRolesList,
  addRole,
  removeRole,
  setRoles,
  // Groups
  getGroups,
  getUserGroups,
  getAllGroups,
  addGroup,
  removeGroup,
  setGroups,
  // Helper
  normalizeUser,
};

export default usersApi;