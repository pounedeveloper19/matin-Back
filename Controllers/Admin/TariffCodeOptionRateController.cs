using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class TariffCodeOptionRateController : BaseManageController<TariffCodeOptionRate>
    {
        protected override PaginationResult GridDataSource(Expression<Func<TariffCodeOptionRate, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<TariffCodeOptionRate>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.TariffCodeOptionId,
                i.Year,
                i.RateRialPerKwh,
                i.RatePeakRialPerKwh,
                i.RateLowRialPerKwh,
            }, filter, predicate, sortExpression: "Year", sortDirection: System.Web.Helpers.SortDirection.Descending);

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<TariffCodeOptionRate, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<TariffCodeOptionRate, bool>> predicate = i => true;
                int? optionId = UrlArgument<int?>("Search_TariffCodeOptionId");
                if (optionId.HasValue && optionId > 0)
                    predicate = predicate.AppendCondition(i => i.TariffCodeOptionId == optionId.Value, false);
                return predicate;
            }
        }
    }
}
