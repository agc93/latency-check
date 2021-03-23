using System;
using System.Threading.Tasks;

namespace LatencyCheck.Service
{
    public static class RunHelpers
    {
        public static void TryRun(Action actionFn, Action errFn = null)
        {
            try
            {
                actionFn();
            }
            catch (NotImplementedException ex)
            {
                // ignored
            }
            catch (Exception ex)
            {
                errFn?.Invoke();
            }
        }
        
        /*public static async Task TryRunAsync(Action actionFn, Action errFn = null)
        {
            try
            {
                actionFn();
            }
            catch (NotImplementedException ex)
            {
                // ignored
            }
            catch (Exception ex)
            {
                errFn?.Invoke();
            }
        }*/
    }
}