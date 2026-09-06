namespace Identity.Contracts.Common;

public record ApiResponse<T>(bool Success, T? Data, string? Message, List<string>? Errors);
public record PagedResponse<T>(List<T> Items, int Total, int Page, int PageSize);
