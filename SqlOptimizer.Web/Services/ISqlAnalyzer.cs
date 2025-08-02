using SqlOptimizer.Web.Models;

namespace SqlOptimizer.Web.Services
{
    public interface ISqlAnalyzer
    {
        // Enhanced analysis with options and proper result model
        OptimizationResult AnalyzeStatements(string sqlText, string tableDefinition, AnalysisOptions? options = null);
        
        // Backward compatibility
        List<string> AnalyzeStatements(string sqlText, string tableDefinition);
    }
} 