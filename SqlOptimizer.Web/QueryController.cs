using Microsoft.AspNetCore.Mvc;
using SqlOptimizer.Web.Services;

namespace SqlOptimizer.Web
{
    [ApiController]
    [Route("optimizer/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly ISqlOptimizerService _optimizerService;
        public QueryController(ISqlOptimizerService optimizerService)
        {
            _optimizerService = optimizerService;
        }

        // POST: optimizer/query/optimize-with-table
        [HttpPost("optimize-with-table")]
        public IActionResult OptimizeWithTable([FromBody] OptimizeWithTableRequest request)
        {
            var result = _optimizerService.OptimizeQueryWithTable(request.SqlQuery, request.TableDefinition);
            return Ok(new { OptimizedQuery = result });
        }

        // POST: optimizer/query/optimize-without-table
        [HttpPost("optimize-without-table")]
        public IActionResult OptimizeWithoutTable([FromBody] OptimizeWithoutTableRequest request)
        {
            var result = _optimizerService.OptimizeQuery(request.SqlQuery);
            return Ok(new { OptimizedQuery = result });
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