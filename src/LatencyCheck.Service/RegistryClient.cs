using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace LatencyCheck.Service
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
            _baseKey = Registry.CurrentUser.CreateSubKey(basePath);
            _keyPath = $"{basePath}\\{_sensorName}";
            if (_baseKey.GetSubKeyNames().Contains(_sensorName))
            {
                _baseKey.DeleteSubKeyTree(_sensorName);
            }
            _sensorKey = Registry.CurrentUser.CreateSubKey(_keyPath);
            var idx = 0;
            SetKey(ref idx, "Average", 0);
            SetKey(ref idx, "Maximum", 0);
        }

        private void SetSensorValue(int index, TcpConnectionInfo info) {
            SetKey(ref index, "Min", info.Min.ToInt());
            SetKey(ref index, "Max", info.Max.ToInt());
            SetKey(ref index, "Avg", info.Avg.ToInt());
            SetKey(ref index, "Smoothed", info.Smoothed.ToInt());
        }

        public void SetSensorValue(ProcessConnectionSet payload)
        {
            var allConnections = payload.SelectMany(p => p.Value).ToList();
            var avg = allConnections.Average(c => c.Smoothed);
            var max = allConnections.Max(c => c.Max).ToInt();
            var idx = 0;
            SetKey(ref idx, "Average", Convert.ToInt32(avg));
            SetKey(ref idx, "Maximum", max);
            foreach (var (process, connections) in payload)
            {
                var connectionList = connections.ToList();
                for (var i = 0; i < connectionList.Count; i++)
                {
                    var processConnection = connectionList[i];
                    SetKey(ref idx, $"Process {i} Latency", processConnection.Smoothed.ToInt());
                }
            }
            
        }

        private void SetKey(ref int index, string name, int value, string unit = "ms") {
            var counterKey = _sensorKey.CreateSubKey($"Other{index}");
            counterKey.SetValue("Name", name, RegistryValueKind.String);
            counterKey.SetValue("Value", value, RegistryValueKind.DWord);
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