using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class PowerEntityController : BaseManageController<PowerEntity>
    {
        protected override PaginationResult GridDataSource(Expression<Func<PowerEntity, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<PowerEntity>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.Name,
                i.ProvinceId,
                Province = i.Province.Name,
                i.EntityTypeId,
                EntityType = i.EntityType.Title,
                i.IsActive,
            }, filter, predicate, sortExpression: "Id", sortDirection: System.Web.Helpers.SortDirection.Ascending,
            includes: new[] { "Province", "EntityType" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }
    }
}
