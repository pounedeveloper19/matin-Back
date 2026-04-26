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
                CustomerNationalId = i.Subscription.Address.CustomerProfile.CustomersLegal.NationalId
                    ?? i.Subscription.Address.CustomerProfile.CustomersReal.NationalCode,
                CustomerName = i.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName
                    ?? (i.Subscription.Address.CustomerProfile.CustomersReal.FirstName + " " + i.Subscription.Address.CustomerProfile.CustomersReal.LastName),
                Status = i.Status.Title,
                i.ContractNumber,
                StartDate = PersianDateConverter.ToPersianDate(i.StartDate, "yyyy/MM/dd"),
                EndDate = PersianDateConverter.ToPersianDate(i.EndDate, "yyyy/MM/dd"),
                WarrantyFileId = i.Warranties.OrderByDescending(w => w.Date).Select(w => w.FileId).FirstOrDefault(),
            }, filter, predicate, sortExpression: "StartDate", sortDirection: System.Web.Helpers.SortDirection.Descending,
            includes: new[] { "Warranties", "Subscription.Address.CustomerProfile.CustomersLegal", "Subscription.Address.CustomerProfile.CustomersReal", "Status" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }
        protected override Contract GetItem(int id)
        {
            var contract = id == 0 ? new Contract() : Repository<Contract>.GetItemById(id);
            if (id != 0)
            {
                var warranty = Repository<Warranty>.GetLast(i => i.ContractId == id);
                if (warranty != null)
                {
                    contract.Amount = warranty.Amount;
                    contract.TypeId = warranty.TypeId;
                    contract.FileId = warranty.FileId;
                }
            }
            return contract;
        }

        public override ExecutionResult Insert([FromBody] Contract item)
        {
            if (!ModelState.IsValid)
                return ExecutionResult.Failure;
            return RunExceptionProof(() =>
            {
                var contract = Repository<Contract>.InsertItem(new Contract
                {
                    ContractRate = item.ContractRate,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    StatusId = 1,
                    SubscriptionId = item.SubscriptionId
                });

                var pc = new System.Globalization.PersianCalendar();
                var now = DateTime.Now;
                contract.ContractNumber = $"CNT-{pc.GetYear(now)}{pc.GetMonth(now):00}-{contract.Id:00000}";
                Repository<Contract>.UpdateItem(contract);

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
