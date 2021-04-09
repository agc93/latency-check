using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LatencyCheck
{
    public class ProcessSet : List<ProcessIdentifier>
    {
        public ProcessSet(IEnumerable<ProcessConnectionClient> clients) {
            this.AddRange(clients
                .Select(p => Path.GetFileNameWithoutExtension(p.ExecutableName))
                .Select(e => new ProcessIdentifier {Name = e})
                .ToList());
        }

        public ProcessSet(List<string> processNames) {
            this.AddRange(processNames
                .Select(Path.GetFileNameWithoutExtension)
                .Select(e => new ProcessIdentifier {Name = e})
                .ToList());
        }
    }
}