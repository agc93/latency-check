using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        private PerformanceCounter GetCounter(string counterName, string instanceName) {
            var counter = new PerformanceCounter(CategoryName, GetCounterName(counterName), instanceName)
                {ReadOnly = false};
            return counter;
        }

        private ProcessSet _processNames { get; set; }

        public CounterCategoryClient CreateIfNotExists()
        {
            if (!PerformanceCounterCategory.Exists(CategoryName))
            {
                var customCat = new PerformanceCounterCategory(CategoryName);
                var creationData = new CounterCreationDataCollection(GetCreationData().ToArray());
                PerformanceCounterCategory.Create(customCat.CategoryName, CategoryHelp, Type, creationData);
            }
            return this;
        }

        [Obsolete("Seems to cause problems because Windows is a confusing mess", false)]
        public CounterCategoryClient CreateForPayload(IEnumerable<ProcessConnectionSet> payload) {
            if (PerformanceCounterCategory.Exists(CategoryName)) {
                try {
                    PerformanceCounterCategory.Delete(CategoryName);
                }
                catch (Exception ex) {
                    //ignored
                    Console.WriteLine(ex.Message);
                }
                
            }
            var customCat = new PerformanceCounterCategory(CategoryName);
            PerformanceCounterCategory.Create(CategoryName, CategoryHelp, Type,
                new CounterCreationDataCollection(GetCreationData(payload)));
            return this;
        }

        public CounterCategoryClient DeleteCategory() {
            if (PerformanceCounterCategory.Exists(CategoryName)) {
                PerformanceCounterCategory.Delete(CategoryName);
            }
            return this;
        }
        
        private string GetCounterName(string processName) => $"{processName} Latency";

        private IEnumerable<KeyValuePair<string, long>> GetInstances(ProcessConnectionSet payload, Func<TcpConnectionInfo, uint> connectionFn) {
            foreach (var (process, connections) in payload)
            {
                var connectionList = connections.ToList();
                if (connectionList.Count > 1) {
                    for (int i = 0; i < connectionList.Count; i++) {
                        var connectionInfo = connectionList[i];
                        yield return new KeyValuePair<string,long>($"{process.Name}:{process.Id}:{i}", connectionFn(connectionInfo));
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
            var enabledPayload = payload
                .Where(cs => cs.Values.Any(v => v.Any()))
                .Where(set => set.Keys.Any(k => _processNames.ContainsProcess(k)));
            foreach (var connectionSet in enabledPayload) {
                var allConnections = connectionSet.SelectMany(p => p.Value).ToList();
                var avg = allConnections.Average(c => c.RTT);
                var max = allConnections.Max(c => c.Max).ToInt();
                var avgCounter = GetCounter("Average", connectionSet.GetProcessName());
                avgCounter.RawValue = Convert.ToInt64(avg);
                var maxCounter = GetCounter("Maximum", connectionSet.GetProcessName());
                maxCounter.RawValue = Convert.ToInt64(max);
                var current = GetInstances(connectionSet, c => c.RTT);
                foreach (var (instanceName, value) in current) {
                    var c = GetCounter("Current", instanceName);
                    c.RawValue = value;
                }
                var average = GetInstances(connectionSet, c => c.Smoothed);
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
        }

        [Obsolete("Seems to cause problems because Windows is a confusing mess", false)]
        private CounterCreationData[] GetCreationData(IEnumerable<ProcessConnectionSet> payload) {
            var allCounters = new List<List<CounterCreationData>>();
            allCounters.Add(new List<CounterCreationData> {
                new CounterCreationData(GetCounterName("Average"), GetCounterName("Average"), PerformanceCounterType.ElapsedTime),
                new CounterCreationData(GetCounterName("Maximum"), GetCounterName("Maximum"), PerformanceCounterType.ElapsedTime),
            });
            foreach (var connectionSet in payload) {
                var instances = GetInstances(connectionSet, c => c.Smoothed);
                allCounters.Add(instances.Select(i => new CounterCreationData(connectionSet.GetProcessName(), connectionSet.GetProcessName(), PerformanceCounterType.ElapsedTime)).ToList());
            }

            var creationData = allCounters.SelectMany(ac => ac).ToArray();
            return creationData;
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
    public class PerformanceCounterClient : IDisposable
    {
        public PerformanceCounterClient(CustomCounterCategory category, string processName) {
            CustomCategory = category;
            ProcessName = processName;
        }

        private readonly CustomCounterCategory CustomCategory;
        public string ProcessName { get; }
        public string CategoryName => CustomCategory.CategoryName;
        public string CategoryHelp => CustomCategory.CategoryHelp;
        
        public PerformanceCounterCategoryType Type { get; init; } = PerformanceCounterCategoryType.MultiInstance;
        public PerformanceCounterType CounterType { get; init; } = PerformanceCounterType.ElapsedTime;
        
        public IEnumerable<CounterCreationData> GetCreationData()
        {
            var counters = new List<CustomCounter> {
                new()
                {
                    CounterName = "Average Latency",
                    CounterHelp = $"The average latency for all connections.",
                }, 
                new() {
                    CounterName = "Maximum Latency",
                    CounterHelp = $"The highest latency for all connections."
                },
                new () {
                    CounterName = "Current",
                    CounterHelp = "The current latency for all connections"
                }
            };
            var baseData = counters.Select(c => new CounterCreationData(c.CounterName, c.CounterHelp, c.Type)).ToArray();
            return baseData;
        }



        public PerformanceCounterCategory CreateIfNotExists()
        {
            if (!PerformanceCounterCategory.Exists(CategoryName))
            {
                var customCat = new PerformanceCounterCategory(CategoryName);
                PerformanceCounterCategory.Create(customCat.CategoryName, CategoryHelp, Type,
                    new CounterCreationDataCollection(GetCreationData().ToArray()));
                return customCat;
            }
            return Category;
        }

        public PerformanceCounterCategory Recreate() {
            if (PerformanceCounterCategory.Exists(CategoryName)) {
                PerformanceCounterCategory.Delete(CategoryName);
            }
            return CreateIfNotExists();
        }

        public void SetNoValue() {
            return;
            var counters = Category.GetCounters();
            var processCounters = counters.Where(c =>
                c.InstanceName.Split(':').First().Equals(ProcessName, StringComparison.CurrentCultureIgnoreCase));
            foreach (var processCounter in processCounters)
            {
                processCounter.RawValue = 0;
            }
        }

        private PerformanceCounter GetCounter(string counterName, string instanceName = null) {

            var counter = new PerformanceCounter(CategoryName, GetCounterName(counterName), instanceName ?? ProcessName)
                {ReadOnly = false};
            return counter;
        }

        public void UpdateCounters(ProcessConnectionSet connectionSet)
        {
            var allConnections = connectionSet.SelectMany(p => p.Value).ToList();
            var avg = allConnections.Average(c => c.RTT);
            var max = allConnections.Max(c => c.Max).ToInt();
            var idx = 0;
            var avgCounter = GetCounter("Average");
            avgCounter.RawValue = Convert.ToInt64(avg);
            var maxCounter = GetCounter("Maximum");
            maxCounter.RawValue = Convert.ToInt64(max);
            foreach (var (process, connections) in connectionSet)
            {
                var connectionList = connections.ToList();
                if (connectionList.Count > 1)
                {
                    foreach (var connectionInfo in connectionList)
                    {
                        var instanceName = $"{process.Name}:{process.Id}:{connectionInfo.LocalPort}";
                        var rowCounter = GetCounter(process.Name, instanceName);
                        rowCounter.RawValue = connectionInfo.Smoothed.ToInt();
                    }    
                }
                else if (connectionList.Count == 1)
                {
                    var connectionInfo = connectionList.First();
                    var instanceName = $"{process.Name}:{process.Id}";
                    var rawCounter = GetCounter(process.Name, instanceName);
                    rawCounter.RawValue = connectionInfo.Smoothed.ToInt();
                }
            }
        }

        private PerformanceCounterCategory __category;

        private PerformanceCounterCategory Category
        {
            get
            {
                __category ??= PerformanceCounterCategory.GetCategories()
                    .FirstOrDefault(c => c.CategoryName == CategoryName);
                return __category;
            }
        }

        private string GetCounterName(string processName) => $"{processName} Latency";

        public void Dispose()
        {
            PerformanceCounterCategory.Delete(CategoryName);
            GC.SuppressFinalize(this);
        }
    }
}