using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        
        public LatencyCheckWorker(ILogger<LatencyCheckWorker> logger, IEnumerable<ProcessConnectionClient> clients, IMemoryCache cache)
        {
            _logger = logger;
            _clients = clients;
            _cache = cache;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromSeconds(4));

            _reloadTimer = new Timer(RefreshAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private async void RefreshAsync(object state) {
            foreach (var client in _clients)
            {
                await client.RefreshPidsAsync();
            }
        }

        private void DoWork(object state)
        {
            var latencySets = _clients.Select(c => c.GetOnce()).ToList();
            _cache.SetLatencySet(new List<ProcessConnectionSet>(latencySets));
            // _cache.Set(CacheKeys.LatencySet, latencySets);

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {0}", latencySets.Count);
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

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