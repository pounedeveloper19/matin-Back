using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                        OrderDate = o.OrderDate != null ? o.OrderDate.Value.ToString("yyyy-MM-dd") : null,
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
    }
}
