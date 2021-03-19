using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
}