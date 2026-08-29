-- ============================================================
-- دیتابیس: MatinPower_Core
-- افزودن ستون‌های BillYear/BillMonth به ElectricityOrders تا مشخص
-- شود سفارش برای کدام ماه/سال (شمسی) مصرف ثبت شده — برای این‌که
-- بتوان نرخ پیش‌فرض پیش‌فاکتور را بر اساس قرارداد فعال همان ماه
-- محاسبه کرد.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ElectricityOrders' AND COLUMN_NAME = 'BillYear'
)
    ALTER TABLE dbo.ElectricityOrders ADD BillYear INT NULL;

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ElectricityOrders' AND COLUMN_NAME = 'BillMonth'
)
    ALTER TABLE dbo.ElectricityOrders ADD BillMonth INT NULL;
