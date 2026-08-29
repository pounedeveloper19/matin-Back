-- ============================================================
-- دیتابیس: MatinPower_Core
-- تغییر مبنای CK_Contract_VolumeNotExceedCapacity از ماهانه (×720 ساعت)
-- به سالانه (×8760 ساعت) — چون حجم درخواستی قرارداد از فرمول
-- «قدرت درخواستی (kW) × 8760 ساعت (یک سال)» محاسبه می‌شود و مدت
-- قرارداد هم پیش‌فرض یک سال است (تاریخ شروع + ۱ سال = تاریخ پایان).
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Contract_VolumeNotExceedCapacity'
)
    ALTER TABLE dbo.Contracts DROP CONSTRAINT CK_Contract_VolumeNotExceedCapacity;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Contract_VolumeNotExceedCapacity'
)
    ALTER TABLE dbo.Contracts WITH CHECK ADD CONSTRAINT CK_Contract_VolumeNotExceedCapacity
        CHECK ([ContractVolumeKwh] IS NULL OR [ContractPowerKw] IS NULL OR [ContractVolumeKwh] <= [ContractPowerKw] * (8760));

ALTER TABLE dbo.Contracts CHECK CONSTRAINT CK_Contract_VolumeNotExceedCapacity;
