using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LatencyCheck.Service.RunHelpers;

namespace LatencyCheck.Service
{
    public class LatencyCheckWorker : IHostedService, IDisposable
    {
        private readonly ILogger<LatencyCheckWorker> _logger;
        private readonly IEnumerable<ProcessConnectionClient> _clients;
        private Timer _timer;
        private Timer _reloadTimer;
        private IEnumerable<IUpdateHandler> _updateHandlers;
        private WorkerOptions _opts;

        public LatencyCheckWorker(ILogger<LatencyCheckWorker> logger, IEnumerable<ProcessConnectionClient> clients, IMemoryCache cache, IEnumerable<IUpdateHandler> updateHandlers, IOptions<WorkerOptions> workerOpts)
        {
            _logger = logger;
            _clients = clients;
            _updateHandlers = updateHandlers;
            _opts = workerOpts?.Value ?? new WorkerOptions();
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Worker Service running.");
            RefreshProcesses();

            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromSeconds(_opts.Interval));

            _reloadTimer = new Timer(Refresh, null, TimeSpan.Zero, TimeSpan.FromSeconds(_opts.ReloadInterval));

            return Task.CompletedTask;
        }

        private void Refresh(object state) {
            _logger.LogDebug(
                "Refreshing tracked PIDs for {0} clients", _clients.Count());
            RefreshProcesses();
            
        }

        private void RefreshProcesses() {
            foreach (var client in _clients)
            {
                client.RefreshPids();
            }
        }

        private void DoWork(object state)
        {
            var latencySets = new List<ProcessConnectionSet>();
            foreach (var client in _clients)
            {
                _logger.LogTrace($"Checking latency for {client?.ExecutableName}");
                var result = client.GetOnce();
                latencySets.Add(result);
                foreach (var updateHandler in _updateHandlers)
                {
                    _logger.LogTrace($"Running event handlers for {updateHandler.GetType().ToString()}");
                    TryRun(async () => await updateHandler.HandleUpdateAsync(result), (ex) => _logger.LogError(ex, "Error in event handler!"));
                }
            }
            _logger.LogTrace($"Running {_updateHandlers.Count()} bulk update handlers.");
            TryRun(() => Task.WaitAll(_updateHandlers.Select(uh => uh.HandleAllAsync(latencySets)).ToArray()), (ex) => _logger.LogError(ex, "Error in event handler!"));

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