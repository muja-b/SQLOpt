using SqlOptimizer.Web.Models;

namespace SqlOptimizer.Web.Services
{
    public interface ISqlOptimizerService
    {
        // Enhanced async methods with proper models
        Task<OptimizationResult> OptimizeQueryAsync(string sqlQuery, AnalysisOptions? options = null);
        Task<OptimizationResult> OptimizeQueryWithTableAsync(string sqlQuery, string tableDefinition, AnalysisOptions? options = null);
        
        // Backward compatibility (sync methods)
        string OptimizeQuery(string sqlQuery);
        string OptimizeQueryWithTable(string sqlQuery, string tableDefinition);
    }
} 