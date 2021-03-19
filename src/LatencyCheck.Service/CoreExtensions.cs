using System;

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
    }
}