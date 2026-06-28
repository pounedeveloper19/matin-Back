namespace MatinPower.Server.Application.Calculation;

/// <summary>
/// Single source of truth for all calculation constants.
/// NEVER reference magic numbers directly in business logic — always use this class.
///
/// When a regulation changes (e.g. Article16 percentage for a new year):
///   1. Add the new constant here
///   2. Update GreenLawPercent() in BillCalculationController
///   3. The change is auditable via git history
///
/// Future: migrate to EnergyCalculationParameters DB table with EffectiveDate column.
/// </summary>
public static class CalculationConstants
{
    // ── ضرایب جریمه و بستانکاری (پیش‌فرض — از TariffCodeOption override می‌شود) ──
    public const decimal DefaultPenaltyMultiplier = 1.3m;
    public const decimal DefaultCreditMultiplier  = 0.75m;

    // ── ضرایب نرخ TOU — فقط برای نرخ‌های ذخیره‌شده در TariffCodeOptionRate ─
    // این ضرایب دیگر برای محاسبه بهای انرژی شبکه استفاده نمی‌شوند.
    // نرخ بهای انرژی شبکه = MarketRate × penaltyMultiplier (بدون هاردکد).
    // این ثوابت فقط برای راهنمایی ورود داده توسط ادمین نگه‌داشته شده‌اند.
    public const decimal TouPeakMultiplier    = 2.0m;   // اوج = ۲ × میان
    public const decimal TouMidMultiplier     = 1.0m;   // میان = پایه
    public const decimal TouLowMultiplier     = 0.5m;   // کم = نصف میان

    // ── آستانه دیماند برای مشمولیت ماده ۱۶ (کیلووات) ────────────────
    public const decimal Article16DemandThresholdKw = 1000m;

    // ── درصد مشمول قانون جهش تولید (ماده ۱۶) بر اساس سال شمسی ────────
    public const decimal Article16Percent1404 = 0.03m;  //  ۳٪ — سال ۱۴۰۴
    public const decimal Article16Percent1405 = 0.04m;  //  ۴٪ — سال ۱۴۰۵
    public const decimal Article16PercentDefault = 0.05m; //  ۵٪ — سال‌های بعد

    /// <summary>
    /// درصد مشمول قانون جهش تولید برای سال و تعرفه مشخص.
    /// منطق: 4-ب معاف است؛ دیماند ≤ ۱۰۰۰ کیلووات معاف است؛ بقیه بر اساس سال.
    /// </summary>
    public static decimal GetArticle16Percent(bool isType4B, decimal actualDemandKw, int jalaliYear)
    {
        if (isType4B || actualDemandKw <= Article16DemandThresholdKw)
            return 0m;

        return jalaliYear switch
        {
            1404 => Article16Percent1404,
            1405 => Article16Percent1405,
            _    => Article16PercentDefault,
        };
    }

    // ── ساعات پیش‌فرض TOU (وقتی از DB پیدا نشد) ─────────────────────
    public const int DefaultPeakHoursPerDay = 5;
    public const int DefaultMidHoursPerDay  = 12;
    public const int DefaultLowHoursPerDay  = 7;

    // ── تقویم جلالی ───────────────────────────────────────────────────
    /// <summary>تعداد روزهای هر ماه شمسی (کبیسه در نظر گرفته نشده — ماه ۱۲ همیشه ۲۹)</summary>
    public static int DaysInJalaliMonth(int month) =>
        month <= 6 ? 31 : month <= 11 ? 30 : 29;
}
