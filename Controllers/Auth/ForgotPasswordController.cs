using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using MatinPower.Server.Models.Log;
using MatinPower.Server.Services;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;


namespace MatinPower.Server.Controllers.Auth
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ForgotPasswordController : BaseController
    {
        private readonly LogDbContext _log;

        public ForgotPasswordController(LogDbContext log)
        {
            _log = log;
        }
        [HttpPost]
        public async Task<ExecutionResult> SendOtp([FromBody] ForgotSendOtpRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Mobile))
                    return new ExecutionResult(ResultType.Danger, "خطا", "نام کاربری الزامی است.", 400);

                var user = Repository<User>.GetLast(i => i.Mobile == request.Mobile && i.IsActive == true);
                if (user == null)
                    return new ExecutionResult(ResultType.Danger, "خطا", "نام کاربری در سیستم یافت نشد.", 404);

                // For legal customers → CEO mobile; for real customers → own mobile
                string targetMobile = user.Mobile;
                bool sentToCeo = false;
                if (user.CustomerProfileId.HasValue)
                {
                    var legal = Repository<CustomersLegal>.GetLast(l => l.Id == user.CustomerProfileId.Value);
                    if (legal != null && !string.IsNullOrEmpty(legal.CeoMobile))
                    {
                        targetMobile = legal.CeoMobile;
                        sentToCeo = true;
                    }
                }

                var code = new Random().Next(100000, 999999).ToString();

                using (var db = DbContextProvider.CreateContext())
                {
                    // Invalidate previous unused OTPs for this mobile
                    var previous = db.OtpCodes
                        .Where(o => o.Mobile == request.Mobile && !o.IsUsed && o.ExpiresAt > DateTime.Now)
                        .ToList();
                    previous.ForEach(o => o.IsUsed = true);

                    db.OtpCodes.Add(new OtpCode
                    {
                        Mobile = request.Mobile,
                        Code = code,
                        ExpiresAt = DateTime.Now.AddMinutes(5),
                        IsUsed = false,
                        CreatedAt = DateTime.Now,
                    });
                    await db.SaveChangesAsync();
                }

                var smsTarget = targetMobile.StartsWith("0") ? "+98" + targetMobile.Substring(1) : targetMobile;
                await SmsService.SendAsync(smsTarget, $"کد بازیابی رمز عبور سامانه متین پاور: {code}\nاین کد ۵ دقیقه اعتبار دارد.");

                string msg = sentToCeo
                    ? "کد تأیید به شماره موبایل مدیرعامل ارسال شد."
                    : "کد تأیید به شماره موبایل شما ارسال شد.";
                return new ExecutionResult(ResultType.Success, "کد ارسال شد", msg, 200);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPost]
        public ExecutionResult VerifyOtp([FromBody] ForgotVerifyOtpRequest request)
        {
            using var db = DbContextProvider.CreateContext();

            var entry = db.OtpCodes
                .Where(o => o.Mobile == request.Mobile && o.Code == request.Code && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            if (entry == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کد وارد شده اشتباه است.", 400);

            if (DateTime.Now > entry.ExpiresAt)
                return new ExecutionResult(ResultType.Danger, "خطا", "کد منقضی شده است. مجدداً درخواست کنید.", 400);

            return new ExecutionResult(ResultType.Success, "کد تأیید شد", null, 200);
        }

        [HttpPost]
        public async Task<ExecutionResult> ResetPassword([FromBody] ForgotResetPasswordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 4)
                    return new ExecutionResult(ResultType.Danger, "خطا", "رمز عبور باید حداقل ۴ کاراکتر باشد.", 400);
                if (request.NewPassword.Length > 15)
                    return new ExecutionResult(ResultType.Danger, "خطا", "رمز عبور نباید بیشتر از ۱۵ کاراکتر باشد.", 400);

                using var db = DbContextProvider.CreateContext();

                var entry = db.OtpCodes
                    .Where(o => o.Mobile == request.Mobile && o.Code == request.Code && !o.IsUsed)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefault();

                if (entry == null)
                    return new ExecutionResult(ResultType.Danger, "خطا", "کد نامعتبر است.", 400);

                if (DateTime.Now > entry.ExpiresAt)
                    return new ExecutionResult(ResultType.Danger, "خطا", "کد منقضی شده است.", 400);

                var user = Repository<User>.GetLast(i => i.Mobile == request.Mobile && i.IsActive == true);
                if (user == null)
                    return new ExecutionResult(ResultType.Danger, "خطا", "کاربر یافت نشد.", 404);

                user.Password = request.NewPassword;
                Repository<User>.UpdateItem(user);

                // Mark OTP as used
                entry.IsUsed = true;
                await db.SaveChangesAsync();

                _log.AuditTrail.Add(new AuditTrailEntry
                {
                    TableName   = "Users",
                    RecordId    = user.Id,
                    AuditTypeId = AuditType.Update,
                    NewValue    = $"PasswordChanged via ForgotPassword | Mobile: {request.Mobile} | IP: {HttpContext.Connection.RemoteIpAddress}",
                    UserId      = user.Id,
                    CreatedAt   = DateTime.Now,
                });
                await _log.SaveChangesAsync();

                return new ExecutionResult(ResultType.Success, "رمز تغییر یافت", "رمز عبور با موفقیت تغییر یافت.", 200);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}
