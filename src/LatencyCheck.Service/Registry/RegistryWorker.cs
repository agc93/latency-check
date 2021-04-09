using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LatencyCheck.Service.Registry
{
    public class RegistryWorker: ClientWorker
    {
        public RegistryWorker(ILogger<RegistryWorker> logger, IEnumerable<ProcessConnectionClient> clients, IMemoryCache cache) : base(logger, clients, cache)
        {
            WorkCallback = DoWork;
            RefreshCallback = RefreshAsync;
            Clients = clients.Select(c => (c, new RegistrySensor(Path.GetFileNameWithoutExtension((string?) c.ExecutableName)))).ToList();
        }
        
        private List<(ProcessConnectionClient Client, RegistrySensor Sensor)> Clients { get; }

        private void DoWork(object state) {
            var sets = _cache.GetLatencySet();
            if (sets != null && Clients.Any())
            {
                foreach (var (processClient, sensor) in Clients)
                {
                    var cacheResponse = sets.MatchToClient(processClient);
                    if (cacheResponse != null)
                    {
                        sensor.SetSensorValue(cacheResponse);
                    }
                }
            }
        }
        
        private static void RefreshAsync(object state) {
            
        }

        
    }
}