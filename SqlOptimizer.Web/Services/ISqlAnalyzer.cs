using SqlParser.Ast;
using SqlParser;

namespace SqlOptimizer.Web.Services
{
    public interface ISqlAnalyzer
    {
        List<string> AnalyzeStatements(ParseResult sqlResult, Table tableResult);
    }
} 