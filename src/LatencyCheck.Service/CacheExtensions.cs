using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LatencyCheck.Service
{
    public static class CacheExtensions
    {
        public static void SetLatencySet(this IMemoryCache cache, List<ProcessConnectionSet> connections) {
            var entry = new MemoryCacheEntryOptions {
                SlidingExpiration = TimeSpan.FromSeconds(2),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            };
            cache.Set(CacheKeys.LatencySet, connections, entry);
        }

        public static List<ProcessConnectionSet> GetLatencySet(this IMemoryCache cache) {
            return cache.TryGetValue<List<ProcessConnectionSet>>(CacheKeys.LatencySet, out var set) ? set : null;
        }

        public static ProcessConnectionSet MatchToClient(
            this List<ProcessConnectionSet> latencySet,
            ProcessConnectionClient client)
        {
            return latencySet.MatchToExecutableName(client.ExecutableName);
        }

        public static ProcessConnectionSet MatchToProcess(
            this List<ProcessConnectionSet> latencySet,
            ProcessIdentifier ident)
        {
            return latencySet.MatchToExecutableName(ident.Name);
        }

        private static ProcessConnectionSet MatchToExecutableName(
            this IEnumerable<ProcessConnectionSet> latencySet,
            string executableName)
        {
            return latencySet.FirstOrDefault(ls => ls.Keys.Any()
                                                   && ls.Keys.First().Name is var processName
                                                   && (processName == executableName || Path.GetFileNameWithoutExtension(processName) ==
                                                       Path.GetFileNameWithoutExtension(executableName)));
        }
    }
}