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
                    ContractPowerKw    = i.ContractPowerKw,
                    ContractVolumeKwh  = i.ContractVolumeKwh,
                    ContractAmountRial = i.ContractAmountRial,
                    PaymentDeadline    = i.PaymentDeadline.HasValue ? PersianDateConverter.ToPersianDate(i.PaymentDeadline.Value, "yyyy/MM/dd") : null,
                    RejectionReason    = i.RejectionReason,
                }, i => i.Subscription.Address.CustomerProfileId == customerId.Value,
                   includes: new[] { "Warranties.Type", "Status", "Subscription" , "Subscription", "Subscription.Address", "Warranties" });
                return (object)result;
            });
        }

        [HttpPost]
        [Route("[controller]/CreateContract")]
        public ExecutionResult CreateContract([FromBody] CreateContractRequest request)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            if ((request.ContractPowerKw ?? 0) <= 0)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "قدرت درخواستی (kW) را وارد کنید.", 400);

            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value > request.EndDate.Value)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "تاریخ پایان باید بعد از تاریخ شروع باشد.", 400);

            var subscription = Repository<Subscription>.Query(db =>
                db.Subscriptions
                    .Where(s => s.Id == request.SubscriptionId && s.Address.CustomerProfileId == customerId.Value)
                    .Select(s => new { s.Id })
                    .FirstOrDefault());
            if (subscription == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "اشتراک یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                // حجم درخواستی همیشه سمت سرور از قدرت درخواستی محاسبه می‌شود (نه مقدار ارسالی کلاینت)
                // تا هرگز از CK_Contract_VolumeNotExceedCapacity در دیتابیس تخطی نکند
                var contract = Repository<Contract>.InsertItem(new Contract
                {
                    SubscriptionId = request.SubscriptionId,
                    ContractRate = 0,
                    ContractPowerKw = request.ContractPowerKw,
                    ContractVolumeKwh = request.ContractPowerKw * 8760,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    StatusId = 1,
                });

                var pc = new System.Globalization.PersianCalendar();
                var now = DateTime.Now;
                contract.ContractNumber = $"CNT-{pc.GetYear(now)}{pc.GetMonth(now):00}-{contract.Id:00000}";
                Repository<Contract>.UpdateItem(contract);

                return (object)contract.Id;
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
