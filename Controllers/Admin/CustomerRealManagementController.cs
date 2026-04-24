using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class CustomerRealManagementController : BaseManageController<Models.CustomersReal>
    {
        protected override PaginationResult GridDataSource(Expression<Func<Models.CustomersReal, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<Models.CustomersReal>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.NationalCode,
                i.FirstName,
                i.LastName,
                i.Mobile,
                i.CustomerProfile.IsActive,
                i.CustomerProfile.FamiliarityType,
                i.CustomerProfile.CustomerTypeId,
            }, filter, predicate, sortExpression: "CreatedAt", sortDirection: System.Web.Helpers.SortDirection.Descending, includes: new[] { "CustomerProfile" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<CustomersReal, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<Models.CustomersReal, bool>> result = i => true;
                result = result.AppendCondition(s => s.CustomerProfile.CustomerTypeId == 1, false);
                string? name = UrlArgument<string?>("Search_Name");
                if (!string.IsNullOrEmpty(name))
                    result = result.AppendCondition(s => s.FirstName.Contains(name) || s.LastName.Contains(name), false);
                string?nationalCode = UrlArgument<string?>("Search_NationalCode");
                if (!string.IsNullOrEmpty(nationalCode))
                    result = result.AppendCondition(s => s.NationalCode.Contains(nationalCode), false);
                string? mobile = UrlArgument<string?>("Search_Mobile");
                if (!string.IsNullOrEmpty(mobile))
                    result = result.AppendCondition(s => s.Mobile.Contains(mobile), false);
                return result;
            }
        }

    }
}
