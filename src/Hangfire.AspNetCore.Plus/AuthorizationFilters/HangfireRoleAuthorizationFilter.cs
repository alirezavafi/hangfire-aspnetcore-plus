using System.Collections.Generic;
using System.Linq;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace Hangfire
{
    public class HangfireRoleAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public List<string> AllowedRoles { get; } = new List<string>();
        public HangfireRoleAuthorizationFilter(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public bool Authorize(DashboardContext context)
        {
            var user = httpContextAccessor.HttpContext?.User;

            var isAuthenticated = user?.Identity?.IsAuthenticated == true;
            if (!AllowedRoles.Any() || !isAuthenticated)
            {
                return isAuthenticated;
            }

            return AllowedRoles.Any(role => user.IsInRole(role));
        }
    }
}