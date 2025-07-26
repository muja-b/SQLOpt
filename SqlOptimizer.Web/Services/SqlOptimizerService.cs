using System.Text;
using System.Text.RegularExpressions;
using SqlParser;
using SqlParser.Ast;

namespace SqlOptimizer.Web.Services
{
    public enum StatementType
    {
        INSERT,
        UPDATE,
        DELETE
    }

    public interface ISqlOptimizerService
    {
        string OptimizeQuery(string sqlQuery);
        string OptimizeQueryWithTable(string sqlQuery, string tableDefinition);
        (ParseResult Result, bool IsOk) ParseWithSqlParser(string sqlScript, string tableDefinition);
        List<string> AnalyzeStatements(List<ParsedSqlStatement> parsedStatements, string tableDefinition);
    }

    public class AnalyzedSqlStatement
    {
        public ParseResult ParseResult { get; set; }
        public List<string> Enhancements { get; set; } = new();
    }
    public class SqlOptimizerService : ISqlOptimizerService
    {
        public string OptimizeQuery(string sqlQuery)
        {
            // TODO: Add real optimization logic
            return $"-- Optimized (simple): {sqlQuery}";
        }

        public string OptimizeQueryWithTable(string sqlQuery, string tableDefinition)
        {
            // Parse the SQL script
            var parsedStatements = ParseWithSqlParser(sqlQuery, tableDefinition);
            
            if (!parsedStatements.IsOk || !parsedStatements.Result.IsValid)
            {
                return $"-- Error: Parsing failed";
            }

            // Analyze the statements for optimizations
            var allEnhancements = AnalyzeStatements(parsedStatements.Result.Statements, tableDefinition);
            
            // Return optimized query with suggestions
            var result = $"-- Original Query:\n{sqlQuery}\n\n";
            if (allEnhancements.Any())
            {
                result += "-- Optimization Suggestions:\n";
                result += string.Join("\n", allEnhancements.Select(e => $"-- {e}"));
            }
            else
            {
                result += "-- No optimization suggestions found.";
            }
            return result;
        }

        public (ParseResult Result, bool IsOk) ParseWithSqlParser(string sqlScript, string tableDefinition)
        {
            if (string.IsNullOrWhiteSpace(sqlScript) || string.IsNullOrWhiteSpace(tableDefinition))
            {
                return (null, false);
            }

            var table = new Table(tableDefinition);
            var parser = new Parser();
            ParseResult result = parser.Parse(sqlScript);

            if (!result.IsValid || result.Statements.Count == 0)
            {
                return (result, false);
            }

            return (result, true);
        }

        public List<string> AnalyzeStatements(List<ParsedSqlStatement> parsedStatements, string tableDefinition)
        {
            var allEnhancements = new List<string>();
            var table = new Table(tableDefinition);
            
            var parser = new Parser();
            var result = parser.Parse(string.Join("; ", parsedStatements.Select(p => p.Raw)) + ";");
            
            if (!result.IsValid || result.Statements.Count == 0)
                return allEnhancements;

            foreach (var stmt in result.Statements)
            {                    
                var enhancements = new List<string>();
                
                // SELECT
                if (stmt is SelectStatement select)
                {
                    if (stmt.Columns.Count == 1 && stmt.Columns[0].ToString() == "*")
                    {
                        enhancements.Add("Avoid SELECT *, it returns unnecessary data and slows things down.");
                    }   
                    if (stmt.Where != null)
                    {
                        var whereColumns = ExtractColumnsFromWhereClause(stmt.Where);
                        var indexedColumns = table.indexes.Select(index => index.columns.Select(col => col.name)).ToList();
                        
                        if (!whereColumns.Any(col => indexedColumns.Any(index => index.Contains(col))))
                        {
                            enhancements.Add("WHERE clause doesn't use indexed columns - consider adding indexes");
                        }
                    }
                    DetectLateWhereClauses(stmt, enhancements);
                    DetectRepeatedConditions(result.Statements, enhancements);
                }
                // INSERT
                else if (stmt is InsertStatement insert)
                {
                    DetectUnsafeStatements(stmt, StatementType.INSERT, enhancements);
                }
                // UPDATE
                else if (stmt is UpdateStatement update)
                {
                    DetectUnsafeStatements(stmt, StatementType.UPDATE, enhancements);
                }
                // DELETE
                else if (stmt is DeleteStatement delete)
                {
                    DetectUnsafeStatements(stmt, StatementType.DELETE, enhancements);
                }
                
                allEnhancements.AddRange(enhancements);
            }
            return allEnhancements;
        }

        public List<string> ExtractColumnsFromWhereClause(Expression whereExpression)
        {
            var columns = new List<string>();
            
            var columnRefs = whereExpression.GetAllColumnReferences();
            
            columns.AddRange(columnRefs.Select(colRef => colRef.Name));
            
            return columns;
        }

        public bool UsesIndexedColumns(List<string> whereColumns, List<string> indexedColumns)
        {
            return whereColumns.Any(col => indexedColumns.Contains(col, StringComparer.OrdinalIgnoreCase));
        }

        public void DetectRepeatedConditions(List<Statement> statements, List<string> enhancements)
        {
            var conditions = statements
                .OfType<SelectStatement>()
                .Where(s => s.Where != null)
                .Select(s => s.Where.ToString())
                .ToList();
            
            if (conditions.Count != conditions.Distinct().Count())
            {
                enhancements.Add("Repeated WHERE conditions found. Consider using a CTE.");
            }
        }

        public void DetectUnsafeStatements(Statement stmt, StatementType statementType, List<string> enhancements)
        {
            if (stmt.Columns == null || stmt.Columns.Count == 0)
            {
                enhancements.Add($"{statementType} statement missing column names. Use explicit column names for safety and clarity.");
            }
        }

        public void DetectLateWhereClauses(SelectStatement select, List<string> enhancements)
        {
            if (select.Joins.Any() && select.Where != null)
            {
                var whereColumns = ExtractColumnsFromWhereClause(select.Where);
                var joinTables = select.Joins.Select(j => j.Table.ToString()).ToList();
                
                if (whereColumns.All(col => IsColumnFromTable(col, select.From.First().ToString())))
                {
                    enhancements.Add("Consider moving WHERE clause before JOINs to filter data early");
                }
            }
            
        }

        private bool IsColumnFromTable(string column, string table)
        {
            return !column.Contains('.') || column.StartsWith(table + ".");
        }

        public void DetectSqlInjectionVulnerabilities(Statement stmt, List<string> enhancements)
        {
            var sql = stmt.ToString().ToLowerInvariant();
            
            if (sql.Contains("'") && sql.Contains("+"))
            {
                enhancements.Add("Potential SQL injection: String concatenation detected. Use parameterized queries.");
            }
            
            if (sql.Contains("execute") || sql.Contains("exec"))
            {
                enhancements.Add("Dynamic SQL detected. Use parameterized queries for security.");
            }
        }
    }
} 