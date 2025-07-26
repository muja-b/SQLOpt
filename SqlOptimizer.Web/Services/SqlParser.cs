using SqlParser.Ast;

namespace SqlOptimizer.Web.Services
{
    public class SqlParser : ISqlParser
    {
        public (ParseResult Result, bool IsOk) Parse(string sqlScript, string tableDefinition)
        {
            if (string.IsNullOrWhiteSpace(sqlScript) || string.IsNullOrWhiteSpace(tableDefinition))
            {
                return (null, false);
            }

            var table = new Table(tableDefinition);
            var parser = new Parser();
            ParseResult result = parser.Parse(sqlScript);

            if (!result.IsValid || result.Statements.Count == 0)
            {
                return (result, false);
            }

            return (result, true);
        }
    }
} 