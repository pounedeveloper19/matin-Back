using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Customer
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class OrderController : BaseController
    {
        private int? GetUserId() => new UseContext(new HttpContextAccessor()).GetUserId();

        [HttpGet]
        public ExecutionResult GetMyOrders()
        {
            var userId = GetUserId();
            if (userId == null) return new ExecutionResult(ResultType.Danger, "خطا", "احراز هویت نشده", 401);

            return RunExceptionProof(() =>
                Repository<ElectricityOrder>.Query(db =>
                    (object)db.ElectricityOrders
                        .Where(o => o.UserId == userId.Value)
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => new
                        {
                            o.Id,
                            o.BillId,
                            BillIdentifier = o.Bill.Subscription.BillIdentifier ?? "",
                            o.RequestedKwh,
                            EnergyType = o.EnergyType.Title ?? "",
                            o.EnergyTypeId,
                            o.PriceAtMoment,
                            Status = o.Status.Title ?? "",
                            o.StatusId,
                            OrderDate = o.OrderDate != null ? o.OrderDate.Value.ToString("yyyy-MM-dd") : null,
                            o.IsPriceRequest,
                            o.IsGreenEnergy,
                            PaymentCount = o.Payments.Count,
                            LastPaymentStatusId = o.Payments.OrderByDescending(p => p.CreatedAt)
                                .Select(p => (int?)p.StatusId).FirstOrDefault(),
                        })
                        .ToList()));
        }

        [HttpGet("{id}")]
        public ExecutionResult GetOrderDetail(int id)
        {
            var userId = GetUserId();
            if (userId == null) return new ExecutionResult(ResultType.Danger, "خطا", "احراز هویت نشده", 401);

            return RunExceptionProof(() =>
                Repository<ElectricityOrder>.Query(db =>
                    (object?)db.ElectricityOrders
                        .Where(o => o.Id == id && o.UserId == userId.Value)
                        .Select(o => new
                        {
                            o.Id,
                            o.BillId,
                            BillIdentifier = o.Bill.Subscription.BillIdentifier ?? "",
                            o.RequestedKwh,
                            EnergyType = o.EnergyType.Title ?? "",
                            o.EnergyTypeId,
                            o.PriceAtMoment,
                            Status = o.Status.Title ?? "",
                            o.StatusId,
                            OrderDate = o.OrderDate != null ? o.OrderDate.Value.ToString("yyyy-MM-dd") : null,
                            o.IsPriceRequest,
                            o.IsGreenEnergy,
                            Payments = o.Payments.Select(p => new
                            {
                                p.Id,
                                p.Amount,
                                Method = p.Method.Title ?? "",
                                p.MethodId,
                                Status = p.Status.Title ?? "",
                                p.StatusId,
                                p.ReferenceNumber,
                                p.ReceiptFileId,
                                CreatedAt = p.CreatedAt != null ? p.CreatedAt.Value.ToString("yyyy-MM-dd") : null,
                            }).ToList(),
                        })
                        .FirstOrDefault()));
        }

        [HttpPost]
        public ExecutionResult CreateOrder([FromBody] CreateOrderRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return new ExecutionResult(ResultType.Danger, "خطا", "احراز هویت نشده", 401);

            if (req.RequestedKwh <= 0)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "مقدار درخواستی باید بزرگتر از صفر باشد.", 400);

            var profileId = new UseContext(new HttpContextAccessor()).GetCustomerId();

            var subscription = Repository<Subscription>.Query(db =>
                db.Subscriptions
                    .Where(s => s.Id == req.SubscriptionId && s.Address.CustomerProfileId == profileId)
                    .Select(s => new { s.Id, s.AddressId })
                    .FirstOrDefault());
            if (subscription == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "اشتراک یافت نشد.", 404);

            var address = Repository<Address>.Query(db =>
                db.Addresses
                    .Where(a => a.Id == subscription.AddressId)
                    .Select(a => new { a.CustomerProfileId, a.PowerEntityId })
                    .FirstOrDefault());
            if (address == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "آدرس اشتراک یافت نشد.", 404);

            var profile = Repository<CustomerProfile>.Query(db =>
                db.CustomerProfiles
                    .Where(p => p.Id == address.CustomerProfileId)
                    .Select(p => new { p.CustomerTypeId })
                    .FirstOrDefault());

            // تعرفه (Tariff) صرفاً برای ثبت اطلاعاتی روی Bill است و در هیچ محاسبه‌ای استفاده نمی‌شود؛
            // نبودِ آن (که معمولاً یعنی ادمین برای این شرکت برق هنوز تعرفه ثبت نکرده) نباید جلوی ثبت سفارش را بگیرد
            var tariff = Repository<Tariff>.Query(db =>
                db.Tariffs
                    .Where(t => t.CustomerTypeId == profile!.CustomerTypeId && t.PowerEntitiesId == address.PowerEntityId)
                    .Select(t => new { t.TariffId })
                    .FirstOrDefault());

            return RunExceptionProof(() =>
            {
                var bill = Repository<Bill>.InsertItem(new Bill
                {
                    SubscriptionId = req.SubscriptionId,
                    TariffId = tariff?.TariffId,
                    CreatedAt = DateTime.Now,
                });

                var order = Repository<ElectricityOrder>.InsertItem(new ElectricityOrder
                {
                    BillId = bill.BillId,
                    UserId = userId.Value,
                    RequestedKwh = req.RequestedKwh,
                    EnergyTypeId = req.EnergyTypeId,
                    PriceAtMoment = 0,
                    StatusId = 1,
                    OrderDate = DateTime.Now,
                    IsPriceRequest = req.IsPriceRequest,
                    BillYear = req.Year,
                    BillMonth = req.Month,
                    IsGreenEnergy = req.IsGreenEnergy,
                });

                return (object)order.Id;
            });
        }

        [HttpGet("{orderId}")]
        public ExecutionResult GetProformaData(int orderId)
        {
            var userId = GetUserId();
            if (userId == null) return new ExecutionResult(ResultType.Danger, "خطا", "احراز هویت نشده", 401);

            var order = Repository<ElectricityOrder>.Query(db =>
                db.ElectricityOrders
                    .Where(o => o.Id == orderId && o.UserId == userId.Value)
                    .Select(o => new
                    {
                        o.Id,
                        o.RequestedKwh,
                        o.PriceAtMoment,
                        o.BillYear,
                        o.BillMonth,
                        o.IsGreenEnergy,
                        SubscriptionId = o.Bill.SubscriptionId,
                        EnergyType = o.EnergyType.Title,
                        OrderDate = o.OrderDate != null
                            ? PersianDateConverter.ToPersianDate(o.OrderDate.Value, "yyyy/MM/dd")
                            : null,
                        BillIdentifier = o.Bill.Subscription.BillIdentifier,
                        BuyerName = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                            ?? ((o.Bill.Subscription.Address.CustomerProfile.CustomersReal.FirstName ?? "") + " " +
                                (o.Bill.Subscription.Address.CustomerProfile.CustomersReal.LastName ?? "")),
                        NationalId = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.NationalId
                            ?? o.Bill.Subscription.Address.CustomerProfile.CustomersReal.NationalCode,
                        EconomicCode   = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.EconomicCode,
                        RegisterNumber = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.RegisterNumber,
                        Phone = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CeoMobile
                            ?? o.Bill.Subscription.Address.CustomerProfile.CustomersReal.Mobile,
                        Address  = o.Bill.Subscription.Address.MainAddress,
                        City     = o.Bill.Subscription.Address.City.Title,
                        Province = o.Bill.Subscription.Address.City.Province.Name,
                        PostalCode = o.Bill.Subscription.Address.PostalCode,
                    })
                    .FirstOrDefault());

            if (order == null)
                return new ExecutionResult(ResultType.Danger, "یافت نشد", "سفارش مورد نظر یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                decimal rate = 0;

                // ۱) نرخ بازار (MarketPeak) برای ماه/سال همین سفارش — یعنی ادمین برای آن ماه
                //    نرخ ثبت کرده (جدول MonthlyMarketRates، نه ElectricityOrder.PriceAtMoment).
                if (order.BillYear.HasValue && order.BillMonth.HasValue)
                {
                    var marketRate = Repository<MonthlyMarketRate>.Query(db =>
                        db.MonthlyMarketRates
                            .Where(m => m.Year == order.BillYear && m.Month == order.BillMonth)
                            .Select(m => (decimal?)m.MarketPeak)
                            .FirstOrDefault());
                    if (marketRate.HasValue && marketRate > 0)
                        rate = marketRate.Value;
                }

                // ۲) وگرنه، اگر برای «ماه قبل» از ماه/سال همین سفارش یک تحلیل قبض ثبت شده باشد،
                //    نرخ از همان تحلیل مشتق می‌شود.
                if (rate <= 0 && order.BillYear.HasValue && order.BillMonth.HasValue)
                {
                    int prevMonth = order.BillMonth.Value - 1;
                    int prevYear  = order.BillYear.Value;
                    if (prevMonth < 1) { prevMonth = 12; prevYear -= 1; }

                    var prevReport = Repository<BillAnalysisReport>.Query(db =>
                        db.BillAnalysisReports
                            .Where(r => r.SubscriptionId == order.SubscriptionId && r.Year == prevYear && r.Month == prevMonth)
                            .Select(r => new { r.PeakCons, r.MidCons, r.LowCons, r.CostWithMatin })
                            .FirstOrDefault());

                    if (prevReport != null && prevReport.CostWithMatin.HasValue)
                    {
                        var totalKwh = (prevReport.PeakCons ?? 0) + (prevReport.MidCons ?? 0) + (prevReport.LowCons ?? 0);
                        if (totalKwh > 0)
                            rate = Math.Round(prevReport.CostWithMatin.Value / totalKwh, 4);
                    }
                }

                // ۳) وگرنه، نرخ قرارداد فعال همان اشتراک.
                if (rate <= 0)
                {
                    var contractRate = Repository<Contract>.Query(db =>
                        db.Contracts
                            .Where(c => c.SubscriptionId == order.SubscriptionId && c.StatusId == 2)
                            .OrderByDescending(c => c.Id)
                            .Select(c => (decimal?)c.ContractRate)
                            .FirstOrDefault());
                    rate = contractRate ?? 0;
                }

                // اگر «نوع انرژی» سفارش خودِ «برق سبز» باشد (نه تیک برق سبز)، این یک محصول جداگانه
                // است و با نرخ تابلوی سبز بورس (نه نرخ قرارداد/عادی) محاسبه می‌شود — همان ماه/سال
                // سفارش، وگرنه آخرین ماه موجود، و در نهایت (اگر ثبت نشده بود) همان نرخ عادی
                bool isGreenType = order.EnergyType != null && order.EnergyType.Contains("سبز");
                if (isGreenType)
                {
                    var boardRate = order.BillYear.HasValue && order.BillMonth.HasValue
                        ? Repository<MonthlyMarketRate>.Query(db =>
                            db.MonthlyMarketRates
                                .Where(m => m.Year == order.BillYear && m.Month == order.BillMonth)
                                .Select(m => m.GreenBoardRate)
                                .FirstOrDefault())
                        : null;

                    if (boardRate == null)
                        boardRate = Repository<MonthlyMarketRate>.Query(db =>
                            db.MonthlyMarketRates
                                .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month)
                                .Select(m => m.GreenBoardRate)
                                .FirstOrDefault());

                    if (boardRate.HasValue && boardRate > 0)
                        rate = boardRate.Value;
                }

                // نرخ بخش ۴٪ برق سبز (تیک «برق سبز می‌خواهم»): از تابلوی سبز بورس همان ماه/سال
                // سفارش، وگرنه آخرین ماه موجود، و در نهایت (اگر ثبت نشده بود) همان نرخ عادی سفارش.
                // ۹۶٪/۴٪ فقط مقدار انرژی را تقسیم می‌کند؛ نرخ هر بخش جداگانه محاسبه می‌شود.
                decimal? greenRate = null;
                if (order.IsGreenEnergy)
                {
                    greenRate = order.BillYear.HasValue && order.BillMonth.HasValue
                        ? Repository<MonthlyMarketRate>.Query(db =>
                            db.MonthlyMarketRates
                                .Where(m => m.Year == order.BillYear && m.Month == order.BillMonth)
                                .Select(m => m.GreenBoardRate)
                                .FirstOrDefault())
                        : null;

                    if (greenRate == null)
                        greenRate = Repository<MonthlyMarketRate>.Query(db =>
                            db.MonthlyMarketRates
                                .OrderByDescending(m => m.Year).ThenByDescending(m => m.Month)
                                .Select(m => m.GreenBoardRate)
                                .FirstOrDefault());

                    if (greenRate == null || greenRate <= 0)
                        greenRate = rate;
                }

                return (object)new
                {
                    order.Id,
                    order.BillIdentifier,
                    order.RequestedKwh,
                    order.EnergyType,
                    PriceAtMoment = rate,
                    order.OrderDate,
                    order.BuyerName,
                    order.NationalId,
                    order.EconomicCode,
                    order.RegisterNumber,
                    order.Phone,
                    order.Address,
                    order.PostalCode,
                    order.City,
                    order.Province,
                    order.IsGreenEnergy,
                    GreenRate = greenRate,
                };
            });
        }

        [HttpPost]
        public ExecutionResult SubmitPayment([FromBody] SubmitPaymentRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return new ExecutionResult(ResultType.Danger, "خطا", "احراز هویت نشده", 401);

            if (req.Amount <= 0)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "مبلغ پرداخت باید بزرگتر از صفر باشد.", 400);

            var order = Repository<ElectricityOrder>.GetLast(o => o.Id == req.OrderId && o.UserId == userId.Value);
            if (order == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "سفارش یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                Repository<Payment>.InsertItem(new Payment
                {
                    OrderId = req.OrderId,
                    Amount = req.Amount,
                    MethodId = req.MethodId,
                    StatusId = 1,
                    ReferenceNumber = req.ReferenceNumber,
                    ReceiptFileId = req.ReceiptFileId,
                    CreatedAt = DateTime.Now,
                });

                return (object)true;
            });
        }
    }
}
