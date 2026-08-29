-- ============================================================
-- دیتابیس: MatinPower_Core (فقط لوکال — طبق درخواست)
-- ۱. حذف تمام برنامه‌های TOU بجز شرکت «برق منطقه‌ای تهران»
-- ۲. برای تمام شرکت‌های برق دیگر، دقیقاً همان برنامه‌ی TOU
--    (همان ماه‌ها و همان ساعت‌ها) که برای «برق منطقه‌ای تهران»
--    ثبت شده، کپی می‌شود.
-- این اسکریپت idempotent است — هر بار که اجرا شود، ابتدا داده‌های
-- قدیمی شرکت‌های غیر تهرانی را پاک و دوباره از روی تهران می‌سازد.
-- ============================================================

DECLARE @tehranEntityId INT;
SELECT TOP 1 @tehranEntityId = Id
FROM dbo.PowerEntities
WHERE Name LIKE N'%منطقه%تهران%';

IF @tehranEntityId IS NULL
BEGIN
    RAISERROR(N'شرکت «برق منطقه‌ای تهران» در جدول PowerEntities پیدا نشد — اسکریپت متوقف شد.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.TOUSchedules WHERE PowerEntityId = @tehranEntityId)
BEGIN
    RAISERROR(N'شرکت «برق منطقه‌ای تهران» هیچ برنامه TOU ثبت‌شده‌ای ندارد — چیزی برای کپی وجود ندارد.', 16, 1);
    RETURN;
END

-- 1. حذف TOU همه‌ی شرکت‌های دیگر
DELETE FROM dbo.TOUSchedules WHERE PowerEntityId <> @tehranEntityId;

-- 2. کپی برنامه‌ی تهران برای تمام شرکت‌های دیگر (فعال و غیرفعال، همه)
INSERT INTO dbo.TOUSchedules (PowerEntityId, MonthNumber, HourNumber, ToutypeId)
SELECT pe.Id, t.MonthNumber, t.HourNumber, t.ToutypeId
FROM dbo.PowerEntities pe
CROSS JOIN dbo.TOUSchedules t
WHERE pe.Id <> @tehranEntityId
  AND t.PowerEntityId = @tehranEntityId;

SELECT
    (SELECT COUNT(*) FROM dbo.PowerEntities WHERE Id <> @tehranEntityId) AS CompaniesUpdated,
    (SELECT COUNT(*) FROM dbo.TOUSchedules WHERE PowerEntityId = @tehranEntityId) AS RowsPerCompany;
