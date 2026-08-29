-- ============================================================
-- دیتابیس: MatinPower_Core
-- یکتایی شماره موبایل کاربران (Users.Mobile) را از حالت مطلق
-- به «یکتا فقط بین کاربران فعال» تغییر می‌دهد، تا وقتی کاربری
-- غیرفعال شد بتوان دوباره با همان موبایل ثبت‌نام/ایجاد کرد.
-- ============================================================

-- 1. حذف Unique Constraint قبلی روی Mobile (در صورت وجود)
DECLARE @constraintName NVARCHAR(200);
SELECT @constraintName = kc.name
FROM sys.key_constraints kc
JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID('dbo.Users') AND c.name = 'Mobile' AND kc.type = 'UQ';

IF @constraintName IS NOT NULL
BEGIN
    DECLARE @dropSql NVARCHAR(400) = N'ALTER TABLE dbo.Users DROP CONSTRAINT ' + QUOTENAME(@constraintName);
    EXEC sp_executesql @dropSql;
END

-- 2. حذف Unique Index قبلی روی Mobile در صورتی که به‌صورت Index (نه Constraint) ساخته شده باشد
DECLARE @indexName NVARCHAR(200);
SELECT TOP 1 @indexName = i.name
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.Users') AND i.is_unique = 1 AND i.is_primary_key = 0 AND c.name = 'Mobile';

IF @indexName IS NOT NULL
BEGIN
    DECLARE @dropIdxSql NVARCHAR(400) = N'DROP INDEX ' + QUOTENAME(@indexName) + N' ON dbo.Users';
    EXEC sp_executesql @dropIdxSql;
END

-- 3. ایجاد Unique Filtered Index: موبایل فقط بین کاربران فعال باید یکتا باشد
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'UQ_Users_Mobile_ActiveOnly' AND object_id = OBJECT_ID('dbo.Users')
)
    CREATE UNIQUE INDEX UQ_Users_Mobile_ActiveOnly ON dbo.Users(Mobile) WHERE IsActive = 1;
