using SqlParser;

namespace SqlOptimizer.Web.Services
{
    public interface ISqlParser
    {
        (ParseResult Result, bool IsOk) Parse(string sqlScript, string tableDefinition);
    }
} 