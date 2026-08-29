-- ============================================================
-- کوئری موقت (نه migration) — جستجوی مقدار 10186 در همه ستون‌های
-- عددی همه جدول‌های دیتابیس MatinPower_Core.
-- فقط برای اجرا در SSMS جهت یافتن منبع مقدار 10186 — بعد از استفاده حذف کنید.
-- ============================================================

DECLARE @SearchValue DECIMAL(18,4) = 10186;
DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql + N'
SELECT ''' + t.TABLE_NAME + N''' AS TableName, ''' + c.COLUMN_NAME + N''' AS ColumnName, [Id], [' + c.COLUMN_NAME + N'] AS MatchedValue
FROM [dbo].[' + t.TABLE_NAME + N']
WHERE ROUND([' + c.COLUMN_NAME + N'], 0) = ' + CAST(@SearchValue AS NVARCHAR(20)) + N'
UNION ALL '
FROM INFORMATION_SCHEMA.TABLES t
JOIN INFORMATION_SCHEMA.COLUMNS c
    ON t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
WHERE t.TABLE_TYPE = 'BASE TABLE'
  AND t.TABLE_SCHEMA = 'dbo'
  AND c.DATA_TYPE IN ('int','bigint','smallint','tinyint','decimal','numeric','float','real','money','smallmoney')
  AND EXISTS ( -- فقط جدول‌هایی که ستون Id دارند (قرارداد استاندارد این پروژه)
      SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS c2
      WHERE c2.TABLE_NAME = t.TABLE_NAME AND c2.TABLE_SCHEMA = t.TABLE_SCHEMA AND c2.COLUMN_NAME = 'Id'
  );

IF LEN(@sql) > 0
BEGIN
    SET @sql = LEFT(@sql, LEN(@sql) - LEN('UNION ALL '));
    SET @sql = @sql + N' ORDER BY TableName, ColumnName';
    EXEC sp_executesql @sql;
END
