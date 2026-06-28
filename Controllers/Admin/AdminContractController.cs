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
                i.ContractPowerKw,
                i.ContractVolumeKwh,
                i.ContractAmountRial,
                PaymentDeadline = i.PaymentDeadline.HasValue ? PersianDateConverter.ToPersianDate(i.PaymentDeadline.Value,("yyyy-MM-dd")) : null,
            }, filter, predicate, sortExpression: "StartDate", sortDirection: System.Web.Helpers.SortDirection.Descending,
            includes: new[] { "Warranties", "Subscription.Address.CustomerProfile.CustomersLegal", "Subscription.Address.CustomerProfile.CustomersReal", "Status" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }
        protected override Contract GetItem(int id)
        {
            var contract = id == 0 ? new Contract() : Repository<Contract>.GetSelectiveList(i => new Contract
            {
                Id                 = i.Id,
                SubscriptionId     = i.SubscriptionId,
                ContractNumber     = i.ContractNumber,
                ContractRate       = i.ContractRate,
                StartDate          = i.StartDate,
                EndDate            = i.EndDate,
                StatusId           = i.StatusId,
                FileId             = i.FileId,
                ContractPowerKw    = i.ContractPowerKw,
                ContractVolumeKwh  = i.ContractVolumeKwh,
                ContractAmountRial = i.ContractAmountRial,
                PaymentDeadline    = i.PaymentDeadline,
            }, f => f.Id == id).Last();

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
                    ContractRate       = item.ContractRate,
                    StartDate          = item.StartDate,
                    EndDate            = item.EndDate,
                    StatusId           = 1,
                    SubscriptionId     = item.SubscriptionId,
                    ContractPowerKw    = item.ContractPowerKw,
                    ContractVolumeKwh  = item.ContractVolumeKwh,
                    ContractAmountRial = item.ContractAmountRial,
                    PaymentDeadline    = item.PaymentDeadline,
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
        [Route("[controller]/GetContractPrintData")]
        [HttpGet]
        public ExecutionResult GetContractPrintData(int contractId) =>
            RunExceptionProof(() =>
                Repository<Contract>.Query(db =>
                {
                    var c = db.Contracts
                        .Where(x => x.Id == contractId)
                        .Select(x => new
                        {
                            x.ContractNumber,
                            x.ContractRate,
                            x.ContractPowerKw,
                            x.ContractVolumeKwh,
                            x.ContractAmountRial,
                            StartDate  = PersianDateConverter.ToPersianDate(x.StartDate, "yyyy/MM/dd"),
                            EndDate    = PersianDateConverter.ToPersianDate(x.EndDate,   "yyyy/MM/dd"),
                            Status     = x.Status.Title,
                            Subscription = x.Subscription.BillIdentifier,
                            Address    = x.Subscription.Address.MainAddress,
                            PostalCode = x.Subscription.Address.PostalCode,
                            Province   = x.Subscription.Address.City.Province.Name,
                            CompanyName    = x.Subscription.Address.CustomerProfile.CustomersLegal.CompanyName,
                            NationalId     = x.Subscription.Address.CustomerProfile.CustomersLegal.NationalId,
                            RegisterNumber = x.Subscription.Address.CustomerProfile.CustomersLegal.RegisterNumber,
                            CeoFullName    = x.Subscription.Address.CustomerProfile.CustomersLegal.CeoFullName,
                            CeoNationalId  = x.Subscription.Address.CustomerProfile.CustomersLegal.CeoNationalId,
                            GazetteDate    = PersianDateConverter.ToPersianDate(x.Subscription.Address.CustomerProfile.CustomersLegal.GazetteDate, "yyyy/MM/dd"),
                            PaymentDeadline = x.PaymentDeadline.HasValue ? PersianDateConverter.ToPersianDate(x.PaymentDeadline.Value, "yyyy/MM/dd") : null,
                            WarrantyAmount = x.Warranties.OrderByDescending(w => w.Date).Select(w => (decimal?)w.Amount).FirstOrDefault(),
                            WarrantyType   = x.Warranties.OrderByDescending(w => w.Date).Select(w => w.Type.Title).FirstOrDefault(),
                        })
                        .FirstOrDefault();
                    return (object?)c;
                }));

        [Route("[controller]/RejectContract")]
        [HttpPost]
        public ExecutionResult RejectContract([FromBody] RejectContractRequest req)
        {
            var rejectionStatusId = Repository<EnumContractStatus>.Query(db =>
                db.EnumContractStatuses
                    .Where(s => s.Title == "عدم تایید")
                    .Select(s => (int?)s.Id)
                    .FirstOrDefault());

            if (!rejectionStatusId.HasValue)
                return new ExecutionResult(ResultType.Danger, "خطا", "وضعیت «عدم تایید» در سیستم تعریف نشده — ابتدا migration اجرا کنید", 400);

            return RunExceptionProof(() =>
            {
                var contract = Repository<Contract>.GetLast(c => c.Id == req.ContractId);
                if (contract == null) throw new Exception("قرارداد یافت نشد");
                contract.StatusId = rejectionStatusId.Value;
                contract.RejectionReason = req.Reason;
                Repository<Contract>.UpdateItem(contract);
                return (object)true;
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
