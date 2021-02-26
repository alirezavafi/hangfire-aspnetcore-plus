using System.Collections.Generic;
using Hangfire.Storage;

namespace Hangfire
{
    public static class HangfireJobKeyExtensions
    {
        private const string linkHashKey = "x-backgroundjob-keys:";

        public static void SetJobKey(this IStorageConnection connection, string id, string key)
        {
            connection.SetRangeInHash(linkHashKey + key, new Dictionary<string, string>() { ["id"] = id });
        }

        public static void RemoveJobKey(this IStorageConnection connection, string key)
        {
            using (var t = connection.CreateWriteTransaction())
            {
                t.RemoveHash(key);
                t.Commit();
            }
        }

        public static string GetJobIdByKey(this IStorageConnection connection, string key)
        {
            var hash = connection.GetAllEntriesFromHash(linkHashKey + key);
            if (hash == null || !hash.ContainsKey("id"))
            {
                return null;
            }

            return hash["id"];
        }
    }
}