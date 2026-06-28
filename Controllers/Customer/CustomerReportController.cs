using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Customer
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class CustomerReportController : BaseController
    {
        [HttpGet]
        public ExecutionResult GetMyProfitReport() =>
            RunExceptionProof(() =>
            {
                var userId = new UseContext(new HttpContextAccessor()).GetUserId();
                if (userId == null) return (object)null!;

                return Repository<BillAnalysisReport>.Query(db =>
                {
                    var profileId = db.Users
                        .Where(u => u.Id == userId.Value)
                        .Select(u => u.CustomerProfileId)
                        .FirstOrDefault();

                    if (profileId == null) return (object)null!;

                    var rows = db.BillAnalysisReports
                        .Where(r => r.Subscription.Address.CustomerProfileId == profileId)
                        .OrderBy(r => r.Year).ThenBy(r => r.Month)
                        .Select(r => new
                        {
                            r.Year,
                            r.Month,
                            r.SubscriptionId,
                            BillIdentifier    = r.Subscription.BillIdentifier,
                            r.PeakCons,
                            r.MidCons,
                            r.LowCons,
                            r.CostWithoutMatin,
                            r.CostWithMatin,
                            r.NetSaving,
                        })
                        .ToList();

                    decimal cumulative = 0;
                    var withCumulative = rows.Select(r =>
                    {
                        cumulative += r.NetSaving ?? 0m;
                        return new
                        {
                            r.Year, r.Month, r.SubscriptionId, r.BillIdentifier,
                            r.PeakCons, r.MidCons, r.LowCons,
                            r.CostWithoutMatin, r.CostWithMatin, r.NetSaving,
                            CumulativeSaving = cumulative,
                        };
                    }).ToList();

                    var totalNet       = rows.Sum(r => r.NetSaving)        ?? 0m;
                    var totalWithMatin = rows.Sum(r => r.CostWithMatin)    ?? 0m;
                    var totalWithout   = rows.Sum(r => r.CostWithoutMatin) ?? 0m;

                    return (object)new
                    {
                        Summary = new
                        {
                            TotalNetSaving        = totalNet,
                            TotalCostWithMatin    = totalWithMatin,
                            TotalCostWithoutMatin = totalWithout,
                            MonthCount            = rows.Count,
                            SavingPercent         = totalWithout > 0 ? Math.Round(totalNet / totalWithout * 100, 1) : 0m,
                        },
                        Rows = withCumulative,
                    };
                });
            });
    }
}
