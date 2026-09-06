namespace Identity.Api.DTOs;

public record AssignRoleRequest(Guid RoleId);
public record AssignPermissionRequest(Guid PermissionId);
public record BulkSyncPermissionsRequest(List<Guid> PermissionIds);
public record AssignGroupRequest(Guid GroupId);
