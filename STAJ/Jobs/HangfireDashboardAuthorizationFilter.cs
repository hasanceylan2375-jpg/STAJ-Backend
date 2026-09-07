using System.Net;
using Hangfire.Dashboard;

namespace STAJ.Jobs
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            if (httpContext.User.Identity?.IsAuthenticated == true
                && httpContext.User.IsInRole("Admin"))
            {
                return true;
            }

            var environment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var remoteIp = httpContext.Connection.RemoteIpAddress;

            // Local development access keeps the dashboard usable while JWT auth
            // remains the production authorization mechanism.
            return environment.IsDevelopment()
                   && remoteIp != null
                   && IPAddress.IsLoopback(remoteIp);
        }
    }
}
