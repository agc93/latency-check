using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace LatencyCheck.Service
{
    public class CacheUpdateHandler : IUpdateHandler
    {
        private readonly IMemoryCache _cache;

        public CacheUpdateHandler(IMemoryCache cache) {
            _cache = cache;
        }

        public async Task HandleUpdateAsync(ProcessConnectionSet payload) {
            return;
        }

        public async Task HandleAllAsync(List<ProcessConnectionSet> payload) {
            _cache.SetLatencySet(payload);
        }
    }
}