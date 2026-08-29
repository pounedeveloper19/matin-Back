-- ============================================================
-- دیتابیس: MatinPower_Core
-- جدول تاریخچه‌ی کد تعرفه‌ی مشتری. تا امروز CustomerProfile.TariffCodeOptionId
-- فقط یک مقدار زنده بود و هر تغییر، مقدار قبلی را کامل بازنویسی می‌کرد —
-- یعنی اگر مشتری تعرفه‌اش را عوض می‌کرد، تحلیل ماه‌های گذشته هم با تعرفه‌ی
-- جدید (اشتباه) محاسبه می‌شد. این جدول نگه می‌دارد که هر تعرفه از چه
-- ماه/سالی معتبر بوده، تا تحلیل هر ماه با تعرفه‌ی واقعاً معتبر همان ماه
-- انجام شود.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CustomerTariffOptionHistory')
BEGIN
    CREATE TABLE dbo.CustomerTariffOptionHistory (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        CustomerProfileId   INT NOT NULL,
        TariffCodeOptionId  INT NOT NULL,
        EffectiveYear       INT NOT NULL,
        EffectiveMonth      INT NOT NULL,
        CreatedAt           DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_CustomerTariffOptionHistory_CustomerProfile
            FOREIGN KEY (CustomerProfileId) REFERENCES dbo.CustomerProfiles(Id),
        CONSTRAINT FK_CustomerTariffOptionHistory_TariffCodeOption
            FOREIGN KEY (TariffCodeOptionId) REFERENCES dbo.TariffCodeOptions(Id)
    );

    CREATE UNIQUE INDEX UQ_CustomerTariffOptionHistory_Profile_Period
        ON dbo.CustomerTariffOptionHistory (CustomerProfileId, EffectiveYear, EffectiveMonth);

    -- برای مشتری‌هایی که از قبل تعرفه تنظیم کرده‌اند، یک رکورد تاریخچه با
    -- یک سال شمسی قدیمی و مطمئن (۱۳۸۰) ثبت می‌شود تا به‌عنوان قدیمی‌ترین
    -- تعرفه‌ی شناخته‌شده برای همه‌ی ماه‌های گذشته‌شان در نظر گرفته شود
    INSERT INTO dbo.CustomerTariffOptionHistory (CustomerProfileId, TariffCodeOptionId, EffectiveYear, EffectiveMonth)
    SELECT Id, TariffCodeOptionId, 1380, 1
    FROM dbo.CustomerProfiles
    WHERE TariffCodeOptionId IS NOT NULL;
END
