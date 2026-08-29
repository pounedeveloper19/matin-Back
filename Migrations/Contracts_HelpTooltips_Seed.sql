-- ============================================================
-- دیتابیس: MatinPower_Core
-- افزودن محتوای راهنمای داینامیک (PageTooltip) برای صفحه‌ی
-- «قراردادهای من» در پنل مشتری (pageKey = 'customer-contracts')
-- این اسکریپت idempotent است، اگر یک ردیف با همین
-- PageKey/FieldKey از قبل وجود داشته باشد رد می‌شود.
-- ============================================================

DECLARE @rows TABLE (FieldKey NVARCHAR(100), Title NVARCHAR(200), Content NVARCHAR(1000));

INSERT INTO @rows (FieldKey, Title, Content) VALUES
(N'startDate',          N'تاریخ شروع',           N'تاریخی که اعتبار این قرارداد از آن آغاز می‌شود.'),
(N'endDate',            N'تاریخ پایان',          N'تاریخی که اعتبار این قرارداد در آن به پایان می‌رسد. برای تمدید، پیش از این تاریخ با پشتیبانی تماس بگیرید.'),
(N'contractRate',       N'نرخ قرارداد',          N'نرخ توافقی خرید هر کیلووات‌ساعت برق (ریال/kWh) طبق این قرارداد؛ همان نرخی که صرفه‌جویی شما نسبت به تعرفه عادی برق از آن محاسبه می‌شود.'),
(N'contractPowerKw',    N'قدرت قرارداد',         N'حداکثر توان مجاز (kW) در این قرارداد.'),
(N'contractVolumeKwh',  N'حجم قرارداد',          N'مقدار کل انرژی (kWh) که طی این قرارداد برای شما تامین می‌شود.'),
(N'contractAmountRial', N'مبلغ قرارداد',         N'مبلغ کل مالی این قرارداد به ریال که باید طبق مهلت پرداخت تسویه شود.'),
(N'paymentDeadline',    N'مهلت پرداخت',          N'آخرین تاریخی که باید مبلغ این قرارداد را پرداخت کنید. پس از این تاریخ ممکن است قرارداد لغو یا مشمول جریمه شود.'),
(N'warrantyType',       N'نوع ضمانت‌نامه',       N'نوع سند تضمینی که ارائه می‌دهید، مثلاً چک، سفته یا ضمانت‌نامه بانکی. بدون ثبت ضمانت‌نامه، قرارداد نهایی نمی‌شود.'),
(N'warrantyAmount',     N'مبلغ ضمانت',           N'مبلغ سند ضمانتی (چک/سفته/ضمانت‌نامه بانکی) که بابت تعهدات این قرارداد نزد شرکت می‌سپارید.'),
(N'warrantyFile',       N'مدرک ضمانت‌نامه',      N'تصویر یا اسکن سند ضمانت (چک، سفته یا ضمانت‌نامه بانکی) را اینجا بارگذاری کنید تا برای بررسی ادمین ارسال شود.');

INSERT INTO PageTooltip (PageKey, FieldKey, Title, Content, IsActive, CreatedAt)
SELECT N'customer-contracts', r.FieldKey, r.Title, r.Content, 1, GETDATE()
FROM @rows r
WHERE NOT EXISTS (
    SELECT 1 FROM PageTooltip pt
    WHERE pt.PageKey = N'customer-contracts' AND pt.FieldKey = r.FieldKey
);
