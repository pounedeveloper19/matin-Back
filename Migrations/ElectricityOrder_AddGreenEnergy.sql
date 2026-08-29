-- ============================================================
-- دیتابیس: MatinPower_Core
-- افزودن ستون IsGreenEnergy به ElectricityOrders تا مشخص شود
-- مشتری در این سفارش برق سبز هم درخواست کرده (۴٪ از مقدار
-- درخواستی به‌عنوان برق سبز و ۹۶٪ به‌عنوان برق عادی محاسبه می‌شود).
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ElectricityOrders' AND COLUMN_NAME = 'IsGreenEnergy'
)
    ALTER TABLE dbo.ElectricityOrders ADD IsGreenEnergy BIT NOT NULL DEFAULT 0;
