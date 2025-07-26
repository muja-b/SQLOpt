using SqlParser;

namespace SqlOptimizer.Web.Models
{
    public class AnalyzedSqlStatement
    {
        public ParseResult ParseResult { get; set; }
        public List<string> Enhancements { get; set; } = new();
    }
} 