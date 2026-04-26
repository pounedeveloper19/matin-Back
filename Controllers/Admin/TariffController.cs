using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class TariffController : BaseManageController<Tariff>
    {
        protected override string SortExpression => "TariffId";

        protected override PaginationResult GridDataSource(Expression<Func<Tariff, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<Tariff>.GetSelectiveListWithPaging(i => new
            {
                TariffId = i.TariffId,
                i.TariffTypeId,
                TariffType = i.TariffType.Title,
                i.CustomerTypeId,
                CustomerType = i.CustomerType.Title,
                i.PowerEntitiesId,
                PowerEntity = i.PowerEntities.Name,
                EffectiveFrom = PersianDateConverter.ToPersianDate(i.EffectiveFrom, "yyyy/MM/dd"),
            }, filter, predicate, sortExpression: "TariffId", sortDirection: System.Web.Helpers.SortDirection.Descending, includes: new[] { "TariffType", "CustomerType", "PowerEntities" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<Tariff, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<Tariff, bool>> result = i => true;
                int? powerEntityId = UrlArgument<int?>("Search_PowerEntityId");
                if (powerEntityId.HasValue && powerEntityId > 0)
                    result = result.AppendCondition(s => s.PowerEntitiesId == powerEntityId.Value, false);
                int? customerTypeId = UrlArgument<int?>("Search_CustomerTypeId");
                if (customerTypeId.HasValue && customerTypeId > 0)
                    result = result.AppendCondition(s => s.CustomerTypeId == customerTypeId.Value, false);
                return result;
            }
        }

        protected override Tariff GetItem(int id)
        {
            return id == 0 ? new Tariff() : Repository<Tariff>.GetItemById(id);
        }
    }
}
