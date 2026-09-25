using System.Runtime.InteropServices;

namespace IMDataCore.DataMigrationTool.Interop;

internal sealed class WinSqliteDatabase : IDisposable
{
    private IntPtr _db;

    public WinSqliteDatabase(string filePath)
    {
        int rc = Native.sqlite3_open16(filePath, out _db);
        if (rc != 0 || _db == IntPtr.Zero)
            throw new InvalidDataException("winsqlite3 could not open copied legacy database (rc=" + rc + ").");
    }

    public List<Dictionary<string, object?>> Query(string sql)
    {
        IntPtr stmt = IntPtr.Zero;
        int rc = Native.sqlite3_prepare16_v2(_db, sql, -1, out stmt, IntPtr.Zero);
        if (rc != 0 || stmt == IntPtr.Zero) throw Error("SQLite prepare failed", rc);
        try
        {
            var rows = new List<Dictionary<string, object?>>();
            int columnCount = Native.sqlite3_column_count(stmt);
            while ((rc = Native.sqlite3_step(stmt)) == 100)
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < columnCount; i++)
                {
                    string name = Marshal.PtrToStringUni(Native.sqlite3_column_name16(stmt, i)) ?? ("c" + i);
                    int type = Native.sqlite3_column_type(stmt, i);
                    object? value = type switch
                    {
                        1 => Native.sqlite3_column_int64(stmt, i),
                        2 => Native.sqlite3_column_double(stmt, i),
                        3 => Marshal.PtrToStringUni(Native.sqlite3_column_text16(stmt, i)),
                        5 => null,
                        _ => null
                    };
                    row[name] = value;
                }
                rows.Add(row);
            }
            if (rc != 101) throw Error("SQLite step failed", rc);
            return rows;
        }
        finally { if (stmt != IntPtr.Zero) Native.sqlite3_finalize(stmt); }
    }

    public bool TableExists(string tableName)
    {
        string sql = "SELECT name FROM sqlite_master WHERE type='table' AND name=" + QuoteLiteral(tableName) + " LIMIT 1;";
        return Query(sql).Count > 0;
    }

    public static string QuoteLiteral(string value) => "'" + (value ?? "").Replace("'", "''") + "'";
    public static string QuoteIdentifier(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";

    private Exception Error(string prefix, int rc)
    {
        string message = _db == IntPtr.Zero ? "" : Marshal.PtrToStringUni(Native.sqlite3_errmsg16(_db)) ?? "";
        return new InvalidDataException($"{prefix} (rc={rc}): {message}");
    }

    public void Dispose()
    {
        if (_db != IntPtr.Zero) { Native.sqlite3_close(_db); _db = IntPtr.Zero; }
    }

    private static class Native
    {
        [DllImport("winsqlite3", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_open16(string filename, out IntPtr db);
        [DllImport("winsqlite3", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_prepare16_v2(IntPtr db, string sql, int nByte, out IntPtr stmt, IntPtr tail);
        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_step(IntPtr stmt);
        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_finalize(IntPtr stmt);
        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_close(IntPtr db);
        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_column_count(IntPtr stmt);
        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr sqlite3_column_name16(IntPtr stmt, int col);
        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] internal static extern int sqlite3_column_type(IntPtr stmt, int col);
        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] internal static extern long sqlite3_column_int64(IntPtr stmt, int col);
        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] internal static extern double sqlite3_column_double(IntPtr stmt, int col);
        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr sqlite3_column_text16(IntPtr stmt, int col);
        [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)] internal static extern IntPtr sqlite3_errmsg16(IntPtr db);
    }
}
