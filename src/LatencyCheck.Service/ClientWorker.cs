using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LatencyCheck.Service
{
    public abstract class ClientWorker : IHostedService, IDisposable
    {
        public ClientWorker(ILogger<ClientWorker> logger, IEnumerable<ProcessConnectionClient> clients, IMemoryCache cache)
        {
            _logger = logger;
            _clients = clients;
            _cache = cache;
            RefreshCallback = RefreshAsync;
        }
        private Timer _timer;
        private Timer _reloadTimer;
        protected readonly ILogger<ClientWorker> _logger;
        protected readonly IEnumerable<ProcessConnectionClient> _clients;
        protected readonly IMemoryCache _cache;
        protected TimerCallback WorkCallback {get;set;}
        protected TimerCallback RefreshCallback {get;set;}
        protected int WorkInterval {get;set;} = 4;
        protected int RefreshInterval {get;set;} = 10;

        public void Dispose() {
            _timer?.Dispose();
            _reloadTimer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            if (WorkCallback != null) {
                _timer = new Timer(WorkCallback, null, TimeSpan.Zero, 
                    TimeSpan.FromSeconds(WorkInterval));
            }
            if (RefreshCallback != null) {
                _reloadTimer = new Timer(RefreshCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(RefreshInterval));
            }
            

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            _reloadTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private async void RefreshAsync(object state) {
            foreach (var client in _clients)
            {
                await client.RefreshPidsAsync();
            }
        }
    }
}