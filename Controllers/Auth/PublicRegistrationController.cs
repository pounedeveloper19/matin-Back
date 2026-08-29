using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using MatinPower.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Auth
{
    [AllowAnonymous]
    public class PublicRegistrationController : BaseController
    {
        private static async Task NotifyAdminsOfNewRegistrationAsync(string customerName, string customerType)
        {
            try
            {
                var mobiles = Repository<User>.Query(db =>
                    db.Users
                        .Where(u => u.IsActive == true && u.CustomerProfileId == null && u.Mobile != null)
                        .Select(u => u.Mobile)
                        .ToList());

                foreach (var mobile in mobiles)
                {
                    var normalized = mobile.StartsWith("0") ? "+98" + mobile.Substring(1) : mobile;
                    await SmsService.SendAsync(normalized,
                        $"درخواست ثبت‌نام جدید ({customerType}) در سامانه متین‌پاور ثبت شد.\nنام: {customerName}\nلطفاً از پنل مدیریت بررسی و تایید کنید.");
                }
            }
            catch
            {
                // اطلاع‌رسانی نباید مسیر اصلی ثبت‌نام کاربر را مختل کند
            }
        }

        [HttpPost]
        [Route("[controller]/RegisterReal")]
        public ExecutionResult RegisterReal([FromBody] PublicRegisterReal model)
        {
            if (string.IsNullOrWhiteSpace(model.Mobile) || string.IsNullOrWhiteSpace(model.Password))
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "شماره موبایل و رمز عبور الزامی هستند.", 400);

            var existUser = Repository<User>.GetLast(i => i.Mobile == model.Mobile && i.IsActive == true);
            if (existUser != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شماره موبایل قبلا ثبت شده است.", 400);

            var existCustomer = Repository<CustomersReal>.GetLast(i => i.NationalCode == model.NationalCode && i.CustomerProfile.IsActive == true);
            if (existCustomer != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این کد ملی قبلا در سیستم ثبت شده است.", 400);

            var result = RunExceptionProof(() =>
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

            if (result.Code == 200)
                _ = NotifyAdminsOfNewRegistrationAsync($"{model.FirstName} {model.LastName}", "حقیقی");

            return result;
        }

        [HttpPost]
        [Route("[controller]/RegisterLegal")]
        public ExecutionResult RegisterLegal([FromBody] PublicRegisterLegal model)
        {
            if (string.IsNullOrWhiteSpace(model.Mobile) || string.IsNullOrWhiteSpace(model.Password))
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "شماره موبایل و رمز عبور الزامی هستند.", 400);

            var existUser = Repository<User>.GetLast(i => i.Mobile == model.Mobile && i.IsActive == true);
            if (existUser != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شماره موبایل قبلا ثبت شده است.", 400);

            var existCustomer = Repository<CustomersLegal>.GetLast(i => i.NationalId == model.NationalId && i.CustomerProfile.IsActive == true);
            if (existCustomer != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شناسه ملی قبلا در سیستم ثبت شده است.", 400);

            var result = RunExceptionProof(() =>
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

            if (result.Code == 200)
                _ = NotifyAdminsOfNewRegistrationAsync(model.CompanyName, "حقوقی");

            return result;
        }
    }
}