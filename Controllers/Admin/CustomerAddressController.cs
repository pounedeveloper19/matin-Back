using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class CustomerAddressController : BaseManageController<Models.Address>
    {
        protected override PaginationResult GridDataSource(Expression<Func<Address, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<Address>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                City = i.City.Title,
                Province = i.City.Province.Name,
                i.PostalCode,
                i.MainAddress,
            }, filter, predicate, sortExpression: "PostalCode", sortDirection: System.Web.Helpers.SortDirection.Descending);

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<Address, bool>> SearchPredicate
        {
            get
            {
                long? parentId = UrlArgument<long?>("parentId");

                Expression<Func<Address, bool>> result = s => s.CustomerProfileId == parentId;

                string? postalCode = UrlArgument<string?>("Search_PostalCode");
                if (!string.IsNullOrEmpty(postalCode))
                    result = result.AppendCondition(s => s.PostalCode.Contains(postalCode), false);
                return result;
            }
        }


        protected override Address GetItem(int id)
        {
            var item = base.GetItem(id);
            if (id == 0)
                item.CustomerProfileId = UrlArgument<int>("parentId");
            if (id != 0)
            {
                var result = Repository<Address>.GetSelectiveList(i => new Address
                {
                    Id = i.Id,
                    CityId = i.CityId,
                    CustomerProfileId = i.CustomerProfileId,
                    PostalCode = i.PostalCode,
                    MainAddress = i.MainAddress,
                    PowerEntityId = i.PowerEntityId,

                }, f => f.Id == id).Last();
                return result;
            }
            return item;
        }
        public override string? GetHeaderDescription()
        {
            var id = UrlArgument<int?>("parentId");
            var header = string.Empty;
            var customer = Repository<CustomerProfile>.GetSelectiveList(i => new { NationalId = i.CustomersLegal!.NationalId, CompanyName = i.CustomersLegal.CompanyName, Fullname = i.CustomersReal!.FirstName + " - " + i.CustomersReal.LastName, NationalCode = i.CustomersReal.NationalCode, i.CustomerTypeId }, i => i.Id == id).Last();
            if (customer.CustomerTypeId == 1)
                header = $": {customer.NationalCode} / شماره ملی:{customer.Fullname} ";
            if (customer.CustomerTypeId == 2)
                header = $": {customer.NationalId} / شناسه ملی:{customer.CompanyName} ";
            return header;
        }


    }
}
