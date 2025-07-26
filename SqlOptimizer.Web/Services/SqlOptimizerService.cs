using System.Text;
using System.Text.RegularExpressions;
using SqlParser;
using SqlParser.Ast;

namespace SqlOptimizer.Web.Services
{
    public interface ISqlOptimizerService
    {
        string OptimizeQuery(string sqlQuery);
        string OptimizeQueryWithTable(string sqlQuery, string tableDefinition);
        List<(ParseResult,string)> ParseWithSqlParser(string sqlScript);
    }

    public class ParsedSqlStatement
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
            // TODO: Add real optimization logic using table definition
            return $"-- Optimized (table-aware): {sqlQuery}\n-- Table: {tableDefinition}";
        }
        public List<ParseResult> ParseWithSqlParser(string sqlScript, string tableDefinition)
        {
            var results = new List<ParseResult>();
            if (string.IsNullOrWhiteSpace(sqlScript) || string.IsNullOrWhiteSpace(tableDefinition)) return results;
            var table = new Table(tableDefinition);
            var parser = new Parser();
            var result = parser.Parse(sqlScript);

            if (!result.IsValid || result.Statements.Count == 0)
                return results;

            foreach (var stmt in result.Statements)
            {                    
                var enhancements = new List<string>();
                DetectSqlInjectionVulnerabilities(stmt, enhancements);
                
                // SELECT
                if (stmt is SelectStatement select)
                {
                    var enhancements = new List<string>();
                    if (stmt.Columns.Count == 1 && stmt.Columns[0].ToString() == "*")
                    {
                        enhancements.Add("Avoid SELECT *, it returns unnessesary data and slows things down.");
                    }   
                    if (select.Where != null)
                    {
                        DetectMissingIndexedColumnsInWhereClause(select.Where, table, enhancements);
                        DetectLateWhereClauses(select, enhancements);
                        DetectRepeatedConditions(result.Statements, enhancements);
                    }
                }
                // INSERT
                else if (stmt is InsertStatement insert)
                {
                    DetectUnsafeInsertStatements(insert, enhancements);
                }
                // UPDATE
                else if (stmt is UpdateStatement update)
                {
                    
                }
                // DELETE
                else if (stmt is DeleteStatement delete)
                {

                }
            }
            return results;
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

        public void DetectUnsafeInsertStatements(InsertStatement insert, List<string> enhancements)
        {
            // Check if INSERT uses VALUES without explicit column names
            if (insert.Columns == null || insert.Columns.Count == 0)
            {
                enhancements.Add("INSERT statement missing column names. Use explicit column names for safety and clarity.");
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