namespace SqlOptimizer.Web.Services
{
    public interface IResultBuilderService
    {
        string BuildOptimizationResult(string query, string tableDef, List<string> suggestions);
    }
} 