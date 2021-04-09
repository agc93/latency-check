using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace LatencyCheck.Service
{
    public static class CoreExtensions
    {
        public static int ToInt(this uint value) {
            return Convert.ToInt32(value);
        }

        public static int ToInt(this float value) {
            return Convert.ToInt32(value);
        }

        public static long ToInt64(this uint value) {
            return Convert.ToInt64(value);
        } 
        public static long ToInt64(this float value) {
            return Convert.ToInt64(value);
        } 

        public static ProcessSet GetProcessesForSource(this ProcessSet allProcesses, IConfiguration config, string name) {
            var section = config.GetSection(name);
            return (section.Exists() && section.Get<List<string>>() is var processNames && processNames.Any())
                ? new ProcessSet(processNames)
                : allProcesses;
        }
    }
}