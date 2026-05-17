using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MatinPower.Server.Controllers.Admin
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AdminReportController : BaseController
    {
        [HttpGet]
        public ExecutionResult ContractReport(
            string? search = null,
            int? statusId = null,
            string? fromDate = null,
            string? toDate = null)
        {
            return RunExceptionProof(() =>
            {
                using var db = DbContextProvider.CreateContext();

                var query = db.Contracts.AsQueryable();

                if (statusId.HasValue)
                    query = query.Where(c => c.StatusId == statusId.Value);

                if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var fd))
                    query = query.Where(c => c.StartDate >= fd);

                if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var td))
                    query = query.Where(c => c.StartDate <= td);

                var items = query
                    .Select(c => new
                    {
                        c.Id,
                        c.ContractNumber,
                        CustomerName = c.Subscription.Address.CustomerProfile.CustomersLegal != null
                            ? c.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                            : c.Subscription.Address.CustomerProfile.CustomersReal != null
                                ? c.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " + c.Subscription.Address.CustomerProfile.CustomersReal.LastName
                                : "",
                        BillIdentifier = c.Subscription.BillIdentifier ?? "",
                        Status = c.Status.Title ?? "",
                        c.StatusId,
                        StartDate = c.StartDate != null ? c.StartDate.Value.ToString("yyyy-MM-dd") : null,
                        EndDate = c.EndDate != null ? c.EndDate.Value.ToString("yyyy-MM-dd") : null,
                        c.ContractRate,
                        c.ContractPowerKw,
                        c.ContractVolumeKwh,
                        c.ContractAmountRial,
                        PaymentDeadline = c.PaymentDeadline != null ? c.PaymentDeadline.Value.ToString("yyyy-MM-dd") : null,
                    })
                    .ToList();

                if (!string.IsNullOrEmpty(search))
                {
                    var s = search.ToLower();
                    items = items.Where(i =>
                        (i.ContractNumber ?? "").ToLower().Contains(s) ||
                        (i.CustomerName ?? "").ToLower().Contains(s) ||
                        i.BillIdentifier.ToLower().Contains(s)
                    ).ToList();
                }

                var summary = new
                {
                    Total = items.Count,
                    ByStatus = items
                        .GroupBy(i => new { i.StatusId, i.Status })
                        .Select(g => new { g.Key.StatusId, g.Key.Status, Count = g.Count() })
                        .ToList(),
                    TotalAmountRial = items.Sum(i => i.ContractAmountRial ?? 0),
                };

                return (object)new { Items = items, Summary = summary };
            });
        }

        [HttpGet]
        public ExecutionResult OrderReport(
            int? statusId = null,
            int? energyTypeId = null,
            bool? isPriceRequest = null,
            string? fromDate = null,
            string? toDate = null)
        {
            return RunExceptionProof(() =>
            {
                using var db = DbContextProvider.CreateContext();

                var query = db.ElectricityOrders.AsQueryable();

                if (statusId.HasValue)
                    query = query.Where(o => o.StatusId == statusId.Value);

                if (energyTypeId.HasValue)
                    query = query.Where(o => o.EnergyTypeId == energyTypeId.Value);

                if (isPriceRequest.HasValue)
                    query = query.Where(o => o.IsPriceRequest == isPriceRequest.Value);

                if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var fd))
                    query = query.Where(o => o.OrderDate >= fd);

                if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var td))
                    query = query.Where(o => o.OrderDate <= td);

                var items = query
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new
                    {
                        o.Id,
                        BillIdentifier = o.Bill.Subscription.BillIdentifier ?? "",
                        CustomerName = o.Bill.Subscription.Address.CustomerProfile.CustomersLegal != null
                            ? o.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                            : o.Bill.Subscription.Address.CustomerProfile.CustomersReal != null
                                ? o.Bill.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " + o.Bill.Subscription.Address.CustomerProfile.CustomersReal.LastName
                                : "",
                        EnergyType = o.EnergyType.Title ?? "",
                        o.EnergyTypeId,
                        o.RequestedKwh,
                        o.PriceAtMoment,
                        Status = o.Status.Title ?? "",
                        o.StatusId,
                        OrderDate = o.OrderDate != null ? o.OrderDate.Value.ToString("yyyy-MM-dd") : null,
                        o.IsPriceRequest,
                        PaymentCount = o.Payments.Count,
                        PaidAmount = o.Payments.Where(p => p.StatusId == 2).Sum(p => (decimal?)p.Amount) ?? 0,
                    })
                    .ToList();

                var summary = new
                {
                    Total = items.Count,
                    ByStatus = items
                        .GroupBy(i => new { i.StatusId, i.Status })
                        .Select(g => new { g.Key.StatusId, g.Key.Status, Count = g.Count() })
                        .ToList(),
                    TotalRequestedKwh = items.Sum(i => i.RequestedKwh),
                    TotalPaidRial = items.Sum(i => i.PaidAmount),
                };

                return (object)new { Items = items, Summary = summary };
            });
        }

        [HttpGet]
        public ExecutionResult PaymentReport(
            int? statusId = null,
            int? methodId = null,
            string? fromDate = null,
            string? toDate = null)
        {
            return RunExceptionProof(() =>
            {
                using var db = DbContextProvider.CreateContext();

                var query = db.Payments.AsQueryable();

                if (statusId.HasValue)
                    query = query.Where(p => p.StatusId == statusId.Value);

                if (methodId.HasValue)
                    query = query.Where(p => p.MethodId == methodId.Value);

                if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var fd))
                    query = query.Where(p => p.CreatedAt >= fd);

                if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var td))
                    query = query.Where(p => p.CreatedAt <= td);

                var items = query
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new
                    {
                        p.Id,
                        p.OrderId,
                        BillIdentifier = p.Order.Bill.Subscription.BillIdentifier ?? "",
                        CustomerName = p.Order.Bill.Subscription.Address.CustomerProfile.CustomersLegal != null
                            ? p.Order.Bill.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                            : p.Order.Bill.Subscription.Address.CustomerProfile.CustomersReal != null
                                ? p.Order.Bill.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " + p.Order.Bill.Subscription.Address.CustomerProfile.CustomersReal.LastName
                                : "",
                        p.Amount,
                        Method = p.Method.Title ?? "",
                        p.MethodId,
                        Status = p.Status.Title ?? "",
                        p.StatusId,
                        p.ReferenceNumber,
                        ReceiptFileId = p.ReceiptFileId.HasValue ? p.ReceiptFileId.Value.ToString() : null,
                        CreatedAt = p.CreatedAt != null ? p.CreatedAt.Value.ToString("yyyy-MM-dd") : null,
                    })
                    .ToList();

                var summary = new
                {
                    Total = items.Count,
                    ByStatus = items
                        .GroupBy(i => new { i.StatusId, i.Status })
                        .Select(g => new { g.Key.StatusId, g.Key.Status, Count = g.Count(), TotalAmount = g.Sum(x => x.Amount) })
                        .ToList(),
                    TotalAmountRial = items.Sum(i => i.Amount),
                    ConfirmedAmountRial = items.Where(i => i.StatusId == 2).Sum(i => i.Amount),
                };

                return (object)new { Items = items, Summary = summary };
            });
        }
    }
}
