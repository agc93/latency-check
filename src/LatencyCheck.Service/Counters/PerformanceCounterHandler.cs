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
            logger.LogDebug($"Setting up perf counters for {_processNames.Count} processes.");
            _logger = logger;
            _category = new CounterCategoryClient(result) {Logger = logger};
            _category.CreateIfNotExists();
        }

        public async Task HandleUpdateAsync(ProcessConnectionSet payload)
        {
            return;
        }

        public Task HandleAllAsync(List<ProcessConnectionSet> payload)
        {
            try {
                _category.TryCreateIfNotExists().Update(payload);
            }
            catch (Exception e) {
                _logger.LogError(e, "Error encountered while updating performance counters!");
                throw;
            }
            return Task.CompletedTask;
        }
    }
}