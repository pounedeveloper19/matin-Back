using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Route("[controller]")]
    public class PendingUsersController : BaseController
    {
        [HttpGet("List")]
        public ExecutionResult List() =>
            RunExceptionProof(() =>
                Repository<User>.Query(db =>
                {
                    var users = db.Users
                    .Where(u => u.IsActive == false && u.CustomerProfileId != null)
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        u.Mobile,
                        u.CustomerProfileId,
                        CustomerType = u.CustomerProfile != null ? u.CustomerProfile.CustomerTypeId : (int?)null,
                        RealName = u.CustomerProfile != null && u.CustomerProfile.CustomersReal != null
                            ? u.CustomerProfile.CustomersReal.FirstName + " " + u.CustomerProfile.CustomersReal.LastName
                            : null,
                        LegalName = u.CustomerProfile != null && u.CustomerProfile.CustomersLegal != null
                            ? u.CustomerProfile.CustomersLegal.CompanyName
                            : null,
                        NationalCode = u.CustomerProfile != null && u.CustomerProfile.CustomersReal != null
                            ? u.CustomerProfile.CustomersReal.NationalCode
                            : null,
                        NationalId = u.CustomerProfile != null && u.CustomerProfile.CustomersLegal != null
                            ? u.CustomerProfile.CustomersLegal.NationalId
                            : null,
                        RegisteredAt = u.CustomerProfile != null
                            ? (u.CustomerProfile.CustomersReal != null
                                ? PersianDateConverter.ToPersianDate(u.CustomerProfile.CustomersReal.CreatedAt, "yyyy/MM/dd")
                                : u.CustomerProfile.CustomersLegal != null
                                    ? PersianDateConverter.ToPersianDate(u.CustomerProfile.CustomersLegal.CreatedAt, "yyyy/MM/dd")
                                    : (string?)null)
                            : (string?)null,
                        City = u.CustomerProfile != null
                            ? u.CustomerProfile.Addresses
                                .OrderByDescending(a => a.Id)
                                .Select(a => a.City.Title)
                                .FirstOrDefault()
                            : null,
                        Province = u.CustomerProfile != null
                            ? u.CustomerProfile.Addresses
                                .OrderByDescending(a => a.Id)
                                .Select(a => a.City.Province.Name)
                                .FirstOrDefault()
                            : null,
                        MainAddress = u.CustomerProfile != null
                            ? u.CustomerProfile.Addresses
                                .OrderByDescending(a => a.Id)
                                .Select(a => a.MainAddress)
                                .FirstOrDefault()
                            : null,
                        HasIdentityDoc = false,
                        HasAddress = u.CustomerProfile != null && u.CustomerProfile.Addresses.Any(),
                    })
                        .OrderByDescending(u => u.Id)
                        .ToList();

                    return (object)users;
                }));

        [HttpPut("Activate/{userId}")]
        public ExecutionResult Activate(int userId)
        {
            var user = Repository<User>.GetLast(u => u.Id == userId);
            if (user == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر یافت نشد.", 404);

            var result = RunExceptionProof(() =>
            {
                user.IsActive = true;
                Repository<User>.UpdateItem(user);

                if (user.CustomerProfileId.HasValue)
                {
                    var profile = Repository<CustomerProfile>.GetLast(p => p.Id == user.CustomerProfileId);
                    if (profile != null)
                    {
                        profile.IsActive = true;
                        Repository<CustomerProfile>.UpdateItem(profile);
                    }
                }
            });

            if (result.Code == 200 && !string.IsNullOrEmpty(user.Mobile))
            {
                var smsMobile = user.Mobile.StartsWith("0") ? "+98" + user.Mobile.Substring(1) : user.Mobile;
                _ = SmsService.SendAsync(smsMobile, "پروفایل شما در سامانه برق تایید شد. اکنون می‌توانید وارد شوید.");
            }

            return result;
        }

        [HttpDelete("Reject/{userId}")]
        public ExecutionResult Reject(int userId)
        {
            var user = Repository<User>.GetLast(u => u.Id == userId);
            if (user == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر یافت نشد.", 404);

            var profileId = user.CustomerProfileId;

            return RunExceptionProof(() =>
            {
                Repository<User>.DeleteItem(user);

                if (profileId.HasValue)
                {
                    var real = Repository<CustomersReal>.GetLast(r => r.Id == profileId);
                    if (real != null) Repository<CustomersReal>.DeleteItem(real);

                    var legal = Repository<CustomersLegal>.GetLast(l => l.Id == profileId);
                    if (legal != null) Repository<CustomersLegal>.DeleteItem(legal);

                    var profile = Repository<CustomerProfile>.GetLast(p => p.Id == profileId);
                    if (profile != null) Repository<CustomerProfile>.DeleteItem(profile);
                }
            });
        }
    }
}
