using University.Shared.Common;
using University.Shared.DTOs.Security;

namespace University.Core.Interfaces.Services.Security;

public interface IAuditLogService
{
    Task<Result<bool>> LogAsync(string action, Guid? userId, string details);
    Task<Result<PagedResult<AuditLogItemDto>>> QueryAsync(
        string? search = null,
        string? action = null,
        string? entity = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 20);
}
