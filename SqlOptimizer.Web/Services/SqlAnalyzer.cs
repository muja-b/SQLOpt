using System.Text;
using System.Text.RegularExpressions;
using SqlParser.Ast;
using SqlParser;
using SqlOptimizer.Web.Models;

namespace SqlOptimizer.Web.Services
{
    public class SqlAnalyzer : ISqlAnalyzer
    {
        public List<string> AnalyzeStatements(ParseResult sqlResult, Table tableResult)
        {
            var allEnhancements = new List<string>();
            
            // Use the parsed statements directly from sqlResult
            foreach (var stmt in sqlResult.Statements)
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
                        var indexedColumns = tableResult.indexes.Select(index => index.columns.Select(col => col.name)).ToList();
                        
                        if (!whereColumns.Any(col => indexedColumns.Any(index => index.Contains(col))))
                        {
                            enhancements.Add("WHERE clause doesn't use indexed columns - consider adding indexes");
                        }
                    }
                    DetectLateWhereClauses(stmt, enhancements);
                    DetectRepeatedConditions(sqlResult.Statements, enhancements);
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

        private List<string> ExtractColumnsFromWhereClause(Expression whereExpression)
        {
            var columns = new List<string>();
            var columnRefs = whereExpression.GetAllColumnReferences();
            columns.AddRange(columnRefs.Select(colRef => colRef.Name));
            return columns;
        }

        private bool UsesIndexedColumns(List<string> whereColumns, List<string> indexedColumns)
        {
            return whereColumns.Any(col => indexedColumns.Contains(col, StringComparer.OrdinalIgnoreCase));
        }

        private void DetectRepeatedConditions(List<Statement> statements, List<string> enhancements)
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

        private void DetectUnsafeStatements(Statement stmt, StatementType statementType, List<string> enhancements)
        {
            if (stmt.Columns == null || stmt.Columns.Count == 0)
            {
                enhancements.Add($"{statementType} statement missing column names. Use explicit column names for safety and clarity.");
            }
        }

        private void DetectLateWhereClauses(Statement stmt, List<string> enhancements)
        {
            if (stmt is SelectStatement select && select.Joins.Any() && select.Where != null)
            {
                var whereColumns = ExtractColumnsFromWhereClause(select.Where);
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
    }
} 