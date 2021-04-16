using System.Collections.Generic;

namespace Hangfire
{
    public class HangfireDashboardOptions
    {
        public HangfireDashboardAuthorizationMode AuthorizationMode { get; set; } = HangfireDashboardAuthorizationMode.AuthorizeForRemoteRequests;
        public IList<string> AllowedRoles { get; set; } = new List<string>();
        public string AuthorizationPolicyName { get; set; }
        public string DashboardPath { get; set; } = "/hangfire";
    }
}