using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LatencyCheck.Service.Registry
{
    public class RegistryUpdateHandler : IUpdateHandler
    {
        private List<string> _processNames;
        private readonly ILogger<RegistryUpdateHandler> _logger;

        public RegistryUpdateHandler(ILogger<RegistryUpdateHandler> logger, IEnumerable<ProcessConnectionClient> clients, IConfiguration configuration)
        {
            var section = configuration.GetSection("HWiNFO");
            if (section.Exists() && section.Get<List<string>>() is var processNames && processNames.Any())
            {
                _processNames = processNames.Select(Path.GetFileNameWithoutExtension).ToList();
            }
            else
            {
                _processNames = clients.Select(c => Path.GetFileNameWithoutExtension(c.ExecutableName)).ToList();
            }

            _logger = logger;
            logger.LogDebug($"Setting up registry sensors for {_processNames.Count} processes.");
            Sensors = _processNames.ToDictionary(pn => pn, pn => new RegistrySensor(pn));
            // Clients = clients.Select(c => (c, new RegistrySensor(Path.GetFileNameWithoutExtension(c.ExecutableName)))).ToList();
        }

        private Dictionary<string, RegistrySensor> Sensors { get; set; }

        // private List<(ProcessConnectionClient Client, RegistrySensor Sensor)> Clients { get; }

        public Task HandleUpdateAsync(ProcessConnectionSet payload)
        {
            if (payload != null && Sensors.Any())
            {
                _logger.LogTrace($"Valid payload with configured sensors: updating registry sensors.");
                if (Sensors.TryGetValue(payload.GetProcessName() ?? "", out var sensor))
                {
                    _logger.LogTrace($"Running sensor update for '{payload.GetProcessName()}'");
                    sensor.SetSensorValue(payload);
                }
            }
            return Task.CompletedTask;
        }
    }
}