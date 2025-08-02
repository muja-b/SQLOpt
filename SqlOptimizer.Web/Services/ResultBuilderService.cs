using System.Text;

namespace SqlOptimizer.Web.Services
{
    public class ResultBuilderService : IResultBuilderService
    {
        public string BuildOptimizationResult(string query, string tableDef, List<string> suggestions)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"-- Original Query:\n{query}\n");
            sb.AppendLine($"-- Table Definition:\n{tableDef}\n");

            if (suggestions.Any())
            {
                sb.AppendLine("-- Optimization Suggestions:");
                foreach (var suggestion in suggestions)
                {
                    sb.AppendLine($"-- {suggestion}");
                }
            }
            else
            {
                sb.AppendLine("-- No optimization suggestions found.");
            }

            return sb.ToString();
        }
    }
} 