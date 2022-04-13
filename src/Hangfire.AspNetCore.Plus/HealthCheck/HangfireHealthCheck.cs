using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hangfire.HealthCheck
{
    public class HangfireHealthCheck : IHealthCheck
    {
        private readonly HangfireHealthCheckOptions _hangfireHealthCheckOptions;

        public HangfireHealthCheck(HangfireHealthCheckOptions hangfireHealthCheckOptions)
        {
            _hangfireHealthCheckOptions = hangfireHealthCheckOptions ?? throw new ArgumentNullException(nameof(hangfireHealthCheckOptions));
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var hangfireMonitoringApi = global::Hangfire.JobStorage.Current.GetMonitoringApi();
                var stats = hangfireMonitoringApi.GetStatistics();
                var serversCount = stats.Servers;
                var failedJobsCount = stats.Failed;
                var data = new Dictionary<string, object>
                {
                    {"AvailableServers", serversCount},
                    {"FailedJobs", failedJobsCount},
                    {"SucceedJobs", stats.Succeeded}
                };
                
                List<string>? errorList = null;
                if (_hangfireHealthCheckOptions.UnHealthyMinimumFailedCount.HasValue)
                {
                    if (failedJobsCount >= _hangfireHealthCheckOptions.UnHealthyMinimumFailedCount)
                    {
                        (errorList ??= new()).Add($"Hangfire has #{failedJobsCount} failed jobs and the maximum available is {_hangfireHealthCheckOptions.UnHealthyMinimumFailedCount}.");
                    }
                }

                if (serversCount == 0)
                {
                    (errorList ??= new()).Add($"{serversCount} server registered. Expected minimum {1}.");
                }

                if (errorList?.Count > 0)
                {
                    return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus, description: string.Join(" + ", errorList), data: data));
                }
                
                List<string>? warnList = null;
                if (_hangfireHealthCheckOptions.DegradedMinimumFailedCount.HasValue)
                {
                    if (failedJobsCount >= _hangfireHealthCheckOptions.DegradedMinimumFailedCount)
                    {
                        (warnList ??= new()).Add($"Hangfire has #{failedJobsCount} failed jobs and the maximum available is {_hangfireHealthCheckOptions.DegradedMinimumFailedCount}.");
                    }
                }

                if (_hangfireHealthCheckOptions.DegradedMinimumAvailableServers.HasValue)
                {
                    var degradedMinimumAvailableServers = _hangfireHealthCheckOptions.DegradedMinimumAvailableServers.Value;
                    if (serversCount <= degradedMinimumAvailableServers)
                    {
                        (warnList ??= new()).Add($"{serversCount} server registered. Expected minimum {degradedMinimumAvailableServers + 1}.");
                    }
                    if (warnList?.Count > 0)
                    {
                        return Task.FromResult(new HealthCheckResult(HealthStatus.Degraded, description: string.Join(" + ", warnList), data: data));
                    }
                }

                return Task.FromResult(HealthCheckResult.Healthy(data: data));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus, exception: ex));
            }
        }
    }
}
