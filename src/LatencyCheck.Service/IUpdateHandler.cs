using System.Collections.Generic;
using System.Threading.Tasks;

namespace LatencyCheck.Service
{
    public interface IUpdateHandler
    {
        Task HandleUpdateAsync(ProcessConnectionSet payload);
        Task HandleAllAsync(List<ProcessConnectionSet> payload) => Task.CompletedTask;
    }
    
    /*public class CacheHandler : IUpdateHandler
    {
        public async Task HandleUpdateAsync(ProcessConnectionSet payload)
        {
            throw new System.NotImplementedException();
        }
    }*/
}