using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LatencyCheck.Service.Counters
{
    public class PerformanceCounterHandler : IUpdateHandler
    {
        private readonly ProcessSet _processNames;
        private ILogger<PerformanceCounterHandler> _logger;
        private readonly CounterCategoryClient _category;

        public PerformanceCounterHandler(ProcessSet set, IConfiguration config, ILogger<PerformanceCounterHandler> logger) {
            var result = set.GetProcessesForSource(config, "PerformanceCounters");
            _processNames = result;
            _logger = logger;
            _category = new CounterCategoryClient(result);
            _category.CreateIfNotExists();
        }

        public async Task HandleUpdateAsync(ProcessConnectionSet payload)
        {
            return;
        }

        public Task HandleAllAsync(List<ProcessConnectionSet> payload)
        {
            _category.CreateIfNotExists().Update(payload);
            return Task.CompletedTask;
        }
    }
}