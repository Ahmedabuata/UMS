namespace Identity.Api.DTOs;

// --- Context / Resource-Specific Assignment Requests ---
public record AssignRoleRequest(Guid RoleId);
public record AssignPermissionRequest(Guid PermissionId);
public record BulkSyncPermissionsRequest(List<Guid> PermissionIds);
public record AssignGroupRequest(Guid GroupId);

// --- Global / Explicit Entity Relationship Requests ---
public record AssignRoleToUserRequest(
    Guid UserId,
    Guid RoleId
);

public record UnassignRoleFromUserRequest(
    Guid UserId,
    Guid RoleId
);

public record AssignPermissionToRoleRequest(
    Guid RoleId,
    Guid PermissionId
);

public record UnassignPermissionFromRoleRequest(
    Guid RoleId,
    Guid PermissionId
);

public record AssignGroupToUserRequest(
    Guid UserId,
    Guid GroupId
);

public record UnassignGroupFromUserRequest(
    Guid UserId,
    Guid GroupId
);