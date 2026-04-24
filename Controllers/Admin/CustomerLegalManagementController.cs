using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class CustomerLegalManagementController : BaseManageController<Models.CustomersLegal>
    {
        protected override PaginationResult GridDataSource(Expression<Func<Models.CustomersLegal, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<Models.CustomersLegal>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.NationalId,
                i.CeoFullName,
                i.CompanyName,
                i.CeoMobile,
                i.CustomerProfile.IsActive,
                i.CustomerProfile.FamiliarityType,
                i.CustomerProfile.CustomerTypeId,
            }, filter, predicate, sortExpression: "CreatedAt", sortDirection: System.Web.Helpers.SortDirection.Descending, includes: new[] { "CustomerProfile" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<CustomersLegal, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<Models.CustomersLegal, bool>> result = i => true;
                string? companyName = UrlArgument<string?>("Search_CompanyName");
                if (!string.IsNullOrEmpty(companyName))
                    result = result.AppendCondition(s => s.CompanyName.Contains(companyName), false);
                string? nationalId = UrlArgument<string?>("Search_NationalId");
                if (!string.IsNullOrEmpty(nationalId))
                    result = result.AppendCondition(s => s.NationalId.Contains(nationalId), false);
                string? ceoFullName = UrlArgument<string?>("Search_CeoFullName");
                if (!string.IsNullOrEmpty(ceoFullName))
                    result = result.AppendCondition(s => s.CeoFullName.Contains(ceoFullName), false);
                return result;
            }
        }
    }
}
