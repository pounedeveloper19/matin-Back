using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class TariffCodeOptionController : BaseManageController<TariffCodeOption>
    {
        protected override PaginationResult GridDataSource(Expression<Func<TariffCodeOption, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<TariffCodeOption>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.TariffCodeId,
                TariffCode = i.TariffCode.Title,
                TariffCodeCode = i.TariffCode.Code,
                i.Title,
                i.PenaltyMultiplier,
                i.CreditMultiplier,
            }, filter, predicate, sortExpression: "Title", sortDirection: System.Web.Helpers.SortDirection.Ascending,
               includes: new[] { "TariffCode" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<TariffCodeOption, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<TariffCodeOption, bool>> predicate = i => true;
                int? tariffCodeId = UrlArgument<int?>("Search_TariffCodeId");
                if (tariffCodeId.HasValue && tariffCodeId > 0)
                    predicate = predicate.AppendCondition(i => i.TariffCodeId == tariffCodeId.Value, false);
                return predicate;
            }
        }
    }
}
