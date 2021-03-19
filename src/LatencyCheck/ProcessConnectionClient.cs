using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Vanara.PInvoke;
using static Vanara.PInvoke.IpHlpApi;

namespace LatencyCheck
{

    public class ProcessConnectionClient
    {
        private readonly string _executableName;
        private readonly Func<IEnumerable<Process>, IEnumerable<Process>> _filter;

        public static ProcessConnectionClient Create(string processName, Func<IEnumerable<Process>, IEnumerable<Process>> filterFunc = null) {
            filterFunc ??= (allPs) =>
            {
                return allPs;
            };
            var client = new ProcessConnectionClient(processName, filterFunc);
            client.Initialise();
            return client;
        }

        private ProcessConnectionClient(string executableName, Func<IEnumerable<Process>, IEnumerable<Process>> filterFunc)
        {
            _executableName = executableName;
            _filter = filterFunc;
        }

        private void Initialise() {
            var matchingProcesses = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(_executableName));
            _processes = _filter(matchingProcesses).ToList();
            var timer = new Timer(_refreshTimer * 1000);
            timer.Elapsed += new ElapsedEventHandler(OnWindow);
            _timer = timer;
        }

        public async Task<ProcessConnectionClient> RefreshPidsAsync() {
            Initialise();
            return this;
        }

        public ProcessConnectionClient Start() {
            _timer.Start();
            return this;
        }

        public ProcessConnectionClient Stop() {
            _timer.Stop();
            return this;
        }

        public ProcessConnectionSet GetOnce() {
            var dict = GetInfo();
            return dict;
            // return dict.ToDictionary(k => k.Key, v => v.Value.ToList());
        }

        private IEnumerable<TcpConnectionInfo> GetConnectionList() {
            var tcpTable = GetTcpTable(true);
            var ownerTable = GetExtendedTcpTable<MIB_TCPTABLE_OWNER_MODULE>(TCP_TABLE_CLASS.TCP_TABLE_OWNER_MODULE_ALL, Ws2_32.ADDRESS_FAMILY.AF_INET, true);
            System.Threading.Thread.Sleep(1);
            var ownerNames = ownerTable.Select(r => {
                try 
                {
                    var pid = Convert.ToInt32(r.dwOwningPid);
                    var process = _processes.FirstOrDefault(p => p.Id == pid);
                    if (process == null) {
                        return null;
                    }
                    return new ProcessModule { Process = process, Owner = r};
                } 
                catch 
                { 
                    return null; 
                }
            }).Where(pm => !string.IsNullOrWhiteSpace(pm?.Process?.ProcessName)).ToList();
            for (int i = 0; i < ownerNames.Count; ++i) {
                if (ownerNames[i].Owner.dwRemoteAddr.S_addr != 16777343)
                {
                    var row = tcpTable.table.FirstOrDefault(t => t.dwLocalPort == ownerNames[i].Owner.dwLocalPort);
                    Win32Error returnCode = GetPerTcpConnectionEStats(row, TCP_ESTATS_TYPE.TcpConnectionEstatsPath, out object rw, out object ros, out object rod);
                    if (returnCode == Win32Error.NO_ERROR && ((TCP_ESTATS_PATH_ROD_v0)rod).SmoothedRtt > 0) {
                        yield return new TcpConnectionInfo(ownerNames[i].Process, (TCP_ESTATS_PATH_ROD_v0)rod, ownerNames[i].Owner, row);
                    }
                }
            }
        }

        private void OnWindow(object source, ElapsedEventArgs e) {
            var dict = GetInfo();
            if (dict.Any()) {
                _connections = dict;
            }
        }

        private ProcessConnectionSet GetInfo() {
            var list = GetConnectionList().ToList();
            var set = new ProcessConnectionSet(list, _processes);
            return set;
        }

        private ProcessConnectionSet _connections {get;set;} = new ProcessConnectionSet();

        private List<Process> _processes {get; set;} = new List<Process>();
        private int _refreshTimer = 2;
        private Timer _timer;
        public IEnumerable<ProcessIdentifier> Processes
        {
            get
            {
                return _processes.Select(p => new ProcessIdentifier { Id = p.Id, Name = p.ProcessName });
            }
        }

        public string ExecutableName => _executableName;
    }
}