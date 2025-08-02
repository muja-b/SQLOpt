using System.Diagnostics;
using System.Text.RegularExpressions;
using SqlOptimizer.Web.Models;

namespace SqlOptimizer.Web.Services
{
    public class EnhancedSqlAnalyzer : ISqlAnalyzer
    {
        private readonly ISqlParser _parser;
        private readonly ILogger<EnhancedSqlAnalyzer> _logger;

        public EnhancedSqlAnalyzer(ILogger<EnhancedSqlAnalyzer> logger)
        {
            _parser = new SqlParser();
            _logger = logger;
        }

        public OptimizationResult AnalyzeStatements(string sqlText, string tableDefinition, AnalysisOptions? options = null)
        {
            var stopwatch = Stopwatch.StartNew();
            options ??= new AnalysisOptions();
            
            var result = new OptimizationResult
            {
                OriginalQuery = sqlText,
                TableDefinition = tableDefinition ?? string.Empty
            };

            try
            {
                if (string.IsNullOrWhiteSpace(sqlText))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "SQL query cannot be empty";
                    return result;
                }

                // Validate SQL syntax first
                if (!_parser.IsValidSql(sqlText))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Invalid SQL syntax detected";
                    return result;
                }

                // Perform analysis based on options
                var suggestions = new List<OptimizationSuggestion>();
                var normalizedSql = sqlText.ToLowerInvariant().Trim();

                if (options.PerformanceAnalysis)
                {
                    AnalyzePerformance(normalizedSql, tableDefinition, suggestions, options.Performance);
                }

                if (options.SecurityAnalysis)
                {
                    AnalyzeSecurity(normalizedSql, suggestions, options.Security);
                }

                if (options.BestPracticeAnalysis)
                {
                    AnalyzeBestPractices(normalizedSql, suggestions, options.BestPractice);
                }

                result.Suggestions = suggestions;
                stopwatch.Stop();

                // Calculate metrics
                result.Metrics = new OptimizationMetrics
                {
                    TotalSuggestions = suggestions.Count,
                    HighPrioritySuggestions = suggestions.Count(s => s.Priority >= SuggestionPriority.High),
                    SecurityIssues = suggestions.Count(s => s.Type == SuggestionType.Security),
                    PerformanceIssues = suggestions.Count(s => s.Type == SuggestionType.Performance),
                    AnalysisTime = stopwatch.Elapsed
                };

                _logger.LogInformation("SQL analysis completed in {ElapsedMs}ms with {SuggestionCount} suggestions", 
                    stopwatch.ElapsedMilliseconds, suggestions.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SQL analysis");
                result.IsValid = false;
                result.ErrorMessage = "An error occurred during analysis";
                return result;
            }
        }

        // Backward compatibility method
        public List<string> AnalyzeStatements(string sqlText, string tableDefinition)
        {
            var result = AnalyzeStatements(sqlText, tableDefinition, new AnalysisOptions());
            return result.Suggestions.Select(s => s.Message).ToList();
        }

        private void AnalyzePerformance(string sql, string tableDefinition, List<OptimizationSuggestion> suggestions, PerformanceSettings settings)
        {
            if (settings.CheckSelectStar && Regex.IsMatch(sql, @"\bselect\s+\*\b", RegexOptions.IgnoreCase))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Performance,
                    Priority = SuggestionPriority.Medium,
                    Message = "Avoid SELECT *, it returns unnecessary data and slows things down",
                    Recommendation = "Specify only the columns you need",
                    Example = "SELECT id, name, email FROM users"
                });
            }

            if (settings.CheckMissingIndexes)
            {
                var whereClauses = _parser.ExtractWhereClauses(sql);
                foreach (var whereClause in whereClauses)
                {
                    var whereColumns = _parser.ExtractColumnsFromWhere(whereClause);
                    var indexedColumns = _parser.ExtractIndexedColumns(tableDefinition);
                    
                    if (whereColumns.Any() && !whereColumns.Any(col => indexedColumns.Contains(col, StringComparer.OrdinalIgnoreCase)))
                    {
                        suggestions.Add(new OptimizationSuggestion
                        {
                            Type = SuggestionType.IndexOptimization,
                            Priority = SuggestionPriority.High,
                            Message = "WHERE clause doesn't use indexed columns - consider adding indexes",
                            Recommendation = $"Add indexes on columns: {string.Join(", ", whereColumns)}",
                            Example = $"CREATE INDEX idx_{string.Join("_", whereColumns)} ON table_name ({string.Join(", ", whereColumns)})"
                        });
                    }
                }
            }

            if (settings.CheckFunctionsInWhere && Regex.IsMatch(sql, @"\bwhere\s+.*\b\w+\s*\(.*\)\s*[=<>]", RegexOptions.IgnoreCase))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Performance,
                    Priority = SuggestionPriority.High,
                    Message = "Avoid functions in WHERE clause as they prevent index usage",
                    Recommendation = "Move function logic to application layer or use computed columns",
                    Example = "Instead of WHERE UPPER(name) = 'JOHN', use WHERE name = 'John' with proper case handling"
                });
            }

            if (settings.CheckLeadingWildcards && Regex.IsMatch(sql, @"\blike\s+['""]%", RegexOptions.IgnoreCase))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Performance,
                    Priority = SuggestionPriority.Medium,
                    Message = "Leading wildcards in LIKE patterns prevent index usage",
                    Recommendation = "Avoid starting LIKE patterns with wildcards when possible",
                    Example = "Use name LIKE 'John%' instead of name LIKE '%John%' if searching from the beginning"
                });
            }
        }

        private void AnalyzeSecurity(string sql, List<OptimizationSuggestion> suggestions, SecuritySettings settings)
        {
            if (settings.CheckSqlInjection && Regex.IsMatch(sql, @"['""]\s*\+\s*\w+\s*\+\s*['""]", RegexOptions.IgnoreCase))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Security,
                    Priority = SuggestionPriority.Critical,
                    Message = "Potential SQL injection: String concatenation detected",
                    Recommendation = "Use parameterized queries instead of string concatenation",
                    Example = "Use @param parameters instead of concatenating user input"
                });
            }

            if (settings.CheckDynamicSql && Regex.IsMatch(sql, @"\b(exec|execute)\s+\(", RegexOptions.IgnoreCase))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.Security,
                    Priority = SuggestionPriority.High,
                    Message = "Dynamic SQL detected",
                    Recommendation = "Use stored procedures or parameterized queries for security",
                    Example = "Replace dynamic SQL with stored procedures or ORM queries"
                });
            }

            if (settings.CheckSensitiveComments)
            {
                foreach (var keyword in settings.SensitiveKeywords)
                {
                    if (Regex.IsMatch(sql, $@"--.*\b{keyword}\b", RegexOptions.IgnoreCase))
                    {
                        suggestions.Add(new OptimizationSuggestion
                        {
                            Type = SuggestionType.Security,
                            Priority = SuggestionPriority.High,
                            Message = "Sensitive information found in comments",
                            Recommendation = "Remove all sensitive information from SQL comments before production",
                            Example = "Remove passwords, keys, and other secrets from comments"
                        });
                        break;
                    }
                }
            }
        }

        private void AnalyzeBestPractices(string sql, List<OptimizationSuggestion> suggestions, BestPracticeSettings settings)
        {
            if (settings.CheckExplicitColumnNames && Regex.IsMatch(sql, @"\binsert\s+into\s+\w+\s+values\b", RegexOptions.IgnoreCase))
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Type = SuggestionType.BestPractice,
                    Priority = SuggestionPriority.Medium,
                    Message = "INSERT without explicit column names is unsafe",
                    Recommendation = "Always specify column names for clarity and safety",
                    Example = "INSERT INTO users (name, email) VALUES ('John', 'john@example.com')"
                });
            }

            if (settings.CheckWhereClause)
            {
                if (Regex.IsMatch(sql, @"\bupdate\s+\w+\s+set\b", RegexOptions.IgnoreCase) && !Regex.IsMatch(sql, @"\bwhere\b", RegexOptions.IgnoreCase))
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Type = SuggestionType.BestPractice,
                        Priority = SuggestionPriority.Critical,
                        Message = "UPDATE without WHERE clause will affect all rows",
                        Recommendation = "Always add a WHERE clause to limit the scope of UPDATE statements",
                        Example = "UPDATE users SET status = 'active' WHERE id = 123"
                    });
                }

                if (Regex.IsMatch(sql, @"\bdelete\s+from\s+\w+\b", RegexOptions.IgnoreCase) && !Regex.IsMatch(sql, @"\bwhere\b", RegexOptions.IgnoreCase))
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Type = SuggestionType.BestPractice,
                        Priority = SuggestionPriority.Critical,
                        Message = "DELETE without WHERE clause will remove all rows",
                        Recommendation = "Always add a WHERE clause to limit the scope of DELETE statements",
                        Example = "DELETE FROM users WHERE id = 123"
                    });
                }
            }
        }
    }
} 