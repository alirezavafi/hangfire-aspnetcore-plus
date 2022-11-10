using System.Collections.Generic;
using Hangfire.Storage;

namespace Hangfire
{
    public static class HangfireJobKeyExtensions
    {
        private const string linkHashKey = "x-backgroundjob-keys:";

        public static void SetBusinessKey(this IStorageConnection connection, string jobId, string businessKey)
        {
            connection.SetRangeInHash(linkHashKey + businessKey, new Dictionary<string, string>() { ["id"] = jobId });
        }

        public static void RemoveBusinessKey(this IStorageConnection connection, string businessKey)
        {
            using var t = connection.CreateWriteTransaction();
            t.RemoveHash(businessKey);
            t.Commit();
        }

        public static string GetJobIdByBusinessKey(this IStorageConnection connection, string businessKey)
        {
            var hash = connection.GetAllEntriesFromHash(linkHashKey + businessKey);
            if (hash == null || !hash.ContainsKey("id"))
                return null;

            return hash["id"];
        }
    }
}