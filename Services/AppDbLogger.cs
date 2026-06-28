using System.Data;
using System.Data.SqlClient;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Services
{
    public static class AppDbLogger
    {
        private static readonly string? _connStr =
            Utilities.GetValueFromConfiguration("ConnectionStrings:LogDbConnection");

        public static void Info(string logger, string action, string message) =>
            Write("INFO", logger, action, message, null);

        public static void Error(string logger, string action, string message, Exception? ex = null) =>
            Write("ERROR", logger, action, message, ex?.ToString());

        public static void Write(string level, string logger, string action, string message, string? exception)
        {
            if (string.IsNullOrEmpty(_connStr)) return;
            try
            {
                using var conn = new SqlConnection(_connStr);
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO ApplicationLogs (LoggedAt, Level, Logger, Message, Exception, Action)
                    VALUES (GETDATE(), @Level, @Logger, @Message, @Exception, @Action)";
                cmd.Parameters.Add("@Level",     SqlDbType.NVarChar).Value = level;
                cmd.Parameters.Add("@Logger",    SqlDbType.NVarChar).Value = logger;
                cmd.Parameters.Add("@Message",   SqlDbType.NVarChar).Value = message;
                cmd.Parameters.Add("@Exception", SqlDbType.NVarChar).Value = (object?)exception ?? DBNull.Value;
                cmd.Parameters.Add("@Action",    SqlDbType.NVarChar).Value = action;
                cmd.ExecuteNonQuery();
            }
            catch { /* log failure must never break the caller */ }
        }
    }
}
