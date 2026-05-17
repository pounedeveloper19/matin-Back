using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class TariffCodeController : BaseManageController<TariffCode>
    {
        protected override PaginationResult GridDataSource(Expression<Func<TariffCode, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<TariffCode>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.Code,
                i.Title,
                OptionCount = i.Options.Count,
            }, filter, predicate, sortExpression: "Code", sortDirection: System.Web.Helpers.SortDirection.Ascending);

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<TariffCode, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<TariffCode, bool>> predicate = i => true;
                string? search = UrlArgument<string?>("Search_Title");
                if (!string.IsNullOrEmpty(search))
                    predicate = predicate.AppendCondition(i => i.Title.Contains(search) || i.Code.Contains(search), false);
                return predicate;
            }
        }
    }
}
