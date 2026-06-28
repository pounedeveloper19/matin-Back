using NLog;

namespace MatinPower.Infrastructure;

public enum AuditAction { Create = 1, Update = 2, Delete = 3, Approve = 4, Reject = 5, Print = 6 }

public static class AuditLogger
{
    private static readonly Logger _logger = LogManager.GetLogger("Audit.System");

    public static void Log(int? userId, AuditAction action, string tableName, int? recordId,
        string? oldValue = null, string? newValue = null)
    {
        try
        {
            var ev = new LogEventInfo(NLog.LogLevel.Info, "Audit.System", action.ToString());
            ev.Properties["UserId"]      = userId.HasValue ? (object)userId.Value : DBNull.Value;
            ev.Properties["TableName"]   = tableName;
            ev.Properties["RecordId"]    = recordId.HasValue ? (object)recordId.Value : DBNull.Value;
            ev.Properties["AuditTypeId"] = (int)action;
            ev.Properties["OldValue"]    = (object?)oldValue ?? DBNull.Value;
            ev.Properties["NewValue"]    = (object?)newValue ?? DBNull.Value;
            _logger.Log(ev);
        }
        catch { /* audit failure must never break main operation */ }
    }
}
