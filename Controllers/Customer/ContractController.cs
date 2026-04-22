using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Result;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Customer
{
    public class ContractController : BaseController
    {
        [HttpPost]
        [Route("[controller]/GetContractList")]
        public ExecutionResult GetContractList()
        {
            var customerId = new UseContext(new HttpContextAccessor()).GetCustomerId();
            var addressId = new UseContext(new HttpContextAccessor()).GetAddressId();
            var address = Repository<Address>.GetLast(i => i.Id == addressId);
            var result = Repository<Contract>.GetSelectiveList(i => new ContractResult
            {
                Id = i.Id,
                ContractNumber = i.ContractNumber,
                ContractRate = i.ContractRate,
                Status = i.Status.Title!,
                Subscription = i.Subscription.BillIdentifier,
                StartDate = PersianDateConverter.ToPersianDate(i.StartDate!.Value),
                EndDate = PersianDateConverter.ToPersianDate(i.EndDate!.Value),
                Address = address.City + " - " + address.MainAddress + " - " + address.PostalCode,
                WarrantyAmount = i.Warranties.LastOrDefault(w => w.ContractId == i.Id)!.Amount,
                WarrantyType = i.Warranties.LastOrDefault(w => w.ContractId == i.Id)!.Type.Title!,

            }, i => i.Subscription.Address.CustomerProfileId == customerId);
            return RunExceptionProof(() =>
            {
                return new ExecutionResult(ResultType.Success, null, null, 200, result);
            });
        }
        [HttpPut]
        [Route("[controller]/ConfirmContract/{contractId}")]
        public ExecutionResult ConfirmContract([FromBody] ContractConfirm confirm)
        {
            var contract = Repository<Contract>.GetLast(i => i.Id == confirm.ContractId);
            if (contract == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "قرارداد یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                contract.StatusId = confirm.StatusId;
                Repository<Contract>.UpdateItem(contract);
                return new ExecutionResult(ResultType.Success, null, null, 200, contract.Id);
            });
        }
    }
}
