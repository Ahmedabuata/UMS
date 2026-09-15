import axiosInstance from './axiosInstance';

/**
 * Audit Logs API Module
 * 
 * Handles all audit-log-related HTTP operations.
 * 
 * Backend Endpoints (AuditLogsController):
 * - GET /api/audit-logs           → getAll (Smart Routing HOT/COLD)
 * - GET /api/audit-logs/count     → getCount
 * - GET /api/audit-logs/{id}      → getById (HOT → COLD)
 * - GET /api/audit-logs/export    → export (CSV)
 * 
 * Query Parameters (for getAll and getCount):
 * - from, to:        Date range
 * - userId:          Filter by user
 * - user:            Alias for userId
 * - action:          Filter by action (e.g., BULK_SYNC_PERMISSIONS)
 * - entity:          Filter by entity (e.g., role_permissions)
 * - search:          Full-text search
 * - page:            Page number (1-based)
 * - pageSize:        Items per page (1-100)
 */
export const auditLogsApi = {
  /**
   * Get paginated list of audit logs.
   * Automatically routes to HOT, COLD, or both based on date range.
   * 
   * GET /api/audit-logs
   * 
   * @param {Object} params - Query parameters
   * @returns {Object} { items: AuditLogDto[], total, page, pageSize }
   */
  getAll: (params) => 
    axiosInstance.get('/audit-logs', { params }),

  /**
   * Get total count of audit logs matching the filters.
   * Sums counts from HOT + COLD based on Smart Routing.
   * 
   * GET /api/audit-logs/count
   * 
   * @param {Object} params - Query parameters (from, to, userId, action, entity)
   * @returns {Object} { count: number }
   */
  getCount: (params) => 
    axiosInstance.get('/audit-logs/count', { params }),

  /**
   * Get a single audit log by ID.
   * Searches HOT first, then COLD.
   * 
   * GET /api/audit-logs/{id}
   * 
   * @param {string} id - Audit log UUID
   * @returns {Object} AuditLogDto
   */
  getById: (id) => 
    axiosInstance.get(`/audit-logs/${id}`),

  /**
   * Export audit logs as CSV.
   * Returns a Blob (file download).
   * 
   * GET /api/audit-logs/export
   * 
   * @param {Object} params - Query parameters (from, to, action, search)
   * @returns {Blob} CSV file
   */
  export: (params) => 
    axiosInstance.get('/audit-logs/export', { 
      params, 
      responseType: 'blob' 
    }),
};

export default auditLogsApi;