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
                FamiliarityTitle = i.FamiliarityType.HasValue ? i.FamiliarityTypeNavigation!.Title : string.Empty,
                i.NationalCode,
                Fullname = i.FirstName + " - " + i.LastName,
                i.Mobile
            }, filter, predicate, sortExpression: "CreatedAt", sortDirection: System.Web.Helpers.SortDirection.Descending);

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<CustomersReal, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<Models.CustomersReal, bool>> result = i => true;
                result = result.AppendCondition(s => s.CustomerTypeId == 2, false);
                string? firstName = UrlArgument<string?>("Search_FirstName");
                if (!string.IsNullOrEmpty(firstName))
                    result = result.AppendCondition(s => s.FirstName.Contains(firstName), false);
                string? lastName = UrlArgument<string?>("Search_LastName");
                if (!string.IsNullOrEmpty(lastName))
                    result = result.AppendCondition(s => s.LastName.Contains(lastName), false);
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
