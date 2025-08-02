using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using System.Text.RegularExpressions;

namespace SqlOptimizer.Web.Services
{
    // Internal class - only used by SqlAnalyzer internally
    internal class SqlParser : ISqlParser
    {
        public ParseResult ParseSql(string sqlScript, string tableDefinition)
        {
            if (string.IsNullOrWhiteSpace(sqlScript))
            {
                return Parser.Parse("INVALID SQL SYNTAX"); // TODO: Add error message
            }

            try
            {
                var parseResult = Parser.Parse(sqlScript);

                return parseResult;
            }
            catch
            {
                return Parser.Parse("INVALID SQL SYNTAX");
            }
        }

        public ParseResult ParseCreateTable(string tableDefinition)
        {
            if (string.IsNullOrWhiteSpace(tableDefinition))
            {
                return Parser.Parse("INVALID SQL SYNTAX"); // TODO: Add error message
            }

            try
            {
                var parseResult = Parser.Parse(tableDefinition);
                return parseResult;
            }
            catch
            {
                return Parser.Parse("INVALID SQL SYNTAX"); // TODO: Add error message
            }
        }

        // Pattern: \b([a-zA-Z_][a-zA-Z0-9_]*)\s*[=<>!]
        // Explanation: Matches column names followed by comparison operators
        // \b = word boundary, [a-zA-Z_] = valid column name start (letter or underscore)
        // [a-zA-Z0-9_]* = rest of column name (letters, numbers, underscores)
        // \s*[=<>!] = optional whitespace followed by comparison operators
        // Example: "WHERE user_id = 123" captures "user_id"
        public List<string> ExtractColumnsFromWhere(string whereClause)
        {
            var columns = new List<string>();
            
            // Extract column names from WHERE conditions
            var columnMatches = Regex.Matches(whereClause, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*[=<>!]", RegexOptions.IgnoreCase);
            foreach (Match match in columnMatches)
            {
                var columnName = match.Groups[1].Value.ToLowerInvariant();
                // Filter out common SQL keywords and operators
                if (!IsReservedWord(columnName))
                {
                    columns.Add(columnName);
                }
            }
            
            // Pattern: \b([a-zA-Z_][a-zA-Z0-9_]*)\s+between\b
            // Explanation: Matches column names used in BETWEEN clauses
            // Example: "WHERE age BETWEEN 18 AND 65" captures "age"
            var betweenMatches = Regex.Matches(whereClause, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s+between\b", RegexOptions.IgnoreCase);
            foreach (Match match in betweenMatches)
            {
                var columnName = match.Groups[1].Value.ToLowerInvariant();
                if (!IsReservedWord(columnName))
                {
                    columns.Add(columnName);
                }
            }
            
            // Pattern: \b([a-zA-Z_][a-zA-Z0-9_]*)\s+in\s*\(
            // Explanation: Matches column names used in IN clauses
            // \s*\( = optional whitespace followed by opening parenthesis
            // Example: "WHERE status IN ('active', 'pending')" captures "status"
            var inMatches = Regex.Matches(whereClause, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s+in\s*\(", RegexOptions.IgnoreCase);
            foreach (Match match in inMatches)
            {
                var columnName = match.Groups[1].Value.ToLowerInvariant();
                if (!IsReservedWord(columnName))
                {
                    columns.Add(columnName);
                }
            }
            
            // Pattern: \b([a-zA-Z_][a-zA-Z0-9_]*)\s+like\b
            // Explanation: Matches column names used in LIKE clauses
            // Example: "WHERE name LIKE '%john%'" captures "name"
            var likeMatches = Regex.Matches(whereClause, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s+like\b", RegexOptions.IgnoreCase);
            foreach (Match match in likeMatches)
            {
                var columnName = match.Groups[1].Value.ToLowerInvariant();
                if (!IsReservedWord(columnName))
                {
                    columns.Add(columnName);
                }
            }
            
            return columns.Distinct().ToList();
        }

        // Pattern: primary\s+key\s*\(\s*([^)]+)\s*\)
        // Explanation: Matches PRIMARY KEY constraint with column list
        // primary\s+key = "PRIMARY KEY" with whitespace
        // \s*\(\s* = optional whitespace around opening parenthesis
        // ([^)]+) = capture group for column list (anything except closing parenthesis)
        // \s*\) = optional whitespace and closing parenthesis
        // Example: "PRIMARY KEY (id, tenant_id)" captures "id, tenant_id"
        public List<string> ExtractIndexedColumns(string tableDefinition)
        {
            var indexedColumns = new List<string>();
            
            if (string.IsNullOrWhiteSpace(tableDefinition))
                return indexedColumns;
            
            var normalizedDef = tableDefinition.ToLowerInvariant();
            
            // Extract PRIMARY KEY columns
            var pkMatches = Regex.Matches(normalizedDef, @"primary\s+key\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            foreach (Match match in pkMatches)
            {
                var columnList = match.Groups[1].Value;
                var columns = columnList.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c));
                indexedColumns.AddRange(columns);
            }
            
            // Pattern: unique\s*\(\s*([^)]+)\s*\)
            // Explanation: Matches UNIQUE constraint with column list
            // Similar to PRIMARY KEY but for UNIQUE constraints
            // Example: "UNIQUE (email)" captures "email"
            var uniqueMatches = Regex.Matches(normalizedDef, @"unique\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase);
            foreach (Match match in uniqueMatches)
            {
                var columnList = match.Groups[1].Value;
                var columns = columnList.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c));
                indexedColumns.AddRange(columns);
            }
            
            // Pattern: \b([a-zA-Z_][a-zA-Z0-9_]*)\s+[^,]*primary\s+key
            // Explanation: Matches column-level PRIMARY KEY constraints
            // ([a-zA-Z_][a-zA-Z0-9_]*) = column name
            // \s+[^,]* = whitespace and data type definition (anything except comma)
            // primary\s+key = "PRIMARY KEY" constraint
            // Example: "id INT PRIMARY KEY" captures "id"
            var columnPkMatches = Regex.Matches(normalizedDef, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s+[^,]*primary\s+key", RegexOptions.IgnoreCase);
            foreach (Match match in columnPkMatches)
            {
                indexedColumns.Add(match.Groups[1].Value);
            }
            
            // Pattern: \b([a-zA-Z_][a-zA-Z0-9_]*)\s+[^,]*unique
            // Explanation: Matches column-level UNIQUE constraints
            // Similar to PRIMARY KEY but for column-level UNIQUE constraints
            // Example: "email VARCHAR(255) UNIQUE" captures "email"
            var columnUniqueMatches = Regex.Matches(normalizedDef, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s+[^,]*unique", RegexOptions.IgnoreCase);
            foreach (Match match in columnUniqueMatches)
            {
                indexedColumns.Add(match.Groups[1].Value);
            }
            
            return indexedColumns.Distinct().ToList();
        }

        // Common SQL reserved words that should not be considered as column names
        // when found in WHERE clauses
        public bool IsReservedWord(string word)
        {
            var reservedWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "and", "or", "not", "null", "true", "false", "like", "in", "between", 
                "exists", "case", "when", "then", "else", "end", "is", "as", "on", "join",
                "select", "from", "where", "insert", "update", "delete", "create", "drop",
                "alter", "table", "index", "view", "into", "values", "set", "order", "by",
                "group", "having", "distinct", "union", "inner", "left", "right", "outer",
                "full", "cross", "asc", "desc", "limit", "top", "offset", "fetch", "first",
                "last", "count", "sum", "avg", "min", "max"
            };
            
            return reservedWords.Contains(word);
        }

        // Pattern: \bwhere\s+(.+?)(?:\s+order\s+by|\s+group\s+by|\s+having|\s*$)
        // Explanation: Captures WHERE clause content until ORDER BY, GROUP BY, HAVING, or end of string
        // (.+?) = non-greedy capture of WHERE clause content
        // (?:\s+order\s+by|\s+group\s+by|\s+having|\s*$) = non-capturing group for clause terminators
        public List<string> ExtractWhereClauses(string sql)
        {
            var whereClauses = new List<string>();
            
            var whereMatches = Regex.Matches(sql, @"\bwhere\s+(.+?)(?:\s+order\s+by|\s+group\s+by|\s+having|\s*$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match match in whereMatches)
            {
                whereClauses.Add(match.Groups[1].Value.Trim());
            }
            
            return whereClauses;
        }

        // Basic SQL syntax validation using regex patterns
        public bool IsValidSql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return false;
            
            var normalizedSql = sql.Trim().ToLowerInvariant();
            
            // Check for basic SQL statement patterns
            var validPatterns = new[]
            {
                @"^\s*select\s+", // SELECT statements
                @"^\s*insert\s+", // INSERT statements
                @"^\s*update\s+", // UPDATE statements
                @"^\s*delete\s+", // DELETE statements
                @"^\s*create\s+", // CREATE statements
                @"^\s*alter\s+",  // ALTER statements
                @"^\s*drop\s+"    // DROP statements
            };
            
            return validPatterns.Any(pattern => Regex.IsMatch(normalizedSql, pattern, RegexOptions.IgnoreCase));
        }

        // Pattern: \b(?:from|join|update|into)\s+([a-zA-Z_][a-zA-Z0-9_]*)\b
        // Explanation: Extracts table names from FROM, JOIN, UPDATE, or INTO clauses
        // (?:from|join|update|into) = non-capturing group for keywords
        // \s+ = required whitespace
        // ([a-zA-Z_][a-zA-Z0-9_]*) = capture group for table name
        public string ExtractTableName(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return string.Empty;
            
            var normalizedSql = sql.ToLowerInvariant();
            
            // Try to extract table name from various SQL clauses
            var patterns = new[]
            {
                @"\bfrom\s+([a-zA-Z_][a-zA-Z0-9_]*)\b",     // FROM clause
                @"\bupdate\s+([a-zA-Z_][a-zA-Z0-9_]*)\b",   // UPDATE clause
                @"\binto\s+([a-zA-Z_][a-zA-Z0-9_]*)\b",     // INSERT INTO clause
                @"\bjoin\s+([a-zA-Z_][a-zA-Z0-9_]*)\b"      // JOIN clause
            };
            
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(normalizedSql, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            
            return string.Empty;
        }
    }
} 