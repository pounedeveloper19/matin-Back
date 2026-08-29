-- ============================================================
-- دیتابیس: MatinPower_Core
-- حذف یکتایی مطلق روی شناسه ملی/کد ملی مشتریان، تا مشتری
-- غیرفعال‌شده بتواند دوباره با همان کد ملی/شناسه ملی ثبت‌نام کند.
-- ستون IsActive روی خودِ این جداول نیست (روی CustomerProfile است)
-- پس امکان ساخت filtered unique index مثل Users.Mobile نبود؛
-- یکتایی بین مشتریان فعال از این پس فقط در سطح اپلیکیشن چک می‌شود.
-- ایندکس (غیر یکتا) برای سرعت جستجو نگه داشته می‌شود.
-- ============================================================

-- 1. Customers_Real.NationalCode
DECLARE @constraintName NVARCHAR(200);
SELECT @constraintName = kc.name
FROM sys.key_constraints kc
JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID('dbo.Customers_Real') AND c.name = 'NationalCode' AND kc.type = 'UQ';

IF @constraintName IS NOT NULL
BEGIN
    DECLARE @dropSql1 NVARCHAR(400) = N'ALTER TABLE dbo.Customers_Real DROP CONSTRAINT ' + QUOTENAME(@constraintName);
    EXEC sp_executesql @dropSql1;
END

DECLARE @indexName NVARCHAR(200);
SELECT TOP 1 @indexName = i.name
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.Customers_Real') AND i.is_unique = 1 AND i.is_primary_key = 0 AND c.name = 'NationalCode';

IF @indexName IS NOT NULL
BEGIN
    DECLARE @dropIdxSql1 NVARCHAR(400) = N'DROP INDEX ' + QUOTENAME(@indexName) + N' ON dbo.Customers_Real';
    EXEC sp_executesql @dropIdxSql1;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_Real_NationalCode' AND object_id = OBJECT_ID('dbo.Customers_Real'))
    CREATE INDEX IX_Customers_Real_NationalCode ON dbo.Customers_Real(NationalCode);

-- 2. Customers_Legal.NationalId
DECLARE @constraintName2 NVARCHAR(200);
SELECT @constraintName2 = kc.name
FROM sys.key_constraints kc
JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE kc.parent_object_id = OBJECT_ID('dbo.Customers_Legal') AND c.name = 'NationalId' AND kc.type = 'UQ';

IF @constraintName2 IS NOT NULL
BEGIN
    DECLARE @dropSql2 NVARCHAR(400) = N'ALTER TABLE dbo.Customers_Legal DROP CONSTRAINT ' + QUOTENAME(@constraintName2);
    EXEC sp_executesql @dropSql2;
END

DECLARE @indexName2 NVARCHAR(200);
SELECT TOP 1 @indexName2 = i.name
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.Customers_Legal') AND i.is_unique = 1 AND i.is_primary_key = 0 AND c.name = 'NationalId';

IF @indexName2 IS NOT NULL
BEGIN
    DECLARE @dropIdxSql2 NVARCHAR(400) = N'DROP INDEX ' + QUOTENAME(@indexName2) + N' ON dbo.Customers_Legal';
    EXEC sp_executesql @dropIdxSql2;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_Legal_NationalId' AND object_id = OBJECT_ID('dbo.Customers_Legal'))
    CREATE INDEX IX_Customers_Legal_NationalId ON dbo.Customers_Legal(NationalId);
