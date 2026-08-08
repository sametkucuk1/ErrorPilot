using Azure.Monitor.Query.Models;
using ErrorPilotEngine.Models;

namespace ErrorPilotEngine.Services;

public static class ErrorRecordMapper
{
    private const string TimestampColumn = "TimeGenerated";
    private const string TypeColumn = "ExceptionType";
    private const string MessageColumn = "OuterMessage";
    private const string ProblemIdColumn = "ProblemId";
    private const string OperationNameColumn = "OperationName";
    private const string CloudRoleNameColumn = "AppRoleName";

    public static IReadOnlyList<ErrorRecord> MapTable(LogsTable table)
    {
        var availableColumns = table.Columns
            .Select(column => column.Name)
            .ToHashSet(StringComparer.Ordinal);

        return table.Rows
            .Select(row => MapRow(row, availableColumns))
            .ToList();
    }

    private static ErrorRecord MapRow(LogsTableRow row, IReadOnlySet<string> availableColumns)
    {
        return new ErrorRecord
        {
            Timestamp = ReadDateTimeOffset(row, availableColumns, TimestampColumn),
            Type = ReadString(row, availableColumns, TypeColumn),
            Message = ReadString(row, availableColumns, MessageColumn),
            ProblemId = ReadString(row, availableColumns, ProblemIdColumn),
            OperationName = ReadString(row, availableColumns, OperationNameColumn),
            CloudRoleName = ReadString(row, availableColumns, CloudRoleNameColumn),
        };
    }

    private static string? ReadString(
        LogsTableRow row,
        IReadOnlySet<string> availableColumns,
        string columnName)
    {
        return availableColumns.Contains(columnName) ? row.GetString(columnName) : null;
    }

    private static DateTimeOffset? ReadDateTimeOffset(
        LogsTableRow row,
        IReadOnlySet<string> availableColumns,
        string columnName)
    {
        return availableColumns.Contains(columnName) ? row.GetDateTimeOffset(columnName) : null;
    }
}
