using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Log;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Route("[controller]")]
    public class AdminDashboardController : BaseController
    {
        private readonly LogDbContext _logDb;

        public AdminDashboardController(LogDbContext logDb)
        {
            _logDb = logDb;
        }

        /// <summary>کارت‌های خلاصه</summary>
        [HttpGet("Summary")]
        public ExecutionResult Summary() =>
            RunExceptionProof(() => (object)new
            {
                TotalContracts       = Repository<Contract>.GetList().Item2,
                ActiveContracts      = Repository<Contract>.GetList(null, c => c.StatusId == 2).Item2,
                TotalCustomers       = Repository<CustomerProfile>.GetList().Item2,
                PendingOrders        = Repository<ElectricityOrder>.GetList(null, o => o.StatusId == 1).Item2,
                PendingRegistrations = Repository<User>.GetList(null, u => u.IsActive == false && u.CustomerProfileId != null).Item2,
                BillReports          = Repository<BillAnalysisReport>.GetList().Item2,
            });

        /// <summary>تعداد قراردادها به تفکیک وضعیت — دونات</summary>
        [HttpGet("ContractsByStatus")]
        public ExecutionResult ContractsByStatus() =>
            RunExceptionProof(() =>
            {
                var all = Repository<Contract>.GetSelectiveList(
                    c => new { c.StatusId, Title = c.Status != null ? c.Status.Title : "" },
                    includes: new[] { "Status" }
                );
                return (object)all
                    .GroupBy(c => new { c.StatusId, c.Title })
                    .Select(g => new { StatusId = g.Key.StatusId, Title = g.Key.Title, Count = g.Count() })
                    .OrderBy(x => x.StatusId)
                    .ToList();
            });

        /// <summary>رشد مشتریان (حقیقی + حقوقی) در ۱۲ ماه اخیر — نمودار خطی</summary>
        [HttpGet("CustomerGrowth")]
        public ExecutionResult CustomerGrowth() =>
            RunExceptionProof(() =>
            {
                var cutoff = DateTime.Now.AddMonths(-11);

                var legalGrouped = Repository<CustomersLegal>.GetSelectiveList(
                    c => new { Year = c.CreatedAt!.Value.Year, Month = c.CreatedAt!.Value.Month },
                    c => c.CreatedAt >= cutoff
                )
                .GroupBy(c => new { c.Year, c.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToList();

                var realGrouped = Repository<CustomersReal>.GetSelectiveList(
                    c => new { Year = c.CreatedAt!.Value.Year, Month = c.CreatedAt!.Value.Month },
                    c => c.CreatedAt >= cutoff
                )
                .GroupBy(c => new { c.Year, c.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToList();

                var months = Enumerable.Range(0, 12)
                    .Select(i => DateTime.Now.AddMonths(-11 + i))
                    .Select(d => new { d.Year, d.Month })
                    .ToList();

                return (object)months.Select(m => new
                {
                    m.Year, m.Month,
                    Legal = legalGrouped.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Count ?? 0,
                    Real  = realGrouped .FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Count ?? 0,
                }).ToList();
            });

        /// <summary>آخرین ۶ ماه نرخ‌های بازار برق</summary>
        [HttpGet("MarketRates")]
        public ExecutionResult MarketRates() =>
            RunExceptionProof(() =>
            {
                var rates = Repository<MonthlyMarketRate>.GetSelectiveList(
                    r => new { r.Year, r.Month, r.MarketAvg, r.GreenBoardRate, r.OpenBoardRate }
                );
                return (object)rates
                    .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
                    .Take(6)
                    .OrderBy(r => r.Year).ThenBy(r => r.Month)
                    .ToList();
            });

        /// <summary>آخرین ۳۰ فعالیت سیستم از AuditTrail</summary>
        [HttpGet("RecentActivity")]
        public ExecutionResult RecentActivity() =>
            RunExceptionProof(() =>
            {
                var auditRows = _logDb.AuditTrail
                    .AsNoTracking()
                    .OrderByDescending(a => a.AuditId)
                    .Take(30)
                    .Select(a => new { a.AuditId, a.TableName, a.RecordId, a.AuditTypeId, a.NewValue, a.UserId, a.CreatedAt })
                    .ToList();

                if (auditRows.Count == 0) return (object)new List<object>();

                var userIds = auditRows.Where(r => r.UserId.HasValue).Select(r => r.UserId!.Value).Distinct().ToList();
                Dictionary<int, string> userNames = new();
                if (userIds.Count > 0)
                {
                    userNames = Repository<User>.GetSelectiveList(
                        u => new { u.Id, u.FullName },
                        u => userIds.Contains(u.Id)
                    ).ToDictionary(u => u.Id, u => u.FullName);
                }

                var auditTypeMap = new Dictionary<int, string>
                {
                    { 1, "ایجاد" }, { 2, "ویرایش" }, { 3, "حذف" },
                    { 4, "تایید" }, { 5, "رد" },      { 6, "چاپ" },
                };

                return (object)auditRows.Select(r => new
                {
                    r.AuditId, r.TableName, r.RecordId,
                    AuditType = r.AuditTypeId.HasValue && auditTypeMap.ContainsKey(r.AuditTypeId.Value)
                        ? auditTypeMap[r.AuditTypeId.Value] : "نامشخص",
                    r.NewValue,
                    UserName  = r.UserId.HasValue && userNames.ContainsKey(r.UserId.Value) ? userNames[r.UserId.Value] : null,
                    r.CreatedAt,
                }).ToList();
            });
    }
}
