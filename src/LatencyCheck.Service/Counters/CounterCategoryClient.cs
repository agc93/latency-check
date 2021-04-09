using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace LatencyCheck.Service.Counters
{
    public class CounterCategoryClient : IDisposable
    {
        public CounterCategoryClient(ProcessSet processSet) {
            _processNames = processSet;
        }

        public string CategoryName { get; init; } = "LatencyCheck";
        public string CategoryHelp { get; init; } = "LatencyCheck-powered process latency counters.";
        public PerformanceCounterCategoryType Type { get; init; } = PerformanceCounterCategoryType.MultiInstance;
        
        public ILogger Logger { get; init; }

        private PerformanceCounter GetCounter(string counterName, string instanceName) {
            var counter = new PerformanceCounter(CategoryName, GetCounterName(counterName), instanceName)
                {ReadOnly = false};
            return counter;
        }

        private ProcessSet _processNames { get; set; }

        public CounterCategoryClient CreateIfNotExists() {
            if (!PerformanceCounterCategory.Exists(CategoryName)) {
                Logger?.LogInformation("Performance Counter Category not found. Creating!");
                var customCat = new PerformanceCounterCategory(CategoryName);
                var creationData = new CounterCreationDataCollection(GetCreationData().ToArray());
                PerformanceCounterCategory.Create(customCat.CategoryName, CategoryHelp, Type, creationData);
            }

            return this;
        }

        public CounterCategoryClient TryCreateIfNotExists() {
            try {
                CreateIfNotExists();
            }
            catch (Exception e) {
                Logger?.LogError(e, "Error while creating custom counter category.");
                //ignored
            }

            return this;
        }

        public CounterCategoryClient DeleteCategory() {
            if (PerformanceCounterCategory.Exists(CategoryName)) {
                PerformanceCounterCategory.Delete(CategoryName);
            }

            return this;
        }

        private string GetCounterName(string processName) => $"{processName} Latency";

        private IEnumerable<KeyValuePair<string, long>> GetInstances(ProcessConnectionSet payload,
            Func<TcpConnectionInfo, uint> connectionFn) {
            foreach (var (process, connections) in payload) {
                var connectionList = connections.ToList();
                if (connectionList.Count > 1) {
                    for (int i = 0; i < connectionList.Count; i++) {
                        var connectionInfo = connectionList[i];
                        yield return new KeyValuePair<string, long>($"{process.Name}:{process.Id}:{i}",
                            connectionFn(connectionInfo));
                    }
                }
                else if (connectionList.Count == 1) {
                    var connectionInfo = connectionList.First();
                    var instanceName = $"{process.Name}:{process.Id}";
                    yield return new KeyValuePair<string, long>(instanceName, connectionFn(connectionInfo));
                }
            }
        }

        public void Update(IEnumerable<ProcessConnectionSet> payload) {
            Logger?.LogTrace("Updating performance counters for payload.");
            var enabledPayload = payload
                .Where(cs => cs.Values.Any(v => v.Any()))
                .Where(set => set.Keys.Any(k => _processNames.ContainsProcess(k, true)));
            foreach (var connectionSet in enabledPayload) {
                Update(connectionSet);
            }
        }

        public void Update(ProcessConnectionSet connectionSet) {
            Logger?.LogTrace($"Updating sensors for {connectionSet.GetProcessName()}");
            var allConnections = connectionSet.SelectMany(p => p.Value).ToList();
            var avg = allConnections.Average(c => c.Smoothed);
            var max = allConnections.Max(c => c.Max).ToInt64();
            var avgCounter = GetCounter("Average", connectionSet.GetProcessName());
            avgCounter.RawValue = Convert.ToInt64(avg);
            var maxCounter = GetCounter("Maximum", connectionSet.GetProcessName());
            maxCounter.RawValue = Convert.ToInt64(max);
            var current = GetInstances(connectionSet, c => c.Smoothed);
            foreach (var (instanceName, value) in current) {
                Logger?.LogTrace($"Updating current values for {instanceName}");
                var c = GetCounter("Current", instanceName);
                c.RawValue = value;
            }

            var average = GetInstances(connectionSet, c => c.Mean);
            foreach (var (instanceName, value) in average) {
                var c = GetCounter("Average", instanceName);
                c.RawValue = value;
            }

            var maximums = GetInstances(connectionSet, c => c.Max);
            foreach (var (instanceName, value) in maximums) {
                var c = GetCounter("Maximum", instanceName);
                c.RawValue = value;
            }
        }

        private CounterCreationData[] GetCreationData() {
            var allCounters = new List<CounterCreationData>();
            allCounters.AddRange(new List<CounterCreationData> {
                new CounterCreationData(
                    GetCounterName("Average"),
                    $"The average latency for all connections.",
                    PerformanceCounterType.NumberOfItems64),
                new CounterCreationData(
                    GetCounterName("Maximum"),
                    $"The highest latency for all connections.",
                    PerformanceCounterType.NumberOfItems64),
                new CounterCreationData(
                    GetCounterName("Current"),
                    $"The current latency for all connections.",
                    PerformanceCounterType.NumberOfItems64)
            });
            return allCounters.ToArray();
        }

        public void Dispose() {
            DeleteCategory();
        }
    }
}