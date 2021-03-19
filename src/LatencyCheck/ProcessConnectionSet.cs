using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LatencyCheck
{
    public class ProcessConnectionSet : IDictionary<ProcessIdentifier, IEnumerable<TcpConnectionInfo>>
    {
        private IDictionary<ProcessIdentifier, IEnumerable<TcpConnectionInfo>> _dictionaryImplementation;
        public IEnumerator<KeyValuePair<ProcessIdentifier, IEnumerable<TcpConnectionInfo>>> GetEnumerator()
        {
            return _dictionaryImplementation.GetEnumerator();
        }

        internal ProcessConnectionSet(IDictionary<ProcessIdentifier, IEnumerable<TcpConnectionInfo>> payload)
        {
            var ordered = payload
                .OrderBy(o => o.Key)
                .ToDictionary(k => k.Key, v => v.Value);
            _dictionaryImplementation = ordered;
        }

        internal ProcessConnectionSet()
        {
            _dictionaryImplementation = new Dictionary<ProcessIdentifier, IEnumerable<TcpConnectionInfo>>();
        }
        
        public ProcessConnectionSet(IEnumerable<TcpConnectionInfo> connections, IEnumerable<Process> processes)
        {
            var dict = connections
                .GroupBy(g => g.ProcessId)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    k => new ProcessIdentifier
                        {Id = k.Key, Name = processes.FirstOrDefault(p => p.Id == k.Key)?.ProcessName},
                    v => v.Cast<TcpConnectionInfo>(), new ProcessIdentifier.ProcessComparer());
            _dictionaryImplementation = dict;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _dictionaryImplementation).GetEnumerator();
        }

        public void Add(KeyValuePair<ProcessIdentifier, IEnumerable<TcpConnectionInfo>> item)
        {
            _dictionaryImplementation.Add(item);
        }

        public void Clear()
        {
            _dictionaryImplementation.Clear();
        }

        public bool Contains(KeyValuePair<ProcessIdentifier, IEnumerable<TcpConnectionInfo>> item)
        {
            return _dictionaryImplementation.Contains(item);
        }

        public void CopyTo(KeyValuePair<ProcessIdentifier, IEnumerable<TcpConnectionInfo>>[] array, int arrayIndex)
        {
            _dictionaryImplementation.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<ProcessIdentifier, IEnumerable<TcpConnectionInfo>> item)
        {
            return _dictionaryImplementation.Remove(item);
        }

        public int Count => _dictionaryImplementation.Count;

        public bool IsReadOnly => _dictionaryImplementation.IsReadOnly;

        public void Add(ProcessIdentifier key, IEnumerable<TcpConnectionInfo> value)
        {
            _dictionaryImplementation.Add(key, value);
        }

        public bool ContainsKey(ProcessIdentifier key)
        {
            return _dictionaryImplementation.ContainsKey(key);
        }

        public bool Remove(ProcessIdentifier key)
        {
            return _dictionaryImplementation.Remove(key);
        }

        public bool TryGetValue(ProcessIdentifier key, out IEnumerable<TcpConnectionInfo> value)
        {
            return _dictionaryImplementation.TryGetValue(key, out value);
        }

        public IEnumerable<TcpConnectionInfo> this[ProcessIdentifier key]
        {
            get => _dictionaryImplementation[key];
            set => _dictionaryImplementation[key] = value;
        }

        public ICollection<ProcessIdentifier> Keys => _dictionaryImplementation.Keys;

        public ICollection<IEnumerable<TcpConnectionInfo>> Values => _dictionaryImplementation.Values;
    }

    public static class ProcessExtensions {
        public static ProcessConnectionSet ToConnections(this Dictionary<ProcessIdentifier, IEnumerable<TcpConnectionInfo>> dict) {
            return new ProcessConnectionSet(dict);
        }
    }
}