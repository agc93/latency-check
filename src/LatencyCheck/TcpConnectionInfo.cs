using System.ComponentModel;
using System.Diagnostics;
using static Vanara.PInvoke.IpHlpApi;

namespace LatencyCheck
{
    public class TcpConnectionInfo
    {
        private TCP_ESTATS_PATH_ROD_v0 stats;
        private MIB_TCPROW_OWNER_MODULE ownerRow;
        private MIB_TCPROW tcpRow;
        private Process process;

        public TcpConnectionInfo(Process process, TCP_ESTATS_PATH_ROD_v0 stats, MIB_TCPROW_OWNER_MODULE ownerRow, MIB_TCPROW tcpRow) {
            this.stats = stats;
            this.ownerRow = ownerRow;
            this.tcpRow = tcpRow;
            this.process = process;
        }

        public uint RTT => stats.SampleRtt;
        public uint Smoothed => stats.SmoothedRtt;
        private uint _mean;
        public uint Mean
        {
            get
            {
                _mean = (stats.CountRtt > 0 && stats.SumRtt > 0) ? stats.SumRtt / stats.CountRtt : _mean;
                return _mean;
            }
        }
        public uint Min => stats.MinRtt;
        public uint Max => stats.MaxRtt;
        public uint Errors => stats.Timeouts;
        [DisplayName("Remote IP")]
        public string RemoteIp => ownerRow.dwRemoteAddr.ToString();
        public string Name => process?.ProcessName;
        // public string Path => ownerInfo.pModulePath;
        public int? ProcessId => process?.Id;
        public uint? LocalPort => ownerRow.dwLocalPort;

        public MIB_TCPROW GetTcpRow() => tcpRow;
        public void SetStats(TCP_ESTATS_PATH_ROD_v0 stats)
        {
            this.stats = stats;
        }
    }
}