using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AdminOrderController : BaseController
    {
        [HttpDelete("{id}")]
        public ExecutionResult Delete(int id)
        {
            if (!new UseContext(new HttpContextAccessor()).IsAdminRole())
                return new ExecutionResult(ResultType.Danger, "عدم دسترسی", "حذف سفارش فقط برای نقش ادمین مجاز است.", 403);

            var order = Repository<ElectricityOrder>.GetLast(o => o.Id == id);
            if (order == null)
                return new ExecutionResult(ResultType.Danger, "یافت نشد", "سفارش مورد نظر یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                // ابتدا پرداخت‌های وابسته حذف می‌شوند تا خطای «اطلاعات وابسته» رخ ندهد
                var payments = Repository<Payment>.GetListExtended(p => p.OrderId == id);
                foreach (var payment in payments)
                    Repository<Payment>.DeleteItem(payment);

                Repository<ElectricityOrder>.DeleteItem(order);
            });
        }

        [HttpGet]
        public ExecutionResult GetList(int pageNumber = 1, int pageSize = 20, int? statusId = null)
        {
            try
            {
                var filter = new PaginationFilter { PageNumber = pageNumber, PageSize = pageSize };
                Expression<Func<ElectricityOrder, bool>> predicate =
                    o => statusId == null || o.StatusId == statusId.Value;

                var result = Repository<ElectricityOrder>.GetSelectiveListWithPaging(
                    i => new
                    {
                        i.Id,
                        i.BillId,
                        BillIdentifier = i.Bill.Subscription.BillIdentifier ?? "",
                        CustomerName = i.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                            ?? (i.Bill.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " +
                                i.Bill.Subscription.Address.CustomerProfile.CustomersReal.LastName),
                        i.RequestedKwh,
                        EnergyType = i.EnergyType.Title ?? "",
                        i.EnergyTypeId,
                        i.PriceAtMoment,
                        Status = i.Status.Title ?? "",
                        i.StatusId,
                        OrderDate = i.OrderDate != null
                            ? PersianDateConverter.ToPersianDate(i.OrderDate.Value, "yyyy/MM/dd")
                            : null,
                        i.IsPriceRequest,
                        PaymentCount = i.Payments.Count(),
                        PaidAmount = i.Payments
                            .Where(p => p.StatusId == 2)
                            .Sum(p => (decimal?)p.Amount) ?? 0m,
                    },
                    filter,
                    predicate,
                    sortExpression: "OrderDate",
                    sortDirection: System.Web.Helpers.SortDirection.Descending,
                    includes: new[]
                    {
                        "Bill.Subscription.Address.CustomerProfile.CustomersLegal",
                        "Bill.Subscription.Address.CustomerProfile.CustomersReal",
                        "EnergyType",
                        "Status",
                        "Payments",
                    });

                var pagination = new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
                return new ExecutionResult(ResultType.Success, "موفق", "", 200, pagination);
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        [HttpGet("{id}")]
        public ExecutionResult GetDetail(int id)
        {
            try
            {
                var order = Repository<ElectricityOrder>.Query(db =>
                    db.ElectricityOrders
                        .Where(o => o.Id == id)
                        .Select(o => new
                        {
                            o.Id,
                            o.BillId,
                            BillIdentifier = o.Bill.Subscription.BillIdentifier ?? "",
                            CustomerName = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                                ?? (o.Bill.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " +
                                    o.Bill.Subscription.Address.CustomerProfile.CustomersReal.LastName),
                            o.RequestedKwh,
                            EnergyType = o.EnergyType.Title ?? "",
                            o.EnergyTypeId,
                            o.PriceAtMoment,
                            Status = o.Status.Title ?? "",
                            o.StatusId,
                            OrderDate = o.OrderDate != null
                                ? PersianDateConverter.ToPersianDate(o.OrderDate.Value, "yyyy/MM/dd")
                                : null,
                            o.IsPriceRequest,
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
                                CreatedAt = p.CreatedAt != null
                                    ? PersianDateConverter.ToPersianDate(p.CreatedAt.Value, "yyyy/MM/dd")
                                    : null,
                            }).ToList(),
                        })
                        .FirstOrDefault());

                if (order == null)
                    return new ExecutionResult(ResultType.Danger, "یافت نشد", "سفارش مورد نظر یافت نشد.", 404);
                return new ExecutionResult(ResultType.Success, "موفق", "", 200, order);
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        [HttpPut]
        public ExecutionResult UpdateStatus([FromBody] UpdateOrderStatusRequest req)
        {
            var order = Repository<ElectricityOrder>.GetLast(o => o.Id == req.OrderId);
            if (order == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "سفارش یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                order.StatusId = req.StatusId;
                if (req.PriceAtMoment.HasValue)
                    order.PriceAtMoment = req.PriceAtMoment.Value;
                Repository<ElectricityOrder>.UpdateItem(order);
            });
        }

        [HttpPut]
        public ExecutionResult ConfirmPayment([FromBody] ConfirmPaymentRequest req)
        {
            var payment = Repository<Payment>.GetLast(p => p.Id == req.PaymentId);
            if (payment == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "پرداخت یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                payment.StatusId = req.StatusId;
                Repository<Payment>.UpdateItem(payment);
            });
        }

        [HttpPost]
        public ExecutionResult SubmitPaymentForOrder([FromBody] SubmitPaymentRequest req)
        {
            if (req.Amount <= 0)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "مبلغ پرداخت باید بزرگتر از صفر باشد.", 400);

            var order = Repository<ElectricityOrder>.GetLast(o => o.Id == req.OrderId);
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
            });
        }

        [HttpGet]
        public ExecutionResult GetProformaInvoice(int orderId)
        {
            try
            {
                var order = Repository<ElectricityOrder>.Query(db =>
                    db.ElectricityOrders
                        .Where(o => o.Id == orderId)
                        .Select(o => new
                        {
                            o.Id,
                            o.RequestedKwh,
                            o.PriceAtMoment,
                            o.BillYear,
                            o.BillMonth,
                            o.IsGreenEnergy,
                            SubscriptionId = o.Bill.SubscriptionId,
                            ContractRate = db.Contracts
                                .Where(c => c.SubscriptionId == o.Bill.SubscriptionId)
                                .OrderByDescending(c => c.Id)
                                .Select(c => (decimal?)c.ContractRate)
                                .FirstOrDefault(),
                            EnergyType = o.EnergyType.Title,
                            OrderDate = o.OrderDate != null
                                ? PersianDateConverter.ToPersianDate(o.OrderDate.Value, "yyyy-MM-dd")
                                : null,
                            BillIdentifier = o.Bill.Subscription.BillIdentifier,
                            CustomerName = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                                ?? ((o.Bill.Subscription.Address.CustomerProfile.CustomersReal.FirstName ?? "") + " " +
                                    (o.Bill.Subscription.Address.CustomerProfile.CustomersReal.LastName ?? "")),
                            NationalId = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.NationalId
                                ?? o.Bill.Subscription.Address.CustomerProfile.CustomersReal.NationalCode,
                            EconomicCode   = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.EconomicCode,
                            RegisterNumber = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.RegisterNumber,
                            CeoFullName    = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CeoFullName,
                            CeoNationalId  = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CeoNationalId,
                            Address        = o.Bill.Subscription.Address.MainAddress,
                            City           = o.Bill.Subscription.Address.City.Title,
                            Province       = o.Bill.Subscription.Address.City.Province.Name,
                            PostalCode     = o.Bill.Subscription.Address.PostalCode,
                        })
                        .FirstOrDefault());

                if (order == null)
                    return new ExecutionResult(ResultType.Danger, "یافت نشد", "سفارش مورد نظر یافت نشد.", 404);

                // اگر «نوع انرژی» سفارش خودِ «برق سبز» باشد (نه تیک برق سبز)، این یک محصول جداگانه
                // است و با نرخ تابلوی سبز بورس (نه نرخ قرارداد) محاسبه می‌شود — همان ماه/سال سفارش،
                // وگرنه آخرین ماه موجود، و در نهایت (اگر ثبت نشده بود) همان نرخ قرارداد
                // ۱) نرخ بازار (MarketPeak) برای ماه/سال همین سفارش — یعنی ادمین برای آن ماه نرخ
                //    ثبت کرده (جدول MonthlyMarketRates)، ۲) وگرنه نرخ مشتق از تحلیل قبض ماه قبل،
                //    ۳) وگرنه نرخ قرارداد فعال.
                decimal? rate = null;
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

                if ((rate == null || rate <= 0) && order.BillYear.HasValue && order.BillMonth.HasValue)
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

                if (rate == null || rate <= 0)
                    rate = order.ContractRate;

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
                // سفارش، وگرنه آخرین ماه موجود، و در نهایت (اگر ثبت نشده بود) همان نرخ سفارش.
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

                var result = new
                {
                    order.Id,
                    order.RequestedKwh,
                    ContractRate = rate,
                    order.EnergyType,
                    order.OrderDate,
                    order.BillIdentifier,
                    order.CustomerName,
                    order.NationalId,
                    order.EconomicCode,
                    order.RegisterNumber,
                    order.CeoFullName,
                    order.CeoNationalId,
                    order.Address,
                    order.City,
                    order.Province,
                    order.PostalCode,
                    order.IsGreenEnergy,
                    GreenRate = greenRate,
                };
                return new ExecutionResult(ResultType.Success, "موفق", "", 200, result);
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        [HttpPost]
        public ExecutionResult CreateOrderForCustomer([FromBody] CreateOrderRequest req)
        {
            var adminUserId = new UseContext(new HttpContextAccessor()).GetUserId();
            if (adminUserId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "احراز هویت نشده", 401);

            if (req.RequestedKwh <= 0)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "مقدار درخواستی باید بزرگتر از صفر باشد.", 400);

            var subscription = Repository<Subscription>.Query(db =>
                db.Subscriptions
                    .Where(s => s.Id == req.SubscriptionId)
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
            // نبودِ آن نباید جلوی ثبت سفارش را بگیرد
            var tariff = Repository<Tariff>.Query(db =>
                db.Tariffs
                    .Where(t => t.CustomerTypeId == profile!.CustomerTypeId && t.PowerEntitiesId == address.PowerEntityId)
                    .Select(t => new { t.TariffId })
                    .FirstOrDefault());

            try
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
                    UserId = adminUserId.Value,
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

                return new ExecutionResult(ResultType.Success, "موفق", "", 200, order.Id.ToString());
            }
            catch (Exception ex) { return HandleException(ex); }
        }
    }
}
