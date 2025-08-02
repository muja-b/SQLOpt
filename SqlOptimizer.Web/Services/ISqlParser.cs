using SqlOptimizer.Web.Services;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using System.Text.RegularExpressions;

namespace SqlOptimizer.Web.Services
{
    // Internal interface - only used by SqlAnalyzer internally
    internal interface ISqlParser
    {
        // Extract column names from WHERE clauses
        List<string> ExtractColumnsFromWhere(string whereClause);
        
        // Extract indexed columns from table definition
        List<string> ExtractIndexedColumns(string tableDefinition);
        
        // Check if a word is a SQL reserved word
        bool IsReservedWord(string word);
        
        // Extract WHERE clauses from SQL
        List<string> ExtractWhereClauses(string sql);
        
        // Validate SQL syntax (basic validation)
        bool IsValidSql(string sql);
        
        // Extract table name from SQL
        string ExtractTableName(string sql);
    }
} 