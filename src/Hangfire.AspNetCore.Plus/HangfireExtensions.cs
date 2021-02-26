using System;
using System.Collections.Generic;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Hangfire
{
    public class HangfireDashboardOptions
    {
        public HangfireDashboardAuthorizationMode AuthorizationMode { get; set; } = HangfireDashboardAuthorizationMode.AuthorizeForRemoteRequests;
        public IList<string> AllowedRoles { get; set; } = new List<string>();
        public string AuthorizationPolicyName { get; set; }
        public string DashboardPath { get; set; } = "/hangfire";
    }
    
    public enum HangfireDashboardAuthorizationMode
    {
        Authorize,
        NoAuthorize,
        AuthorizeForRemoteRequests
    }
    
    public static class HangfireExtensions
    {
        public static IServiceCollection AddHangfirePlus(this IServiceCollection services, JobStorage jobStorage,
            Action<IGlobalConfiguration> configAction = null, TimeSpan? queuePollInternal = null)
        {
            services.AddHttpContextAccessor();
            services.AddTransient<HangfireRoleAuthorizationFilter>();
            services.AddHealthChecks()
                .AddHangfire((p) =>
                {
                    p.MaximumJobsFailed = 5;
                    p.MinimumAvailableServers = 1;
                }, tags: new[] {"jobs"});
            services.AddHangfire(configuration =>
            {
                configuration
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseStorage(jobStorage)
                    .UseConsole()
                    .UseFilter(new ProlongExpirationTimeAttribute())
                    .UseFilter(new AutomaticRetryAttribute() {Attempts = 10});
                configAction?.Invoke(configuration);
            });
            services.AddHangfireConsoleExtensions();
            JobStorage.Current = jobStorage;

            return services;
        }

        public static IServiceCollection AddHangfireServerPlus(
            this IServiceCollection services, JobStorage jobStorage,
            Action<IGlobalConfiguration> config = null, int workerCount = 20, string[] queues = null,
            TimeSpan? queuePollInternal = null)
        {
            services.AddHangfirePlus(jobStorage, config, queuePollInternal);
            services.AddHangfireServer(opt =>
            {
                opt.WorkerCount = workerCount;
                opt.Queues = queues ?? new[] {"default"};
            });
            return services;
        }
        
        public static IEndpointRouteBuilder MapHangfireDashboardPlus(this IEndpointRouteBuilder e,
            IServiceProvider provider, HangfireDashboardOptions options = null)
        {
            options ??= new HangfireDashboardOptions();
            IDashboardAuthorizationFilter authFilter;
            switch (options.AuthorizationMode)
            {
                case HangfireDashboardAuthorizationMode.Authorize:
                    var roleAuthFilter = new HangfireRoleAuthorizationFilter(provider.GetService<IHttpContextAccessor>());
                    roleAuthFilter.AllowedRoles.AddRange(options.AllowedRoles ?? new List<string>());
                    authFilter = roleAuthFilter;
                    break;
                case HangfireDashboardAuthorizationMode.NoAuthorize:
                    authFilter = new NoDashboardAuthorizationFilter();
                    break;
                case HangfireDashboardAuthorizationMode.AuthorizeForRemoteRequests:
                    authFilter = new LocalRequestsOnlyAuthorizationFilter();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(options.AuthorizationMode), options.AuthorizationMode, null);
            }

            var r = e.MapHangfireDashboard(options.DashboardPath, new DashboardOptions()
            {
                Authorization = new List<IDashboardAuthorizationFilter> {authFilter}
            });

            if (options.AuthorizationMode == HangfireDashboardAuthorizationMode.Authorize)
            {
                if (string.IsNullOrWhiteSpace(options.AuthorizationPolicyName))
                    r.RequireAuthorization();
                else
                    r.RequireAuthorization(options.AuthorizationPolicyName);
            }
            
            return e;
        }
    }
}