using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;
using Contract = MatinPower.Server.Models.Contract;

namespace MatinPower.Server.Controllers.Admin
{
    public class AdminContractController : BaseManageController<Contract>
    {
        protected override PaginationResult GridDataSource(Expression<Func<Contract, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<Contract>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                CustomerNationalId = i.Subscription.Address.CustomerProfile.CustomersLegal.NationalId,
                Status = i.Status.Title,
                i.ContractNumber,
                StartDate = PersianDateConverter.ToPersianDate(i.StartDate),
                EndDate = PersianDateConverter.ToPersianDate(i.EndDate),
            }, filter, predicate, sortExpression: "StartDate", sortDirection: System.Web.Helpers.SortDirection.Descending);

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }
        public override ExecutionResult Insert([FromBody] Contract item)
        {
            if (!ModelState.IsValid)
                return ExecutionResult.Failure;
            return RunExceptionProof(() =>
            {
                var contract = Repository<Contract>.InsertItem(new Contract
                {
                    ContractNumber = item.ContractNumber,
                    ContractRate = item.ContractRate,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    StatusId = 1,
                    SubscriptionId = item.SubscriptionId
                });
                Repository<Warranty>.InsertItem(new Warranty
                {
                    ContractId = contract.Id,
                    Amount = item.Amount,
                    FileId = item.FileId,
                    TypeId = item.TypeId,
                    Date = DateTime.Now
                });
            });
        }
        public override ExecutionResult Update([FromBody] Contract item)
        {
            var warranty = Repository<Warranty>.GetLast(i => i.ContractId == item.Id);
            if (warranty == null)
            {
                Repository<Warranty>.InsertItem(new Warranty
                {
                    ContractId = item.Id,
                    Amount = item.Amount,
                    FileId = item.FileId,
                    TypeId = item.TypeId,
                    Date = DateTime.Now
                });
            }
            else
            {
                warranty.Amount = item.Amount;
                warranty.FileId = item.FileId;
                warranty.TypeId = item.TypeId;
                Repository<Warranty>.UpdateItem(warranty);
            }
                return base.Update(item);
        }

    }
}
