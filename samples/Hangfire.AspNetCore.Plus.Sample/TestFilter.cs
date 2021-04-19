using System.Threading.Tasks;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Hangfire.AspNetCore.Plus.Sample
{
    public class TestFilter : JobFailureFilterAttribute
    {
        private ILogger _logger;

        public TestFilter()
        {
            _logger = ServiceProvider.GetRequiredService<ILogger>();
        }


        protected override Task OnJobFailure(ElectStateContext context, FailedState state)
        {
            _logger.Error("job failure");
            return Task.CompletedTask;
        }
    }
}