using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LatencyCheck.Service.Counters
{
    public class CustomCounter
    {
        public string CounterName { get; init; }
        public string CounterHelp { get; init; }
        public PerformanceCounterType Type { get; init; } = PerformanceCounterType.ElapsedTime;
        public List<string> Instances { get; init; }

        public IEnumerable<PerformanceCounter> ToCounters(string categoryName) {
            return Instances.Any()
                ? Instances.Select(i => new PerformanceCounter(categoryName, CounterName, i, false))
                : new[] {new PerformanceCounter(categoryName, CounterName)};
        }
    }
}