using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static LatencyCheck.Service.RunHelpers;

namespace LatencyCheck.Service
{
    public class LatencyCheckWorker : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<LatencyCheckWorker> _logger;
        private readonly IEnumerable<ProcessConnectionClient> _clients;
        private readonly IMemoryCache _cache;
        private Timer _timer;
        private Timer _reloadTimer;
        private IEnumerable<IUpdateHandler> _updateHandlers;

        public LatencyCheckWorker(ILogger<LatencyCheckWorker> logger, IEnumerable<ProcessConnectionClient> clients, IMemoryCache cache, IEnumerable<IUpdateHandler> updateHandlers)
        {
            _logger = logger;
            _clients = clients;
            _cache = cache;
            _updateHandlers = updateHandlers;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Worker Service running.");
            await RefreshProcessesAsync();

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromSeconds(4));

            _reloadTimer = new Timer(RefreshAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        private async void RefreshAsync(object state) {
            _logger.LogDebug(
                "Refreshing tracked PIDs for {0} clients", _clients.Count());
            await RefreshProcessesAsync();
            
        }

        private async Task RefreshProcessesAsync() {
            foreach (var client in _clients)
            {
                await client.RefreshPidsAsync();
            }
        }

        async void DoWork(object state)
        {
            var latencySets = new List<ProcessConnectionSet>();
            foreach (var client in _clients)
            {
                var result = client.GetOnce();
                latencySets.Add(result);
                foreach (var updateHandler in _updateHandlers)
                {
                    TryRun(async () => await updateHandler.HandleUpdateAsync(result));
                }
            }
            _cache.SetLatencySet(latencySets);
            TryRun(() => Task.WaitAll(_updateHandlers.Select(uh => uh.HandleAllAsync(latencySets)).ToArray()));

            // _cache.Set(CacheKeys.LatencySet, latencySets);

            _logger.LogDebug(
                "Latency Check completed for {0} processes", latencySets.Count);
        }

        

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Worker Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            _reloadTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _reloadTimer?.Dispose();
        }
    }
}