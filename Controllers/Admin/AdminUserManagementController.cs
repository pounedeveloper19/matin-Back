using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Route("[controller]")]
    public class AdminUserManagementController : BaseController
    {
        [HttpGet("List")]
        public ExecutionResult List(int pageNumber = 1, int pageSize = 20, string? search = null, string? isActive = null) =>
            RunExceptionProof(() =>
                Repository<User>.Query(db =>
                {
                    var query = db.Users.AsQueryable();

                    if (!string.IsNullOrWhiteSpace(search))
                        query = query.Where(u => u.FullName.Contains(search) || u.Mobile.Contains(search));

                    if (isActive == "true")
                        query = query.Where(u => u.IsActive == true);
                    else if (isActive == "false")
                        query = query.Where(u => u.IsActive != true);

                    var totalRecords = query.Count();

                    var data = query
                        .OrderByDescending(u => u.Id)
                        .Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .Select(u => new
                        {
                            u.Id,
                            u.FullName,
                            u.Mobile,
                            u.IsActive,
                            u.CustomerProfileId,
                            RoleId = (int?)db.UserRoles
                                .Where(ur => ur.UserId == u.Id)
                                .Select(ur => ur.RoleId)
                                .FirstOrDefault(),
                            RoleTitle = db.UserRoles
                                .Where(ur => ur.UserId == u.Id)
                                .Select(ur => ur.Role.Title)
                                .FirstOrDefault(),
                            CustomerName = u.CustomerProfile != null
                                ? (u.CustomerProfile.CustomersReal != null
                                    ? u.CustomerProfile.CustomersReal.FirstName + " " + u.CustomerProfile.CustomersReal.LastName
                                    : u.CustomerProfile.CustomersLegal != null
                                        ? u.CustomerProfile.CustomersLegal.CompanyName
                                        : null)
                                : null,
                        })
                        .ToList();

                    return (object)new
                    {
                        PageNumber    = pageNumber,
                        PageSize      = pageSize,
                        TotalRecords  = totalRecords,
                        TotalPages    = (int)Math.Ceiling((double)totalRecords / pageSize),
                        FilteredCount = totalRecords,
                        Data          = data,
                    };
                }));

        [HttpPut("SetRole")]
        public ExecutionResult SetRole([FromBody] SetUserRoleRequest request)
        {
            var userExists = Repository<User>.Query(db => db.Users.Any(u => u.Id == request.UserId));
            if (!userExists)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                Repository<UserRole>.ExecuteCommand(db =>
                {
                    var existing = db.UserRoles.Where(ur => ur.UserId == request.UserId).ToList();
                    db.UserRoles.RemoveRange(existing);
                    if (request.RoleId.HasValue)
                        db.UserRoles.Add(new UserRole { UserId = request.UserId, RoleId = request.RoleId.Value });
                    db.SaveChanges();
                });
            });
        }

        [HttpPut("ToggleActive/{userId}")]
        public ExecutionResult ToggleActive(int userId)
        {
            var user = Repository<User>.GetLast(u => u.Id == userId);
            if (user == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                user.IsActive = !(user.IsActive ?? false);
                Repository<User>.UpdateItem(user);

                // پروفایل مشتری مرتبط هم هم‌زمان همگام می‌شود تا وضعیت «فعال» در همه جا یکسان بماند
                if (user.CustomerProfileId.HasValue)
                {
                    var profile = Repository<CustomerProfile>.GetLast(p => p.Id == user.CustomerProfileId);
                    if (profile != null && profile.IsActive != user.IsActive)
                    {
                        profile.IsActive = user.IsActive;
                        Repository<CustomerProfile>.UpdateItem(profile);
                    }
                }
            });
        }

        [HttpPut("UpdateUser")]
        public ExecutionResult UpdateUser([FromBody] UpdateUserRequest request)
        {
            var user = Repository<User>.GetLast(u => u.Id == request.Id);
            if (user == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر یافت نشد.", 404);

            if (user.Mobile != request.Mobile)
            {
                var mobileExists = Repository<User>.Query(db =>
                    db.Users.Any(u => u.Mobile == request.Mobile && u.Id != request.Id));
                if (mobileExists)
                    return new ExecutionResult(ResultType.Danger, "خطا", "این شماره موبایل قبلاً ثبت شده است.", 409);
            }

            return RunExceptionProof(() =>
            {
                user.FullName = request.FullName;
                user.Mobile   = request.Mobile;
                if (!string.IsNullOrWhiteSpace(request.Password))
                    user.Password = request.Password;
                Repository<User>.UpdateItem(user);
            });
        }

        [HttpPost("CreateAdmin")]
        public ExecutionResult CreateAdmin([FromBody] CreateAdminRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile))
                return new ExecutionResult(ResultType.Danger, "خطا", "موبایل الزامی است.", 400);
            if (string.IsNullOrWhiteSpace(request.Password))
                return new ExecutionResult(ResultType.Danger, "خطا", "رمز عبور الزامی است.", 400);

            var mobileExists = Repository<User>.Query(db => db.Users.Any(u => u.Mobile == request.Mobile));
            if (mobileExists)
                return new ExecutionResult(ResultType.Danger, "خطا", "این شماره موبایل قبلاً ثبت شده است.", 409);

            return RunExceptionProof(() =>
            {
                Repository<User>.ExecuteCommand(db =>
                {
                    var user = new User
                    {
                        FullName          = string.IsNullOrWhiteSpace(request.FullName) ? request.Mobile : request.FullName,
                        Mobile            = request.Mobile,
                        Password          = request.Password,
                        IsActive          = true,
                        CustomerProfileId = null,
                    };

                    db.Users.Add(user);
                    db.SaveChanges();

                    if (request.RoleId.HasValue)
                    {
                        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = request.RoleId.Value });
                        db.SaveChanges();
                    }
                });
            });
        }
    }
}
