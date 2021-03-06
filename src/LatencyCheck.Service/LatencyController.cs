using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LatencyCheck.Service
{
    [ApiController]
    [Route("[controller]")]
    public class ConnectionsController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private ProcessSet _processes;

        public ConnectionsController(IMemoryCache cache, ProcessSet processes)
        {
            _cache = cache;
            _processes = processes;
        }

        [HttpGet("current")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
        public ActionResult<Dictionary<ProcessIdentifier, List<TcpConnectionInfo>>> GetLatency() {
            var result = _cache.GetLatencySet();
            if (result == null || !result.Any()) {
                return StatusCode(412);
            }
            var shortened = result.Select(r => r.ToDictionary(k => $"{k.Key.Name}:{k.Key.Id}", v => v.Value)).ToList();
            return Ok(shortened);
        }

        [HttpGet("available")]
        public ActionResult<List<string>> GetAvailable() {
            var result = _processes.Select(c => c.Name).ToList();
            return result;
        }
    }
}