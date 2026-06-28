using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class BillAdminController : BaseManageController<BillAnalysisReport>
    {
        [HttpGet]
        [Route("[controller]/GetAvailableYears")]
        public ExecutionResult GetAvailableYears()
        {
            try
            {
                var calendar    = new System.Globalization.PersianCalendar();
                int currentYear = calendar.GetYear(DateTime.Now);

                var filter = new PaginationFilter { PageNumber = 1, PageSize = 5000 };
                var result = Repository<BillAnalysisReport>.GetSelectiveListWithPaging(
                    r => new { r.Year },
                    filter,
                    r => true,
                    sortExpression: "Year",
                    sortDirection: System.Web.Helpers.SortDirection.Descending,
                    includes: null);

                var json   = System.Text.Json.JsonSerializer.Serialize(result.Item1);
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(json)!;

                var dbYears = parsed
                    .Select(e => e.TryGetProperty("Year", out var y)  ? y.GetInt32()
                               : e.TryGetProperty("year", out var yl) ? yl.GetInt32() : 0)
                    .Where(y => y > 0)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .Take(3)
                    .ToList();

                var years = dbYears
                    .Append(currentYear)
                    .Distinct()
                    .OrderBy(y => y)
                    .ToList();

                return new ExecutionResult(ResultType.Success, "موفق", "", 200, years);
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        [HttpGet]
        [Route("[controller]/GetCustomerNames")]
        public ExecutionResult GetCustomerNames()
        {
            try
            {
                var filter = new PaginationFilter { PageNumber = 1, PageSize = 5000 };

                // Query CustomersLegal directly — avoids complex nullable navigation projection
                var legalResult = Repository<CustomersLegal>.GetSelectiveListWithPaging(
                    r => new { r.Id, r.CompanyName },
                    filter,
                    r => r.CompanyName != null && r.CompanyName != "",
                    sortExpression: "CompanyName",
                    sortDirection: System.Web.Helpers.SortDirection.Ascending,
                    includes: null);

                var json     = System.Text.Json.JsonSerializer.Serialize(legalResult.Item1);
                var parsed   = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(json)!;
                var customers = parsed
                    .Select(e => new
                    {
                        ProfileId    = e.TryGetProperty("id",          out var p) ? p.GetInt32()    : 0,
                        CustomerName = e.TryGetProperty("companyName",  out var n) ? n.GetString() ?? "" : "",
                    })
                    .Where(x => x.ProfileId > 0 && !string.IsNullOrEmpty(x.CustomerName))
                    .ToList();

                return new ExecutionResult(ResultType.Success, "موفق", "", 200, customers);
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        [HttpGet]
        [Route("[controller]/GetAllProfitSummary")]
        public ExecutionResult GetAllProfitSummary(
            int pageNumber = 1, int pageSize = 20,
            int? fromYear = null, int? fromMonth = null,
            int? toYear = null, int? toMonth = null,
            string? customerName = null)
        {
            try
            {
                var filter = new PaginationFilter { PageNumber = pageNumber, PageSize = pageSize };

                Expression<Func<BillAnalysisReport, bool>> predicate = r => true;
                if (!string.IsNullOrWhiteSpace(customerName))
                    predicate = predicate.AppendCondition(
                        r => r.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName.Contains(customerName) ||
                             (r.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " +
                              r.Subscription.Address.CustomerProfile.CustomersReal.LastName).Contains(customerName),
                        false);
                if (fromYear.HasValue && fromMonth.HasValue)
                {
                    int fromVal = fromYear.Value * 12 + fromMonth.Value;
                    predicate = predicate.AppendCondition(r => r.Year * 12 + r.Month >= fromVal, false);
                }
                if (toYear.HasValue && toMonth.HasValue)
                {
                    int toVal = toYear.Value * 12 + toMonth.Value;
                    predicate = predicate.AppendCondition(r => r.Year * 12 + r.Month <= toVal, false);
                }

                var result = Repository<BillAnalysisReport>.GetSelectiveListWithPaging(
                    r => new
                    {
                        r.Id,
                        ProfileId      = r.Subscription.Address.CustomerProfileId,
                        CustomerName   = r.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                            ?? (r.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " +
                                r.Subscription.Address.CustomerProfile.CustomersReal.LastName),
                        r.Year,
                        r.Month,
                        BillIdentifier = r.Subscription.BillIdentifier ?? "",
                        r.PeakCons,
                        r.MidCons,
                        r.LowCons,
                        r.CostWithMatin,
                        r.CostWithoutMatin,
                        r.NetSaving,
                    },
                    filter,
                    predicate,
                    sortExpression: "Year",
                    sortDirection: System.Web.Helpers.SortDirection.Descending,
                    includes: new[]
                    {
                        "Subscription.Address.CustomerProfile.CustomersLegal",
                        "Subscription.Address.CustomerProfile.CustomersReal",
                    });

                var pagination = new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
                return new ExecutionResult(ResultType.Success, "موفق", "", 200, pagination);
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        [HttpGet]
        [Route("[controller]/GetProfitReport/{profileId}")]
        public ExecutionResult GetProfitReport(int profileId)
        {
            try
            {
                var filter = new PaginationFilter { PageNumber = 1, PageSize = 500 };
                Expression<Func<BillAnalysisReport, bool>> predicate =
                    r => r.Subscription.Address.CustomerProfileId == profileId;

                var result = Repository<BillAnalysisReport>.GetSelectiveListWithPaging(
                    r => new
                    {
                        r.Year, r.Month, r.SubscriptionId,
                        BillIdentifier    = r.Subscription.BillIdentifier ?? "",
                        r.PeakCons, r.MidCons, r.LowCons,
                        r.CostWithoutMatin, r.CostWithMatin, r.NetSaving,
                    },
                    filter, predicate,
                    sortExpression: "Year",
                    sortDirection: System.Web.Helpers.SortDirection.Ascending,
                    includes: new[] { "Subscription" });

                var json    = System.Text.Json.JsonSerializer.Serialize(result.Item1);
                var parsed  = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(json)!;
                var ordered = parsed
                    .OrderBy(e  => e.GetProperty("year").GetInt32())
                    .ThenBy(e   => e.GetProperty("month").GetInt32())
                    .ToList();

                decimal cumulative = 0m;
                var rows = ordered.Select(e =>
                {
                    decimal saving = e.TryGetProperty("netSaving", out var ns) && ns.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? ns.GetDecimal() : 0m;
                    cumulative += saving;
                    return new
                    {
                        Year             = e.GetProperty("year").GetInt32(),
                        Month            = e.GetProperty("month").GetInt32(),
                        SubscriptionId   = e.GetProperty("subscriptionId").GetInt32(),
                        BillIdentifier   = e.GetProperty("billIdentifier").GetString() ?? "",
                        PeakCons         = e.TryGetProperty("peakCons",         out var pc)   && pc.ValueKind   != System.Text.Json.JsonValueKind.Null ? pc.GetDecimal()   : 0m,
                        MidCons          = e.TryGetProperty("midCons",          out var mc)   && mc.ValueKind   != System.Text.Json.JsonValueKind.Null ? mc.GetDecimal()   : 0m,
                        LowCons          = e.TryGetProperty("lowCons",          out var lc)   && lc.ValueKind   != System.Text.Json.JsonValueKind.Null ? lc.GetDecimal()   : 0m,
                        CostWithoutMatin = e.TryGetProperty("costWithoutMatin", out var cwom) && cwom.ValueKind != System.Text.Json.JsonValueKind.Null ? cwom.GetDecimal() : 0m,
                        CostWithMatin    = e.TryGetProperty("costWithMatin",    out var cwm)  && cwm.ValueKind  != System.Text.Json.JsonValueKind.Null ? cwm.GetDecimal()  : 0m,
                        NetSaving        = saving,
                        CumulativeSaving = cumulative,
                    };
                }).ToList();

                var totalNet    = rows.Sum(r => r.NetSaving);
                var totalMatin  = rows.Sum(r => r.CostWithMatin);
                var totalWithout = rows.Sum(r => r.CostWithoutMatin);

                return new ExecutionResult(ResultType.Success, "موفق", "", 200, new
                {
                    Summary = new
                    {
                        TotalNetSaving        = totalNet,
                        TotalCostWithMatin    = totalMatin,
                        TotalCostWithoutMatin = totalWithout,
                        MonthCount            = rows.Count,
                        SavingPercent         = totalWithout > 0 ? Math.Round(totalNet / totalWithout * 100, 1) : 0m,
                    },
                    Rows = rows,
                });
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        [HttpGet]
        [Route("[controller]/GetByProfile/{profileId}")]
        public ExecutionResult GetByProfile(int profileId)
        {
            try
            {
                var filter = new PaginationFilter { PageNumber = 1, PageSize = 500 };
                Expression<Func<BillAnalysisReport, bool>> predicate =
                    r => r.Subscription.Address.CustomerProfileId == profileId;

                var result = Repository<BillAnalysisReport>.GetSelectiveListWithPaging(
                    r => new
                    {
                        r.Id,
                        r.SubscriptionId,
                        BillIdentifier = r.Subscription.BillIdentifier ?? "",
                        r.Year, r.Month,
                        r.PeakCons, r.MidCons, r.LowCons,
                        r.CostWithoutMatin, r.CostWithMatin, r.NetSaving,
                        CreatedAt = PersianDateConverter.ToPersianDate(r.CreatedAt, "yyyy/MM/dd"),
                    },
                    filter, predicate,
                    sortExpression: "Year",
                    sortDirection: System.Web.Helpers.SortDirection.Descending,
                    includes: new[] { "Subscription" });

                return new ExecutionResult(ResultType.Success, "موفق", "", 200, result.Item1);
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        protected override PaginationResult GridDataSource(Expression<Func<BillAnalysisReport, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<BillAnalysisReport>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.SubscriptionId,
                BillIdentifier   = i.Subscription.BillIdentifier,
                i.Year,
                i.Month,
                i.PeakCons,
                i.MidCons,
                i.LowCons,
                i.CostWithoutMatin,
                i.CostWithMatin,
                i.NetSaving,
                CreatedAt = PersianDateConverter.ToPersianDate(i.CreatedAt, "yyyy/MM/dd"),
            }, filter, predicate, sortExpression: "CreatedAt", sortDirection: System.Web.Helpers.SortDirection.Descending, includes: new[] { "Subscription" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<BillAnalysisReport, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<BillAnalysisReport, bool>> result = i => true;
                int? subscriptionId = UrlArgument<int?>("Search_SubscriptionId");
                if (subscriptionId.HasValue && subscriptionId > 0)
                    result = result.AppendCondition(s => s.SubscriptionId == subscriptionId.Value, false);
                int? year = UrlArgument<int?>("Search_Year");
                if (year.HasValue && year > 0)
                    result = result.AppendCondition(s => s.Year == year.Value, false);
                int? month = UrlArgument<int?>("Search_Month");
                if (month.HasValue && month > 0)
                    result = result.AppendCondition(s => s.Month == month.Value, false);
                return result;
            }
        }
    }
}
