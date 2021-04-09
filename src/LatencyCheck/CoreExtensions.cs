using System.Linq;

namespace LatencyCheck
{
    public static class CoreExtensions
    {
        public static bool ContainsProcess(this ProcessSet set, ProcessIdentifier ident) {
            return ident.Id != null
                ? set.Any(s => set.Contains(ident))
                : set.Any(s => s.Name == ident.Name);
        }
    }
}