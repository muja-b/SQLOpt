using System.Text.RegularExpressions;
using SqlOptimizer.Web.Models;

namespace SqlOptimizer.Web.Services
{
    public class SqlOptimizerService : ISqlOptimizerService
    {
        private readonly ISqlAnalyzer _analyzer;
        private readonly IResultBuilderService _resultBuilder;

        public SqlOptimizerService(ISqlAnalyzer analyzer, IResultBuilderService resultBuilder)
        {
            _analyzer = analyzer;
            _resultBuilder = resultBuilder;
        }

        // Enhanced async methods
        public async Task<OptimizationResult> OptimizeQueryAsync(string sqlQuery, AnalysisOptions? options = null)
        {
            return await Task.Run(() => _analyzer.AnalyzeStatements(sqlQuery, string.Empty, options));
        }

        public async Task<OptimizationResult> OptimizeQueryWithTableAsync(string sqlQuery, string tableDefinition, AnalysisOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                return new OptimizationResult
                {
                    IsValid = false,
                    ErrorMessage = "SQL query is required"
                };
            }

            return await Task.Run(() => _analyzer.AnalyzeStatements(sqlQuery, tableDefinition ?? string.Empty, options));
        }

        // Backward compatibility methods
        public string OptimizeQuery(string sqlQuery)
        {
            // For queries without table definition, we can still do basic analysis
            var enhancements = _analyzer.AnalyzeStatements(sqlQuery, string.Empty);
            return _resultBuilder.BuildOptimizationResult(sqlQuery, string.Empty, enhancements);
        }

        public string OptimizeQueryWithTable(string sqlQuery, string tableDefinition)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(sqlQuery))
            {
                return "-- Error: SQL query is required";
            }
            
            // Analyze the SQL and get enhancement suggestions
            // SQL validation is now handled inside the analyzer
            var enhancements = _analyzer.AnalyzeStatements(sqlQuery, tableDefinition ?? string.Empty);
            
            // Build the optimization result
            return _resultBuilder.BuildOptimizationResult(sqlQuery, tableDefinition, enhancements);
        }
    }
} 