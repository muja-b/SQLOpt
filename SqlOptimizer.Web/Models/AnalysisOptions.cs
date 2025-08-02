namespace SqlOptimizer.Web.Models
{
    public class AnalysisOptions
    {
        public bool PerformanceAnalysis { get; set; } = true;
        public bool SecurityAnalysis { get; set; } = true;
        public bool BestPracticeAnalysis { get; set; } = true;
        public bool IndexAnalysis { get; set; } = true;
        
        public PerformanceSettings Performance { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public BestPracticeSettings BestPractice { get; set; } = new();
    }

    public class PerformanceSettings
    {
        public bool CheckSelectStar { get; set; } = true;
        public bool CheckMissingIndexes { get; set; } = true;
        public bool CheckUnnecessaryDistinct { get; set; } = true;
        public bool CheckSubqueryOptimization { get; set; } = true;
        public bool CheckMissingLimit { get; set; } = true;
        public bool CheckFunctionsInWhere { get; set; } = true;
        public bool CheckLeadingWildcards { get; set; } = true;
        public int BulkInsertThreshold { get; set; } = 100;
    }

    public class SecuritySettings
    {
        public bool CheckSqlInjection { get; set; } = true;
        public bool CheckDynamicSql { get; set; } = true;
        public bool CheckSensitiveComments { get; set; } = true;
        public bool CheckHardcodedCredentials { get; set; } = true;
        public List<string> SensitiveKeywords { get; set; } = new() 
        { 
            "password", "admin", "secret", "key", "token", "credential" 
        };
    }

    public class BestPracticeSettings
    {
        public bool CheckExplicitColumnNames { get; set; } = true;
        public bool CheckWhereClause { get; set; } = true;
        public bool CheckTableAliases { get; set; } = true;
        public bool CheckTransactionUsage { get; set; } = true;
    }
} 