using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class TariffSlabController : BaseManageController<TariffSlab>
    {
        protected override PaginationResult GridDataSource(Expression<Func<TariffSlab, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<TariffSlab>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.TariffId,
                i.FromKwh,
                i.ToKwh,
                i.Multiplier,
            }, filter, predicate, sortExpression: "TariffId", sortDirection: System.Web.Helpers.SortDirection.Ascending);

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<TariffSlab, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<TariffSlab, bool>> result = i => true;
                int? tariffId = UrlArgument<int?>("Search_TariffId");
                if (tariffId.HasValue && tariffId > 0)
                    result = result.AppendCondition(s => s.TariffId == tariffId.Value, false);
                return result;
            }
        }
    }
}
