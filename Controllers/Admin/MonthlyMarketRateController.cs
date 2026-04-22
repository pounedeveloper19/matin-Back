using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class MonthlyMarketRateController : BaseManageController<MonthlyMarketRate>
    {
        protected override PaginationResult GridDataSource(Expression<Func<MonthlyMarketRate, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<MonthlyMarketRate>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.Year,
                i.Month,
                i.MarketPeak,
                i.MarketMid,
                i.MarketLow,
                i.BackupRate,
                i.BoardPeak,
                i.BoardMid,
                i.BoardLow,
                i.GreenBoardRate,
                i.Article16Rate,
                i.FuelFee,
                i.IndustrialTariffBase,
                i.ExecutiveTariffBase,
            }, filter, predicate, sortExpression: "Year", sortDirection: System.Web.Helpers.SortDirection.Descending);

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<MonthlyMarketRate, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<MonthlyMarketRate, bool>> result = i => true;
                int? year = UrlArgument<int?>("Search_Year");
                if (year.HasValue && year > 0)
                    result = result.AppendCondition(s => s.Year == year.Value, false);
                int? month = UrlArgument<int?>("Search_Month");
                if (month.HasValue && month > 0)
                    result = result.AppendCondition(s => s.Month == month.Value, false);
                return result;
            }
        }

        [HttpGet]
        [Route("[controller]/GetByYearMonth")]
        public ExecutionResult GetByYearMonth()
        {
            return RunExceptionProof(() =>
            {
                int year  = UrlArgument<int>("year");
                int month = UrlArgument<int>("month");
                var rate  = Repository<MonthlyMarketRate>.GetLast(i => i.Year == year && i.Month == month);
                return new ExecutionResult(ResultType.Success, null, null, 200, rate);
            });
        }
    }
}
