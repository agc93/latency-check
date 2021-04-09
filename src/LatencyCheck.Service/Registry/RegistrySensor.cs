using System;
using System.Linq;
using Microsoft.Win32;

namespace LatencyCheck.Service.Registry
{
    public class RegistrySensor : IDisposable
    {
        private const string basePath = "Software\\HWiNFO64\\Sensors\\Custom";
        private string _keyPath;
        private readonly RegistryKey _sensorKey;
        private readonly string _sensorName;
        private readonly RegistryKey _baseKey;

        public RegistrySensor(string sensorName)
        {
            _sensorName = "LCS " + sensorName.Trim();
            _baseKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(basePath);
            _keyPath = $"{basePath}\\{_sensorName}";
            if (_baseKey.GetSubKeyNames().Contains(_sensorName))
            {
                _baseKey.DeleteSubKeyTree(_sensorName);
            }
            _sensorKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_keyPath);
            var idx = 0;
            SetKey(ref idx, "Average", 0);
            SetKey(ref idx, "Maximum", 0);
        }

        public void SetSensorValue(ProcessConnectionSet payload)
        {
            var allConnections = payload.SelectMany(p => p.Value).ToList();
            var avg = allConnections.Average(c => c.Smoothed);
            var max = allConnections.Max(c => c.Max).ToInt64();
            var idx = 0;
            SetKey(ref idx, "Average", Convert.ToInt64(avg));
            SetKey(ref idx, "Maximum", max);
            foreach (var (process, connections) in payload)
            {
                var connectionList = connections.ToList();
                for (var i = 0; i < connectionList.Count; i++)
                {
                    var processConnection = connectionList[i];
                    SetKey(ref idx, $"Process {i} Latency", processConnection.Smoothed.ToInt64());
                }
            }
            //TODO: remove keys past the current 'i' for a specific sensor
            
        }

        private void SetKey(ref int index, string name, long value, string unit = "ms") {
            var counterKey = _sensorKey.CreateSubKey($"Other{index}");
            counterKey.SetValue("Name", name, RegistryValueKind.String);
            counterKey.SetValue("Value", value, RegistryValueKind.QWord);
            counterKey.SetValue("Unit", unit, RegistryValueKind.String);
            index++;
        }

        protected virtual void Dispose(bool disposing)
        {
            _baseKey.DeleteSubKeyTree(_sensorName);
            _sensorKey?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}