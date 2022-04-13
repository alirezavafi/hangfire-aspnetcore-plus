namespace Hangfire.HealthCheck
{
    /// <summary>
    /// Options for <see cref="Hangfire.HealthCheck.HangfireHealthCheck"/>.
    /// </summary>
    public class HangfireHealthCheckOptions
    {
        public int? UnHealthyMinimumFailedCount { get; set; }
        public int? DegradedMinimumFailedCount { get; set; }
        public int? DegradedMinimumAvailableServers { get; set; }
    }
}
