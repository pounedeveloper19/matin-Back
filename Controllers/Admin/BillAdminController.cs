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
                i.CreatedAt,
            }, filter, predicate, sortExpression: "CreatedAt", sortDirection: System.Web.Helpers.SortDirection.Descending);

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
