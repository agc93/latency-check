using System;
using System.IO;
using System.Linq;

namespace LatencyCheck
{
    public static class CoreExtensions
    {
        public static bool ContainsProcess(this ProcessSet set, ProcessIdentifier ident, bool ignorePids = false) {
            return ignorePids || ident.Id == null
                ? set.Any(s =>
                    s.GetName().Equals(ident.GetName(), StringComparison.CurrentCultureIgnoreCase))
                : set.Any(s =>
                    s.GetName().Equals(ident.GetName(), StringComparison.CurrentCultureIgnoreCase) && s.Id == ident.Id);
            /*return set.Any(s =>
                s.GetName().Equals(ident.GetName(), StringComparison.CurrentCultureIgnoreCase) &&
                (ident.Id == null || ident.Id == s.Id));*/
            /*return ident.Id != null
                ? set.Any(s => s.Name == ident.Name)
                : set.Any(s => s.Name == ident.Name);*/
        }
    }
}