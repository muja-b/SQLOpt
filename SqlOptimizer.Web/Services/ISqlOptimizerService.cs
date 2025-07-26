namespace SqlOptimizer.Web.Services
{
    public interface ISqlOptimizerService
    {
        string OptimizeQuery(string sqlQuery);
        string OptimizeQueryWithTable(string sqlQuery, string tableDefinition);
    }
} 