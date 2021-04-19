using System.Threading.Tasks;
using Hangfire.States;

namespace Hangfire
{
    public abstract class SuccessJobFilterAttribute : JobFilterWithServiceProviderAttribute, IElectStateFilter
    {
        public void OnStateElection(ElectStateContext context)
        {
            if (context.CandidateState is SucceededState s)
            {
                Task.Run(() => this.OnJobSuccess(context, s)).Wait();
            }
        }

        protected abstract Task OnJobSuccess(ElectStateContext context, SucceededState state);
    }
}