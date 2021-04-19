using System.Threading.Tasks;
using Hangfire.States;

namespace Hangfire
{
    public abstract class JobFailureFilterAttribute : JobFilterWithServiceProviderAttribute, IElectStateFilter
    {
        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is FailedState s)
            {
                Task.Run(() => this.OnJobFailure(context, s)).Wait();
            }
        }

        protected abstract Task OnJobFailure(ElectStateContext context, FailedState state);
    }
}