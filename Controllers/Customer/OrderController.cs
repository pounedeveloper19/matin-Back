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

            var tariff = Repository<Tariff>.Query(db =>
                db.Tariffs
                    .Where(t => t.CustomerTypeId == profile!.CustomerTypeId && t.PowerEntitiesId == address.PowerEntityId)
                    .Select(t => new { t.TariffId })
                    .FirstOrDefault());
            if (tariff == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "تعرفه‌ای برای این اشتراک یافت نشد. با پشتیبانی تماس بگیرید.", 404);

            return RunExceptionProof(() =>
            {
                var bill = Repository<Bill>.InsertItem(new Bill
                {
                    SubscriptionId = req.SubscriptionId,
                    TariffId = tariff.TariffId,
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
                });

                return (object)order.Id;
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
