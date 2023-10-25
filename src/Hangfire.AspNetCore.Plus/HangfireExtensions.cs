using System;
using System.Collections.Generic;
using Correlate.DependencyInjection;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Correlate;
using Hangfire.Dashboard;
using Hangfire.Logging;
using Hangfire.PerformContextAccessor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using HangfireLogger = Hangfire.Console.Extensions.HangfireLogger;

namespace Hangfire
{
    public static class HangfireExtensions
    {
        public static IServiceCollection AddHangfirePlus(this IServiceCollection services, JobStorage jobStorage,
            Action<IGlobalConfiguration> configAction = null, bool useJobLongExpirationTime = true, bool useCorrelationId = true, bool automaticRetry = false, int automaticRetryCount = 3)
        {
            services.AddHttpContextAccessor();
            services.AddTransient<HangfireRoleAuthorizationFilter>();
            services.AddHangfirePerformContextAccessor();
            services.AddHangfire((serviceProvider, configuration) =>
            {
                configuration
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseStorage(jobStorage)
                    .UseConsole()
                    .UsePerformContextAccessorFilter()
                    ;
                if (useCorrelationId)
                {
                    services.AddCorrelate();
                    configuration.UseCorrelate(serviceProvider);
                }
                if (automaticRetry)
                    configuration.UseFilter(new AutomaticRetryAttribute() { Attempts = automaticRetryCount });
                if (useJobLongExpirationTime)
                    configuration.UseFilter(new ProlongExpirationTimeAttribute());

                configAction?.Invoke(configuration);
            });
            services.AddHangfireConsoleExtensions();
            services.AddSingleton<IHangfireLogger, HangfireSerilogLogger>();
            JobStorage.Current = jobStorage;

            return services;
        }

        public static IServiceCollection AddHangfireServerPlus(
            this IServiceCollection services, JobStorage jobStorage,
            Action<IGlobalConfiguration> config = null, int workerCount = 20, string[] queues = null,
            bool useJobLongExpirationTime = true, bool useCorrelationId = true, bool automaticRetry = false, int automaticRetryCount = 3)
        {
            services.AddHangfirePlus(jobStorage, config, useJobLongExpirationTime: useJobLongExpirationTime, useCorrelationId: useCorrelationId, automaticRetry: automaticRetry, automaticRetryCount: automaticRetryCount);
            services.AddHangfireServer(opt =>
            {
                opt.WorkerCount = workerCount;
                opt.Queues = queues ?? new[] {"default"};
            });
            return services;
        }

        public static IApplicationBuilder UseHangfirePlus(this IApplicationBuilder app)
        {
            JobFilterWithServiceProviderAttribute.ServiceProvider = app.ApplicationServices;
            return app;
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