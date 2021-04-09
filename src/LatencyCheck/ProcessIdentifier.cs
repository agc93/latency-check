using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LatencyCheck
{
    public class ProcessIdentifier : IComparable<ProcessIdentifier> {
        public string Name {get;init;}
        public int? Id {get;init;}

        public int CompareTo(ProcessIdentifier other)
        {
            return Name == other.Name
                ? (Id ?? 0).CompareTo(other.Id ?? 0)
                : Name.CompareTo(other.Name);
        }

        public override string ToString()
        {
            return $"{Name}:{(Id?.ToString() ?? "???")}";
        }

        public class ProcessComparer : IEqualityComparer<ProcessIdentifier>
        {
            public bool Equals(ProcessIdentifier x, ProcessIdentifier y)
            {
                return x.Id == y.Id && x.Name == y.Name;
            }

            public int GetHashCode([DisallowNull] ProcessIdentifier obj)
            {
                return (obj.Id ?? 1369) ^ obj.Name.GetHashCode();
            }
        }
    }

    [Obsolete("Currently unstable")]
    public class ProcessFamily
    {
        public ProcessFamily(string processName)
        {
            ExecutableName = processName;
        }
        public string ExecutableName { get; set; }

        public IEnumerable<ProcessIdentifier> Processes =>
            _processes.Select(p => new ProcessIdentifier {Name = p.ProcessName, Id = p.Id});

        private List<System.Diagnostics.Process> _processes { get; set; } = new List<Process>();

        public IEnumerable<System.Diagnostics.Process> GetRawProcesses()
        {
            return _processes;
        }
    }
}