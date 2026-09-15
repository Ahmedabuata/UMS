using Hangfire.Dashboard;

namespace Identity.Api.Filters;

public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext.User?.Identity?.IsAuthenticated != true)
            return false;

        if (httpContext.User.IsInRole("SUPER_ADMIN"))
            return true;

        return httpContext.User.Claims
            .Any(c => c.Type == "permission" && c.Value == "AUDIT_ARCHIVE_MANAGE");
    }
}