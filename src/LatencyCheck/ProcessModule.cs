using static Vanara.PInvoke.IpHlpApi;

namespace LatencyCheck
{
    internal class ProcessModule {
        // internal TCPIP_OWNER_MODULE_BASIC_INFO OwnerInfo {get; init;}
        internal int ProcessId {get; init; }
        internal string ProcessName {get;init;}
        internal System.Diagnostics.Process Process {get;init;}
        internal MIB_TCPROW_OWNER_MODULE Owner {get;init;}
    }
}