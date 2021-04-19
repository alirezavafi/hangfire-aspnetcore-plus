using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire.MissionControl;
using Hangfire.SqlServer;
using Hangfire.Tags;
using Hangfire.Tags.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Hangfire.AspNetCore.Plus.Sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var storageOptions = new SqlServerStorageOptions()
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(30),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(5),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                SchemaName = "Hangfire",
            };

            var storage = new SqlServerStorage("Server=.;Database=HangfireTest;User Id=***;Password=***;", storageOptions);
            Action<IGlobalConfiguration> additionalHangfireConfiguration = (conf) =>
            {
                conf.UseTagsWithSql(new TagsOptions()
                {
                    Storage = new SqlTagsServiceStorage(storageOptions),
                    TagsListStyle = TagsListStyle.Dropdown
                });
                conf.UseMissionControl(typeof(Startup).Assembly);
            };
            
            services.AddHangfirePlus(storage, additionalHangfireConfiguration);
            services.AddHangfireServer();
            // or
            //services.AddHangfireServerPlus(storage, additionalHangfireConfiguration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            app.UseHangfirePlus();
            app.UseEndpoints(e =>
            {
                e.MapHangfireDashboardPlus(app.ApplicationServices, new HangfireDashboardOptions()
                {
                    AuthorizationMode = HangfireDashboardAuthorizationMode.NoAuthorize,
                    DashboardPath = "/dashboard",
                });
            });

            Hangfire.BackgroundJob.Enqueue<TestJob>(p => p.Test());
        }
    }
}