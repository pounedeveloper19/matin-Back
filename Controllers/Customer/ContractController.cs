using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Result;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Customer
{
    public class ContractController : BaseController
    {
        private int? GetCustomerProfileId()
        {
            var userId = new UseContext(new HttpContextAccessor()).GetUserId();
            if (userId == null) return null;
            var user = Repository<User>.GetLast(i => i.Id == userId.Value);
            return user?.CustomerProfileId;
        }

        [HttpPost]
        [Route("[controller]/GetContractList")]
        public ExecutionResult GetContractList()
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            return RunExceptionProof(() =>
            {
                var result = Repository<Contract>.GetSelectiveList(i => new ContractResult
                {
                    Id = i.Id,
                    ContractNumber = i.ContractNumber,
                    ContractRate = i.ContractRate,
                    StatusId = i.StatusId,
                    Status = i.Status.Title!,
                    Subscription = i.Subscription.BillIdentifier,
                    StartDate = i.StartDate.HasValue ? PersianDateConverter.ToPersianDate(i.StartDate.Value, "yyyy/MM/dd") : "",
                    EndDate = i.EndDate.HasValue ? PersianDateConverter.ToPersianDate(i.EndDate.Value, "yyyy/MM/dd") : "",
                    Address = i.Subscription.Address.MainAddress ?? "",
                    WarrantyAmount = i.Warranties.Any() ? i.Warranties.OrderByDescending(w => w.Id).First().Amount : 0,
                    WarrantyType = i.Warranties.Any() ? (i.Warranties.OrderByDescending(w => w.Id).First().Type.Title ?? "") : "",
                    WarrantyTypeId = i.Warranties.Any() ? i.Warranties.OrderByDescending(w => w.Id).First().TypeId : 0,
                    WarrantyFileId = i.Warranties.Any() && i.Warranties.OrderByDescending(w => w.Id).First().FileId.HasValue
                        ? i.Warranties.OrderByDescending(w => w.Id).First().FileId.ToString()
                        : null,
                }, i => i.Subscription.Address.CustomerProfileId == customerId.Value,
                   includes: new[] { "Warranties.Type", "Status", "Subscription" , "Subscription", "Subscription.Address", "Warranties" });
                return (object)result;
            });
        }

        [HttpPost]
        [Route("[controller]/SubmitWarranty")]
        public ExecutionResult SubmitWarranty([FromBody] SubmitWarrantyRequest request)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var contract = Repository<Contract>.GetLast(
                i => i.Id == request.ContractId && i.Subscription.Address.CustomerProfileId == customerId.Value);

            if (contract == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "قرارداد یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                var existing = Repository<Warranty>.GetLast(i => i.ContractId == request.ContractId);
                if (existing != null)
                {
                    existing.Amount = request.Amount;
                    existing.TypeId = request.TypeId;
                    if (request.FileId.HasValue) existing.FileId = request.FileId;
                    existing.Date = DateTime.Now;
                    Repository<Warranty>.UpdateItem(existing);
                }
                else
                {
                    Repository<Warranty>.InsertItem(new Warranty
                    {
                        ContractId = request.ContractId,
                        Amount = request.Amount,
                        TypeId = request.TypeId,
                        FileId = request.FileId,
                        Date = DateTime.Now,
                    });
                }

                contract.StatusId = 2;
                Repository<Contract>.UpdateItem(contract);
            });
        }
    }
}
