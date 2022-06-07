using System;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire
{
    public class JobExpirationTimeAttribute : JobFilterAttribute, IApplyStateFilter
    {
        private readonly TimeSpan _timeSpan;

        public JobExpirationTimeAttribute(TimeSpan timeSpan)
        {
            _timeSpan = timeSpan;
        }

        public JobExpirationTimeAttribute(int days)
        {
            _timeSpan = TimeSpan.FromDays(days);
        }
        
        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            context.JobExpirationTimeout = _timeSpan;
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }
    }
}