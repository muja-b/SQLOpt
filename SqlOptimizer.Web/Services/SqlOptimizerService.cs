using System.Text;
using System.Text.RegularExpressions;
using SqlParser;
using SqlParser.Ast;
using SqlOptimizer.Web.Models;

namespace SqlOptimizer.Web.Services
{
    public class SqlOptimizerService : ISqlOptimizerService
    {
        private readonly ISqlParser _parser;
        private readonly ISqlAnalyzer _analyzer;

        public SqlOptimizerService(ISqlParser parser, ISqlAnalyzer analyzer)
        {
            _parser = parser;
            _analyzer = analyzer;
        }

        public string OptimizeQuery(string sqlQuery)
        {
            // TODO: Add real optimization logic
            return $"-- Optimized (simple): {sqlQuery}";
        }

        public string OptimizeQueryWithTable(string sqlQuery, string tableDefinition)
        {
            // Parse the SQL script
            var parsedResult = _parser.Parse(sqlQuery, tableDefinition);
            
            if (!parsedResult.IsOk)
            {
                return $"-- Error: Parsing failed";
            }

            // Create table object from table definition
            var table = new Table(tableDefinition);

            // Analyze the statements for optimizations using parsed results
            var enhancements = _analyzer.AnalyzeStatements(parsedResult.Result, table);
            
            // Return optimized query with suggestions
            var result = $"-- Original Query:\n{sqlQuery}\n\n";
            
            // Add table information
            result += $"-- Table Definition:\n{tableDefinition}\n\n";
            
            if (enhancements.Any())
            {
                result += "-- Optimization Suggestions:\n";
                result += string.Join("\n", enhancements.Select(e => $"-- {e}"));
            }
            else
            {
                result += "-- No optimization suggestions found.";
            }
            
            return result;
        }
    }
} 