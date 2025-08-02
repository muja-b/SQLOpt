namespace SqlOptimizer.Web.Models
{
    public class OptimizationResult
    {
        public string OriginalQuery { get; set; } = string.Empty;
        public string TableDefinition { get; set; } = string.Empty;
        public List<OptimizationSuggestion> Suggestions { get; set; } = new();
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public OptimizationMetrics Metrics { get; set; } = new();
    }

    public class OptimizationSuggestion
    {
        public SuggestionType Type { get; set; }
        public SuggestionPriority Priority { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Recommendation { get; set; }
        public string? Example { get; set; }
    }

    public class OptimizationMetrics
    {
        public int TotalSuggestions { get; set; }
        public int HighPrioritySuggestions { get; set; }
        public int SecurityIssues { get; set; }
        public int PerformanceIssues { get; set; }
        public TimeSpan AnalysisTime { get; set; }
    }

    public enum SuggestionType
    {
        Performance,
        Security,
        BestPractice,
        IndexOptimization,
        QueryStructure
    }

    public enum SuggestionPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
} 