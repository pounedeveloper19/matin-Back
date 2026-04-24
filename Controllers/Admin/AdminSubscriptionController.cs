using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class AdminSubscriptionController : BaseManageController<Subscription>
    {
        protected override PaginationResult GridDataSource(Expression<Func<Subscription, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<Subscription>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.BillIdentifier,
                i.ContractCapacityKw,
                MainAddress = i.Address.MainAddress,
                City        = i.Address.City.Title,
            }, filter, predicate,
               sortExpression: "BillIdentifier",
               sortDirection: System.Web.Helpers.SortDirection.Ascending, includes: new[] { "Address.City", "Address" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<Subscription, bool>> SearchPredicate
        {
            get
            {
                int? parentId = UrlArgument<int?>("parentId");
                Expression<Func<Subscription, bool>> predicate = s => s.Address.CustomerProfileId == parentId;

                string? billId = UrlArgument<string?>("Search_BillIdentifier");
                if (!string.IsNullOrEmpty(billId))
                    predicate = predicate.AppendCondition(s => s.BillIdentifier.Contains(billId), false);

                return predicate;
            }
        }

        protected override Subscription GetItem(int id)
        {
            if (id != 0)
            {
                return Repository<Subscription>.GetSelectiveList(i => new Subscription
                {
                    Id                 = i.Id,
                    AddressId          = i.AddressId,
                    BillIdentifier     = i.BillIdentifier,
                    ContractCapacityKw = i.ContractCapacityKw,
                }, f => f.Id == id).Last();
            }
            return base.GetItem(id);
        }

        public override string? GetHeaderDescription()
        {
            var id = UrlArgument<int?>("parentId");
            if (id == null) return null;

            var customer = Repository<CustomerProfile>.GetSelectiveList(i => new
            {
                NationalId   = i.CustomersLegal!.NationalId,
                CompanyName  = i.CustomersLegal.CompanyName,
                Fullname     = i.CustomersReal!.FirstName + " - " + i.CustomersReal.LastName,
                NationalCode = i.CustomersReal.NationalCode,
                i.CustomerTypeId,
            }, i => i.Id == id).Last();

            if (customer.CustomerTypeId == 1)
                return $": {customer.NationalCode} / شماره ملی:{customer.Fullname} ";
            if (customer.CustomerTypeId == 2)
                return $": {customer.NationalId} / شناسه ملی:{customer.CompanyName} ";
            return null;
        }
    }
}
