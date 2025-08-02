using System.Text.RegularExpressions;
using SqlOptimizer.Web.Models;

namespace SqlOptimizer.Web.Services
{
    public class SqlAnalyzer : ISqlAnalyzer
    {
        private readonly ISqlParser _parser;

        public SqlAnalyzer()
        {
            // Create SqlParser internally - it's an implementation detail
            _parser = new SqlParser();
        }

        // New interface method with options support
        public OptimizationResult AnalyzeStatements(string sqlText, string tableDefinition, AnalysisOptions? options = null)
        {
            // For backward compatibility, convert old analysis to new format
            var suggestions = AnalyzeStatements(sqlText, tableDefinition);
            
            return new OptimizationResult
            {
                OriginalQuery = sqlText,
                TableDefinition = tableDefinition ?? string.Empty,
                IsValid = !suggestions.Any(s => s.Contains("Invalid SQL syntax")),
                ErrorMessage = suggestions.FirstOrDefault(s => s.Contains("Invalid SQL syntax")),
                Suggestions = suggestions.Select(s => new OptimizationSuggestion
                {
                    Type = DetermineSuggestionType(s),
                    Priority = DeterminePriority(s),
                    Message = s
                }).ToList(),
                Metrics = new OptimizationMetrics
                {
                    TotalSuggestions = suggestions.Count,
                    HighPrioritySuggestions = suggestions.Count(s => s.Contains("without WHERE") || s.Contains("SQL injection")),
                    SecurityIssues = suggestions.Count(s => s.Contains("injection") || s.Contains("Dynamic SQL")),
                    PerformanceIssues = suggestions.Count(s => s.Contains("SELECT *") || s.Contains("index"))
                }
            };
        }

        // Original interface method
        public List<string> AnalyzeStatements(string sqlText, string tableDefinition)
        {
            var enhancements = new List<string>();
            
            if (string.IsNullOrWhiteSpace(sqlText))
                return enhancements;
            
            // Validate SQL syntax first
            if (!_parser.IsValidSql(sqlText))
            {
                enhancements.Add("Invalid SQL syntax detected. Please check your query.");
                return enhancements;
            }
            
            // Analyze the SQL using regex patterns
            AnalyzeSql(sqlText, tableDefinition, enhancements);
            
            return enhancements;
        }

        private SuggestionType DetermineSuggestionType(string suggestion)
        {
            if (suggestion.Contains("injection") || suggestion.Contains("Dynamic SQL") || suggestion.Contains("credentials"))
                return SuggestionType.Security;
            if (suggestion.Contains("SELECT *") || suggestion.Contains("index") || suggestion.Contains("LIKE") || suggestion.Contains("function"))
                return SuggestionType.Performance;
            if (suggestion.Contains("without WHERE") || suggestion.Contains("column names"))
                return SuggestionType.BestPractice;
            if (suggestion.Contains("index"))
                return SuggestionType.IndexOptimization;
            return SuggestionType.QueryStructure;
        }

        private SuggestionPriority DeterminePriority(string suggestion)
        {
            if (suggestion.Contains("SQL injection") || suggestion.Contains("without WHERE"))
                return SuggestionPriority.Critical;
            if (suggestion.Contains("index") || suggestion.Contains("function"))
                return SuggestionPriority.High;
            if (suggestion.Contains("SELECT *") || suggestion.Contains("DISTINCT"))
                return SuggestionPriority.Medium;
            return SuggestionPriority.Low;
        }

        private void AnalyzeSql(string sql, string tableDefinition, List<string> enhancements)
        {
            // Normalize SQL for easier regex matching
            var normalizedSql = sql.ToLowerInvariant().Trim();
            
            // SELECT statement analysis
            AnalyzeSelectStatements(normalizedSql, tableDefinition, enhancements);
            
            // INSERT statement analysis
            AnalyzeInsertStatements(normalizedSql, enhancements);
            
            // UPDATE statement analysis
            AnalyzeUpdateStatements(normalizedSql, enhancements);
            
            // DELETE statement analysis
            AnalyzeDeleteStatements(normalizedSql, enhancements);
            
            // Security analysis
            AnalyzeSecurityIssues(normalizedSql, enhancements);
        }

        private void AnalyzeSelectStatements(string sql, string tableDefinition, List<string> enhancements)
        {
            // Pattern: \bselect\s+\*\b
            // Explanation: Matches "SELECT *" with word boundaries
            // \b = word boundary, \s+ = one or more whitespace characters
            // This catches "SELECT *" but not "SELECTA*" or "*SELECT"
            if (Regex.IsMatch(sql, @"\bselect\s+\*\b", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Avoid SELECT *, it returns unnecessary data and slows things down.");
            }
            
            // Check for missing indexes in WHERE clause using SqlParser
            var whereClauses = _parser.ExtractWhereClauses(sql);
            foreach (var whereClause in whereClauses)
            {
                var whereColumns = _parser.ExtractColumnsFromWhere(whereClause);
                var indexedColumns = _parser.ExtractIndexedColumns(tableDefinition);
                
                if (whereColumns.Any() && !whereColumns.Any(col => indexedColumns.Contains(col, StringComparer.OrdinalIgnoreCase)))
                {
                    enhancements.Add("WHERE clause doesn't use indexed columns - consider adding indexes");
                }
            }
            
            // Pattern: \bselect\s+distinct\s+count\b
            // Explanation: Matches "SELECT DISTINCT COUNT" which is often redundant
            // COUNT already handles uniqueness, so DISTINCT is usually unnecessary
            if (Regex.IsMatch(sql, @"\bselect\s+distinct\s+count\b", RegexOptions.IgnoreCase))
            {
                enhancements.Add("DISTINCT with COUNT might be unnecessary. Consider if you really need unique counts.");
            }
            
            // Pattern: \bin\s*\(\s*select\b
            // Explanation: Matches "IN (SELECT" pattern for subqueries
            // \s* = zero or more whitespace, allows for "IN(SELECT" or "IN ( SELECT"
            // EXISTS is often more efficient than IN with subqueries
            if (Regex.IsMatch(sql, @"\bin\s*\(\s*select\b", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Consider using EXISTS instead of IN for better performance with subqueries.");
            }
            
            // Pattern: \bnot\s+in\s*\(\s*select\b
            // Explanation: Matches "NOT IN (SELECT" pattern
            // NOT EXISTS is often more efficient and handles NULLs better than NOT IN
            if (Regex.IsMatch(sql, @"\bnot\s+in\s*\(\s*select\b", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Consider using NOT EXISTS instead of NOT IN for better performance with subqueries.");
            }
            
            // Pattern: \bselect\b combined with negative lookahead for \b(limit|top|fetch)\b
            // Explanation: Matches SELECT statements without LIMIT, TOP, or FETCH keywords
            // This helps prevent accidentally returning millions of rows
            if (Regex.IsMatch(sql, @"\bselect\b", RegexOptions.IgnoreCase) && 
                !Regex.IsMatch(sql, @"\b(limit|top|fetch)\b", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Consider adding LIMIT/TOP to prevent returning excessive rows.");
            }
            
            // Pattern: \bwhere\s+.*\b\w+\s*\(.*\)\s*[=<>]
            // Explanation: Matches functions used in WHERE clause comparisons
            // \w+\s*\( = function name followed by opening parenthesis
            // .*\) = function parameters and closing parenthesis
            // \s*[=<>] = comparison operator after the function
            // Functions in WHERE prevent index usage: WHERE UPPER(name) = 'JOHN'
            if (Regex.IsMatch(sql, @"\bwhere\s+.*\b\w+\s*\(.*\)\s*[=<>]", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Avoid functions in WHERE clause as they prevent index usage.");
            }
            
            // Pattern: \blike\s+['""]%
            // Explanation: Matches LIKE patterns starting with wildcard
            // ['""] = single or double quote, % = wildcard at the beginning
            // Leading wildcards prevent index usage: WHERE name LIKE '%john'
            if (Regex.IsMatch(sql, @"\blike\s+['""]%", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Leading wildcards in LIKE patterns prevent index usage.");
            }
        }

        private void AnalyzeInsertStatements(string sql, List<string> enhancements)
        {
            // Pattern: \binsert\s+into\s+\w+\s+values\b
            // Explanation: Matches INSERT INTO table VALUES without column specification
            // \w+ = table name (word characters), values without (columns) is risky
            // Example: INSERT INTO users VALUES ('John', 'Doe') - unsafe
            // Safe: INSERT INTO users (first_name, last_name) VALUES ('John', 'Doe')
            if (Regex.IsMatch(sql, @"\binsert\s+into\s+\w+\s+values\b", RegexOptions.IgnoreCase))
            {
                enhancements.Add("INSERT without explicit column names is unsafe. Specify column names for clarity and safety.");
            }
            
            // Pattern: \(\s*[^)]+\s*\)
            // Explanation: Matches parentheses groups (VALUES clauses)
            // \( = opening parenthesis, \s* = optional whitespace
            // [^)]+ = one or more characters that are not closing parenthesis
            // \s*\) = optional whitespace and closing parenthesis
            // Counts how many VALUES clauses exist to detect bulk inserts
            var valueCount = Regex.Matches(sql, @"\(\s*[^)]+\s*\)", RegexOptions.IgnoreCase).Count;
            if (valueCount > 100)
            {
                enhancements.Add("Consider using batch processing for large INSERT operations.");
            }
        }

        private void AnalyzeUpdateStatements(string sql, List<string> enhancements)
        {
            // Pattern: \bupdate\s+\w+\s+set\b combined with negative lookahead for \bwhere\b
            // Explanation: Matches UPDATE statements without WHERE clause
            // This is dangerous as it updates ALL rows in the table
            // Example: UPDATE users SET status = 'active' - affects all users!
            if (Regex.IsMatch(sql, @"\bupdate\s+\w+\s+set\b", RegexOptions.IgnoreCase) && 
                !Regex.IsMatch(sql, @"\bwhere\b", RegexOptions.IgnoreCase))
            {
                enhancements.Add("UPDATE without WHERE clause will affect all rows. Add a WHERE clause to limit the scope.");
            }
            
            // Pattern: \bupdate\s+.*\bwhere\s+.*\b\w+\s*\(.*\)\s*[=<>]
            // Explanation: Matches UPDATE with functions in WHERE clause
            // Similar to SELECT, functions in UPDATE WHERE clause prevent index usage
            // Example: UPDATE users SET status = 'active' WHERE UPPER(name) = 'JOHN'
            if (Regex.IsMatch(sql, @"\bupdate\s+.*\bwhere\s+.*\b\w+\s*\(.*\)\s*[=<>]", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Avoid functions in UPDATE WHERE clause as they prevent index usage.");
            }
        }

        private void AnalyzeDeleteStatements(string sql, List<string> enhancements)
        {
            // Pattern: \bdelete\s+from\s+\w+\b combined with negative lookahead for \bwhere\b
            // Explanation: Matches DELETE statements without WHERE clause
            // This is extremely dangerous as it deletes ALL rows from the table
            // Example: DELETE FROM users - deletes all users!
            if (Regex.IsMatch(sql, @"\bdelete\s+from\s+\w+\b", RegexOptions.IgnoreCase) && 
                !Regex.IsMatch(sql, @"\bwhere\b", RegexOptions.IgnoreCase))
            {
                enhancements.Add("DELETE without WHERE clause will remove all rows. Add a WHERE clause to limit the scope.");
            }
            
            // Pattern: \bdelete\s+.*\bwhere\s+.*\b\w+\s*\(.*\)\s*[=<>]
            // Explanation: Matches DELETE with functions in WHERE clause
            // Functions in DELETE WHERE clause prevent index usage, making deletes slow
            // Example: DELETE FROM users WHERE UPPER(name) = 'JOHN'
            if (Regex.IsMatch(sql, @"\bdelete\s+.*\bwhere\s+.*\b\w+\s*\(.*\)\s*[=<>]", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Avoid functions in DELETE WHERE clause as they prevent index usage.");
            }
        }

        private void AnalyzeSecurityIssues(string sql, List<string> enhancements)
        {
            // Pattern: ['""]\s*\+\s*\w+\s*\+\s*['"""]
            // Explanation: Matches string concatenation patterns that indicate SQL injection risk
            // ['""] = single or double quote, \s*\+\s* = plus operator with optional whitespace
            // \w+ = variable name, often indicates concatenated user input
            // Example: "SELECT * FROM users WHERE name = '" + userName + "'"
            if (Regex.IsMatch(sql, @"['""]\s*\+\s*\w+\s*\+\s*['""]", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Potential SQL injection: String concatenation detected. Use parameterized queries.");
            }
            
            // Pattern: \b(exec|execute)\s+\(
            // Explanation: Matches dynamic SQL execution with parentheses
            // EXEC() or EXECUTE() with parameters often indicates dynamic SQL
            // Example: EXEC('SELECT * FROM ' + @tableName) - dangerous!
            if (Regex.IsMatch(sql, @"\b(exec|execute)\s+\(", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Dynamic SQL detected. Use parameterized queries for security.");
            }
            
            // Pattern: --.*\b(password|admin|secret)\b
            // Explanation: Matches commented lines containing sensitive keywords
            // -- = SQL comment start, .* = any characters
            // \b(password|admin|secret)\b = sensitive words with word boundaries
            // Example: -- password = 'secret123' (should be removed!)
            if (Regex.IsMatch(sql, @"--.*\b(password|admin|secret)\b", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Sensitive information found in comments. Remove before production.");
            }
            
            // Pattern: \b(password|pwd)\s*=\s*['""][^'""]+['""]
            // Explanation: Matches hardcoded password assignments
            // (password|pwd) = password keywords, \s*=\s* = equals with optional whitespace
            // ['""] = quote, [^'""]+['""] = password value in quotes
            // Example: password = 'mypassword123' - should use config instead!
            if (Regex.IsMatch(sql, @"\b(password|pwd)\s*=\s*['""][^'""]+['""]", RegexOptions.IgnoreCase))
            {
                enhancements.Add("Hardcoded credentials detected. Use secure configuration instead.");
            }
        }
    }
} 
