using Microsoft.AspNetCore.Mvc;

namespace SqlOptimizer.Web
{
    [ApiController]
    [Route("optimizer/[controller]")]
    public class QueryController : ControllerBase
    {
        // POST: optimizer/query/optimize-with-table
        [HttpPost("optimize-with-table")]
        public IActionResult OptimizeWithTable([FromBody] OptimizeWithTableRequest request)
        {
            // TODO: Implement SQL optimization logic using request.SqlQuery and request.TableDefinition
            return Ok(new { OptimizedQuery = "-- Optimized SQL will appear here" });
        }

        // POST: optimizer/query/optimize-without-table
        [HttpPost("optimize-without-table")]
        public IActionResult OptimizeWithoutTable([FromBody] OptimizeWithoutTableRequest request)
        {
            return Ok(new { Message = "Optimized query without table" });
        }
    }

    public class OptimizeWithTableRequest
    {
        public string SqlQuery { get; set; } = string.Empty;
        public string TableDefinition { get; set; } = string.Empty;
    }

    public class StoreTableRequest
    {
        public string TableDefinition { get; set; } = string.Empty;
    }

    public class OptimizeWithoutTableRequest
    {
        public string SqlQuery { get; set; } = string.Empty;
    }
} 