-- ============================================================
-- دیتابیس: MatinPower_Core
-- اضافه کردن ستون علت رد و وضعیت عدم تایید
-- ============================================================

-- 1. ستون RejectionReason به جدول Contract
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Contract' AND COLUMN_NAME = 'RejectionReason'
)
    ALTER TABLE Contract ADD RejectionReason NVARCHAR(500) NULL;

-- 2. وضعیت «عدم تایید» در EnumContractStatus
IF NOT EXISTS (SELECT 1 FROM EnumContractStatus WHERE Title = N'عدم تایید')
BEGIN
    DECLARE @newId INT = ISNULL((SELECT MAX(Id) FROM EnumContractStatus), 0) + 1;
    INSERT INTO EnumContractStatus (Id, Title) VALUES (@newId, N'عدم تایید');
END
