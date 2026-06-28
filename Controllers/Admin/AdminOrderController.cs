using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web.WebPages;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AdminOrderController : BaseController
    {
        [HttpGet]
        public ExecutionResult GetList(int pageNumber = 1, int pageSize = 20, int? statusId = null)
        {
            return RunExceptionProof(() =>
            {
                using var db = DbContextProvider.CreateContext();

                var query = db.ElectricityOrders
                    .Where(o => statusId == null || o.StatusId == statusId.Value);

                var total = query.Count();

                var items = query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        o.Id,
                        o.BillId,
                        BillIdentifier = o.Bill.Subscription.BillIdentifier ?? "",
                        CustomerName = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal != null
                            ? o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                            : o.Bill.Subscription.Address.CustomerProfile.CustomersReal != null
                                ? o.Bill.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " + o.Bill.Subscription.Address.CustomerProfile.CustomersReal.LastName
                                : "",
                        o.RequestedKwh,
                        EnergyType = o.EnergyType.Title ?? "",
                        o.EnergyTypeId,
                        o.PriceAtMoment,
                        Status = o.Status.Title ?? "",
                        o.StatusId,
                        OrderDate = o.OrderDate != null ? PersianDateConverter.ToPersianDate(o.OrderDate.Value, "yyyy/MM/dd") : null,
                        o.IsPriceRequest,
                        PaymentCount = o.Payments.Count,
                        PaidAmount = o.Payments
                            .Where(p => p.StatusId == 2)
                            .Sum(p => (decimal?)p.Amount) ?? 0,
                    })
                    .ToList();

                return (object)new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = total,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                    Data = items,
                };
            });
        }

        [HttpGet("{id}")]
        public ExecutionResult GetDetail(int id)
        {
            return RunExceptionProof(() =>
            {
                using var db = DbContextProvider.CreateContext();

                var order = db.ElectricityOrders
                    .Where(o => o.Id == id)
                    .Select(o => new
                    {
                        o.Id,
                        o.BillId,
                        BillIdentifier = o.Bill.Subscription.BillIdentifier ?? "",
                        CustomerName = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal != null
                            ? o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                            : o.Bill.Subscription.Address.CustomerProfile.CustomersReal != null
                                ? o.Bill.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " + o.Bill.Subscription.Address.CustomerProfile.CustomersReal.LastName
                                : "",
                        o.RequestedKwh,
                        EnergyType = o.EnergyType.Title ?? "",
                        o.EnergyTypeId,
                        o.PriceAtMoment,
                        Status = o.Status.Title ?? "",
                        o.StatusId,
                        OrderDate = o.OrderDate != null ? PersianDateConverter.ToPersianDate(o.OrderDate.Value, "yyyy/MM/dd") : null,
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
                    .FirstOrDefault();

                return (object)order;
            });
        }

        [HttpPut]
        public ExecutionResult UpdateStatus([FromBody] UpdateOrderStatusRequest req)
        {
            var order = Repository<ElectricityOrder>.GetLast(o => o.Id == req.OrderId);
            if (order == null) return new ExecutionResult(ResultType.Danger, "خطا", "سفارش یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                order.StatusId = req.StatusId;
                if (req.PriceAtMoment.HasValue)
                    order.PriceAtMoment = req.PriceAtMoment.Value;
                Repository<ElectricityOrder>.UpdateItem(order);
                return (object)true;
            });
        }

        [HttpPut]
        public ExecutionResult ConfirmPayment([FromBody] ConfirmPaymentRequest req)
        {
            var payment = Repository<Payment>.GetLast(p => p.Id == req.PaymentId);
            if (payment == null) return new ExecutionResult(ResultType.Danger, "خطا", "پرداخت یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                payment.StatusId = req.StatusId;
                Repository<Payment>.UpdateItem(payment);
                return (object)true;
            });
        }

        /// <summary>ثبت فیش پرداخت برای هر سفارش توسط ادمین (بدون بررسی مالکیت)</summary>
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
                return (object)true;
            });
        }

        [HttpGet]
        public ExecutionResult GetProformaInvoice(int orderId)
        {
            return RunExceptionProof(() =>
            {
                using var db = DbContextProvider.CreateContext();
                var order = db.ElectricityOrders
                    .Where(o => o.Id == orderId)
                    .Select(o => new
                    {
                        o.Id,
                        o.RequestedKwh,
                        ContractRate = db.Contracts
                            .Where(c => c.SubscriptionId == o.Bill.SubscriptionId)
                            .OrderByDescending(c => c.Id)
                            .Select(c => (decimal?)c.ContractRate)
                            .FirstOrDefault(),
                        EnergyType = o.EnergyType.Title,
                        OrderDate = o.OrderDate != null ? o.OrderDate.Value.ToString("yyyy-MM-dd") : null,
                        BillIdentifier = o.Bill.Subscription.BillIdentifier,
                        CustomerName = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                                      ?? (o.Bill.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " + o.Bill.Subscription.Address.CustomerProfile.CustomersReal.LastName),
                        NationalId = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.NationalId
                                      ?? o.Bill.Subscription.Address.CustomerProfile.CustomersReal.NationalCode,
                        RegisterNumber = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.RegisterNumber,
                        CeoFullName = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CeoFullName,
                        CeoNationalId = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CeoNationalId,
                        Address = o.Bill.Subscription.Address.MainAddress,
                        PostalCode = o.Bill.Subscription.Address.PostalCode,
                    })
                    .FirstOrDefault();
                return (object?)order;
            });
        }

        /// <summary>ثبت سفارش برای مشتری توسط ادمین (بدون بررسی مالکیت اشتراک)</summary>
        [HttpPost]
        public ExecutionResult CreateOrderForCustomer([FromBody] CreateOrderRequest req)
        {
            var adminUserId = new UseContext(new HttpContextAccessor()).GetUserId();
            if (adminUserId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "احراز هویت نشده", 401);

            if (req.RequestedKwh <= 0)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "مقدار درخواستی باید بزرگتر از صفر باشد.", 400);

            using var db = DbContextProvider.CreateContext();

            var subscription = db.Subscriptions
                .Where(s => s.Id == req.SubscriptionId)
                .Select(s => new { s.Id, s.AddressId })
                .FirstOrDefault();
            if (subscription == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "اشتراک یافت نشد.", 404);

            var address = db.Addresses
                .Where(a => a.Id == subscription.AddressId)
                .Select(a => new { a.CustomerProfileId, a.PowerEntityId })
                .FirstOrDefault();
            if (address == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "آدرس اشتراک یافت نشد.", 404);

            var profile = db.CustomerProfiles
                .Where(p => p.Id == address.CustomerProfileId)
                .Select(p => new { p.CustomerTypeId })
                .FirstOrDefault();

            var tariff = db.Tariffs
                .Where(t => t.CustomerTypeId == profile!.CustomerTypeId && t.PowerEntitiesId == address.PowerEntityId)
                .Select(t => new { t.TariffId })
                .FirstOrDefault();
            if (tariff == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "تعرفه‌ای برای این اشتراک یافت نشد.", 404);

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
                    UserId = adminUserId.Value,
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
    }
}
