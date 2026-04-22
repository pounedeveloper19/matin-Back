using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Auth
{
    public class AuthController : BaseController
    {
        /// <summary>
        /// ورود به سیستم با شماره موبایل و رمز عبور
        /// </summary>
        [HttpPost]
        [Route("[controller]/Login")]
        public ExecutionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.Password))
                return new ExecutionResult(ResultType.Danger, "خطای ورود", "موبایل و رمز عبور الزامی هستند.", 400);

            var user = Repository<User>.GetLast(i =>
                i.Mobile == request.Mobile &&
                i.Password == request.Password &&
                i.IsActive == true);

            if (user == null)
                return new ExecutionResult(ResultType.Danger, "خطای احراز هویت", "موبایل یا رمز عبور اشتباه است.", 401);

            // تعیین نقش کاربر از جدول UserRole
            var userRole = Repository<UserRole>.GetLast(i => i.UserId == user.Id);
            string role;
            if (userRole != null && userRole.Role != null)
            {
                // اگر عنوان نقش شامل کلمه admin یا مدیر باشد → ادمین
                role = (userRole.Role.Title.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
                        userRole.Role.Title.Contains("مدیر"))
                    ? "admin"
                    : "customer";
            }
            else
            {
                // اگر نقش ثبت نشده: کاربران با CustomerProfileId → مشتری، بقیه → ادمین
                role = user.CustomerProfileId.HasValue ? "customer" : "admin";
            }

            var token = TokenManager.CreateToken(user.Mobile, "0", user.Id.ToString());

            return new ExecutionResult(ResultType.Success, "ورود موفق", null, 200, new LoginResponse
            {
                Token = token,
                Role = role,
                FullName = user.FullName ?? user.Mobile,
            });
        }
    }
}
