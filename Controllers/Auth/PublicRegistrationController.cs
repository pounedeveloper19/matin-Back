using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Auth
{
    [AllowAnonymous]
    public class PublicRegistrationController : BaseController
    {
        [HttpPost]
        [Route("[controller]/RegisterReal")]
        public ExecutionResult RegisterReal([FromBody] PublicRegisterReal model)
        {
            if (string.IsNullOrWhiteSpace(model.Mobile) || string.IsNullOrWhiteSpace(model.Password))
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "شماره موبایل و رمز عبور الزامی هستند.", 400);

            var existUser = Repository<User>.GetLast(i => i.Mobile == model.Mobile);
            if (existUser != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شماره موبایل قبلا ثبت شده است.", 400);

            var existCustomer = Repository<CustomersReal>.GetLast(i => i.NationalCode == model.NationalCode);
            if (existCustomer != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این کد ملی قبلا در سیستم ثبت شده است.", 400);

            return RunExceptionProof(() =>
            {
                var profile = new CustomerProfile
                {
                    CustomerTypeId = 1,
                    IsActive = false,
                    FamiliarityType = model.FamiliarityType > 0 ? model.FamiliarityType : (int?)null,
                };
                Repository<CustomerProfile>.InsertItem(profile);

                var realCustomer = new CustomersReal
                {
                    Id = profile.Id,
                    NationalCode = model.NationalCode,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Mobile = model.Mobile,
                    CreatedAt = DateTime.Now,
                };
                Repository<CustomersReal>.InsertItem(realCustomer);

                Repository<User>.InsertItem(new User
                {
                    FullName = $"{model.FirstName} {model.LastName}",
                    Mobile = model.Mobile,
                    Password = model.Password,
                    IsActive = false,
                    CustomerProfileId = profile.Id,
                });
            });
        }

        [HttpPost]
        [Route("[controller]/RegisterLegal")]
        public ExecutionResult RegisterLegal([FromBody] PublicRegisterLegal model)
        {
            if (string.IsNullOrWhiteSpace(model.Mobile) || string.IsNullOrWhiteSpace(model.Password))
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "شماره موبایل و رمز عبور الزامی هستند.", 400);

            var existUser = Repository<User>.GetLast(i => i.Mobile == model.Mobile);
            if (existUser != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شماره موبایل قبلا ثبت شده است.", 400);

            var existCustomer = Repository<CustomersLegal>.GetLast(i => i.NationalId == model.NationalId);
            if (existCustomer != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شناسه ملی قبلا در سیستم ثبت شده است.", 400);

            return RunExceptionProof(() =>
            {
                var profile = new CustomerProfile
                {
                    CustomerTypeId = 2,
                    IsActive = false,
                    FamiliarityType = model.FamiliarityType > 0 ? model.FamiliarityType : (int?)null,
                };
                Repository<CustomerProfile>.InsertItem(profile);

                var legalCustomer = new CustomersLegal
                {
                    Id = profile.Id,
                    NationalId = model.NationalId,
                    CompanyName = model.CompanyName,
                    EconomicCode = model.EconomicCode,
                    CeoFullName = model.CeoFullName,
                    CeoMobile = model.CeoMobile,
                    CreatedAt = DateTime.Now,
                };
                Repository<CustomersLegal>.InsertItem(legalCustomer);

                Repository<User>.InsertItem(new User
                {
                    FullName = model.CeoFullName,
                    Mobile = model.Mobile,
                    Password = model.Password,
                    IsActive = false,
                    CustomerProfileId = profile.Id,
                });
            });
        }
    }
}