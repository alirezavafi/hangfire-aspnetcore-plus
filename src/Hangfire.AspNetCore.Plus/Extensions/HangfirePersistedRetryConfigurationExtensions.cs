using System.Collections.Generic;
using System.Linq;
using Hangfire.Storage;

namespace Hangfire
{
    public static class HangfirePersistedRetryConfigurationExtensions
    {
        public static void SetPersistedRetryConfiguration(this IStorageConnection connection, string key, int retryAttempts, int[] delayInSecondsPerAttempt = null)
        {
            var r = new PersistedRetryConfigurationAttribute(key, retryAttempts, delayInSecondsPerAttempt);
            connection.SetRangeInHash(r.HashKey, new []{ new KeyValuePair<string, string>(r.RetryAttemptPropertyName, retryAttempts.ToString()), new KeyValuePair<string, string>(r.DelayInSecondsPropertyName, delayInSecondsPerAttempt?.Select(x => x.ToString()).Aggregate((x,y) => $"{x},{y}"))});
        }
    }
}