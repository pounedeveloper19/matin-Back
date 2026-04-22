using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Route("[controller]")]
    public class PendingUsersController : BaseController
    {
        [HttpGet("List")]
        public ExecutionResult List()
        {
            return RunExceptionProof(() =>
            {
                var users = Repository<User>
                    .GetListExtended(u => u.IsActive == false && u.CustomerProfileId != null)
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
                    })
                    .OrderByDescending(u => u.Id)
                    .ToList();

                return new ExecutionResult(ResultType.Success, null, null, 200, users);
            });
        }

        [HttpPut("Activate/{userId}")]
        public ExecutionResult Activate(int userId)
        {
            return RunExceptionProof(() =>
            {
                var user = Repository<User>.GetLast(u => u.Id == userId);
                if (user == null)
                    return new ExecutionResult(ResultType.Danger, "خطا", "کاربر یافت نشد.", 404);

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

                return new ExecutionResult(ResultType.Success, "موفق", "کاربر با موفقیت فعال شد.", 200);
            });
        }

        [HttpDelete("Reject/{userId}")]
        public ExecutionResult Reject(int userId)
        {
            return RunExceptionProof(() =>
            {
                var user = Repository<User>.GetLast(u => u.Id == userId);
                if (user == null)
                    return new ExecutionResult(ResultType.Danger, "خطا", "کاربر یافت نشد.", 404);

                var profileId = user.CustomerProfileId;
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

                return new ExecutionResult(ResultType.Success, "موفق", "درخواست رد شد.", 200);
            });
        }
    }
}
