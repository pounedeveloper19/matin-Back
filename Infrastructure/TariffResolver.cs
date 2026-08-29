using MatinPower.Server.Models;
using TicketManagement.Infrastructure;

namespace MatinPower.Infrastructure;

public static class TariffResolver
{
    // تعرفه‌ی معتبر برای یک ماه/سال مشخص را برمی‌گرداند: اول از تاریخچه‌ی تعرفه (چون تعرفه
    // مشتری ممکن است ماه به ماه عوض شده باشد)، و اگر تاریخچه‌ای ثبت نشده (مشتریان قدیمی)
    // به مقدار زنده‌ی پروفایل برمی‌گردد
    public static int? ResolveTariffCodeOptionId(int customerProfileId, int year, int month)
    {
        var historical = Repository<CustomerTariffOptionHistory>.Query(db =>
            db.CustomerTariffOptionHistories
                .Where(h => h.CustomerProfileId == customerProfileId &&
                    (h.EffectiveYear < year || (h.EffectiveYear == year && h.EffectiveMonth <= month)))
                .OrderByDescending(h => h.EffectiveYear)
                .ThenByDescending(h => h.EffectiveMonth)
                .Select(h => (int?)h.TariffCodeOptionId)
                .FirstOrDefault());

        if (historical.HasValue)
            return historical;

        return Repository<CustomerProfile>.GetLast(p => p.Id == customerProfileId)?.TariffCodeOptionId;
    }
}
