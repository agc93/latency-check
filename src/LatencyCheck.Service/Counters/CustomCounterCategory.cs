using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LatencyCheck.Service.Counters
{
    public class CustomCounterCategory
    {
        public string CategoryName { get; init; } = "LatencyCheck";
        public string CategoryHelp { get; init; } = "LatencyCheck-powered process latency counters.";
        
        public PerformanceCounterCategoryType Type { get; init; } = PerformanceCounterCategoryType.MultiInstance;

        private CounterCreationDataCollection GetCounterData(IEnumerable<CustomCounter> counters)
        {
            var data = counters.Select(c => new CounterCreationData(c.CounterName, c.CounterHelp, c.Type)).ToArray();
            return new CounterCreationDataCollection(data);
        }

        public CounterCreationDataCollection GetCreationData(string processName)
        {
            var counters = new List<CustomCounter> {
                new()
                {
                        CounterName = "Average Latency",
                        CounterHelp = $"The average latency for all connections from {processName}."
                    }, 
                new()
                    {
                        CounterName = "Maximum Latency",
                        CounterHelp = $"The highest latency for all connections from {processName}."
                    }
            };
            return GetCounterData(counters);
        }
    }
}