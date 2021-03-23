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
                    /*try
                    {
                        updateHandler.HandleUpdateAsync(result);
                    }
                    catch (NotImplementedException ex)
                    {
                        // ignored
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error encountered in bulk event handlers!");
                    }*/
                }
            }
            _cache.SetLatencySet(latencySets);
            TryRun(() => Task.WaitAll(_updateHandlers.Select(uh => uh.HandleAllAsync(latencySets)).ToArray()));

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