using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace LatencyCheck.Service
{
    public interface IUpdateHandler
    {
        Task HandleUpdateAsync(ProcessConnectionSet payload);
        Task HandleAllAsync(List<ProcessConnectionSet> payload) => Task.CompletedTask;
    }
    
    /*public class CacheHandler : IUpdateHandler
    {
        public async Task HandleUpdateAsync(ProcessConnectionSet payload)
        {
            throw new System.NotImplementedException();
        }
    }*/

    public class RegistryUpdateHandler : IUpdateHandler
    {
        private List<string> _processNames;

        public RegistryUpdateHandler(IEnumerable<ProcessConnectionClient> clients, IConfiguration configuration)
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
            Sensors = _processNames.ToDictionary(pn => pn, pn => new RegistrySensor(pn));
            // Clients = clients.Select(c => (c, new RegistrySensor(Path.GetFileNameWithoutExtension(c.ExecutableName)))).ToList();
        }

        private Dictionary<string, RegistrySensor> Sensors { get; set; }

        // private List<(ProcessConnectionClient Client, RegistrySensor Sensor)> Clients { get; }

        public Task HandleUpdateAsync(ProcessConnectionSet payload)
        {
            if (payload != null && Sensors.Any())
            {
                if (Sensors.TryGetValue(payload.GetProcessName() ?? "", out var sensor))
                {
                    sensor.SetSensorValue(payload);
                }
            }
            return Task.CompletedTask;
        }

        /*private (ProcessConnectionClient Client, RegistrySensor Sensor)? MatchClient(ProcessConnectionSet ls)
        {
            if (ls.Keys.Any() && ls.Keys.First().Name is var processName)
            {
                return Clients.FirstOrDefault(c =>
                    c.Client.ExecutableName == processName ||
                    Path.GetFileNameWithoutExtension(c.Client.ExecutableName) ==
                    Path.GetFileNameWithoutExtension(processName));
            }

            return null;
        }*/
    }
}