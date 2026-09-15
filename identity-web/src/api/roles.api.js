import axiosInstance from './axiosInstance';

/**
 * Roles API Module
 * 
 * Handles all role-related HTTP operations:
 * - CRUD operations for roles
 * - Role-Permission assignment (bulk operations)
 * 
 * GOLDEN RULE #11:
 * For bulk changes, ALWAYS use bulk operations (syncPermissions).
 * NEVER loop addPermission/removePermission in the UI.
 */
export const rolesApi = {
  // ============================================================
  // Roles CRUD
  // ============================================================

  /**
   * Get paginated list of roles.
   * GET /api/roles
   * @param {Object} params - { page, pageSize, search }
   */
  getAll: (params) => 
    axiosInstance.get('/roles', { params }),

  /**
   * Get a single role by ID.
   * GET /api/roles/{id}
   */
  getById: (id) => 
    axiosInstance.get(`/roles/${id}`),

  /**
   * Create a new role.
   * POST /api/roles
   */
  create: (data) => 
    axiosInstance.post('/roles', data),

  /**
   * Update an existing role.
   * PUT /api/roles/{id}
   */
  update: (id, data) => 
    axiosInstance.put(`/roles/${id}`, data),

  /**
   * Delete a role.
   * DELETE /api/roles/{id}
   */
  delete: (id) => 
    axiosInstance.delete(`/roles/${id}`),

  // ============================================================
  // Role Permissions
  // ============================================================

  /**
   * Get permissions assigned to a specific role.
   * GET /api/roles/{roleId}/permissions
   * 
   * @param {string} roleId - Role UUID
   * @param {Object} params - { search, page, pageSize }
   * @returns { items: PermissionDto[], total, page, pageSize }
   */
  getPermissions: (roleId, params = {}) => 
    axiosInstance.get(`/roles/${roleId}/permissions`, { params }),

  /**
   * Add a SINGLE permission to a role.
   * POST /api/roles/{roleId}/permissions
   * Body: { permissionId: "guid" }
   * 
   * ⚠️ WARNING: Use sparingly!
   * - Each call revokes ALL users' tokens for this role.
   * - Creates a separate Audit log entry.
   * - For UI, use syncPermissions instead (GOLDEN RULE #11).
   * 
   * @param {string} roleId - Role UUID
   * @param {string} permissionId - Permission UUID
   */
  addPermission: (roleId, permissionId) => 
    axiosInstance.post(`/roles/${roleId}/permissions`, { permissionId }),

  /**
   * Replace ALL permissions for a role (Bulk Sync).
   * PUT /api/roles/{roleId}/permissions
   * Body: { permissionIds: ["guid1", "guid2", ...] }
   * 
   * ✅ RECOMMENDED: Use this for UI bulk operations.
   * - Single HTTP request.
   * - Database Transaction (atomic).
   * - Revokes tokens ONCE.
   * - Single Audit log entry.
   * 
   * @param {string} roleId - Role UUID
   * @param {string[]} permissionIds - Array of Permission UUIDs
   */
  syncPermissions: (roleId, permissionIds) => 
    axiosInstance.put(`/roles/${roleId}/permissions`, { permissionIds }),

  /**
   * Remove a SINGLE permission from a role.
   * DELETE /api/roles/{roleId}/permissions/{permissionId}
   * 
   * ⚠ WARNING: Use sparingly!
   * - Each call revokes ALL users' tokens for this role.
   * - Creates a separate Audit log entry.
   * - For UI, use syncPermissions instead (GOLDEN RULE #11).
   * 
   * @param {string} roleId - Role UUID
   * @param {string} permissionId - Permission UUID
   */
  removePermission: (roleId, permissionId) => 
    axiosInstance.delete(`/roles/${roleId}/permissions/${permissionId}`),

  // ============================================================
  // Role Users (Optional - for future features)
  // ============================================================

  /**
   * Get users assigned to a specific role.
   * GET /api/roles/{roleId}/users
   * 
   * (Placeholder - add if endpoint exists)
   */
  getUsers: (roleId, params = {}) => 
    axiosInstance.get(`/roles/${roleId}/users`, { params }),

  /**
   * Assign multiple users to a role (Bulk).
   * POST /api/roles/{roleId}/users
   * Body: { userIds: ["guid1", "guid2", ...] }
   * 
   * (Placeholder - add if endpoint exists)
   */
  syncUsers: (roleId, userIds) => 
    axiosInstance.post(`/roles/${roleId}/users`, { userIds }),
};

export default rolesApi;