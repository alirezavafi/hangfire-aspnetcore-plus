namespace Hangfire
{
    public class HealthCheckOptions
    {
        public string Name { get; set; } = "Hangfire";
        public string[] Tags { get; set; } = new[] {"jobs"};
        public int MaximumJobsFailed { get; set; } = 5;
        public int MinimumAvailableServers { get; set; } = 1;
    }
}