using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using MatinPower.Server.Services;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Route("[controller]/[action]")]
    public class AdminTicketController : BaseController
    {
        [HttpGet]
        public ExecutionResult GetAll()
        {
            return RunExceptionProof(() =>
            {
                var tickets = Repository<Ticket>.GetSelectiveList(i => new
                {
                    i.Id,
                    i.Subject,
                    Status = i.Status.Title,
                    i.StatusId,
                    CustomerName = i.CustomerProfile.CustomersReal != null
                        ? i.CustomerProfile.CustomersReal.FirstName + " " + i.CustomerProfile.CustomersReal.LastName
                        : i.CustomerProfile.CustomersLegal != null
                        ? i.CustomerProfile.CustomersLegal.CompanyName
                        : "نامشخص",
                    CreatedAt = PersianDateConverter.ToPersianDate(i.CreatedAt, "yyyy/MM/dd"),
                    MessageCount = i.TicketMessages.Count,
                }, i => true, includes: new[] { "Status", "CustomerProfile.CustomersReal", "CustomerProfile.CustomersLegal", "TicketMessages" });

                return (object)tickets;
            });
        }

        [HttpGet]
        public ExecutionResult GetMessages(int ticketId)
        {
            return RunExceptionProof(() =>
            {
                var messages = Repository<TicketMessage>.GetSelectiveList(i => new
                {
                    i.Id,
                    i.Body,
                    FileId = i.FileId.HasValue ? i.FileId.ToString() : (string?)null,
                    SenderName = i.SenderUser.FullName ?? "کاربر",
                    IsAdmin = i.SenderUser.CustomerProfileId == null,
                    CreatedAt = PersianDateConverter.ToPersianDate(i.CreatedAt, "yyyy/MM/dd"),
                }, i => i.TicketId == ticketId, includes: new[] { "SenderUser" });

                return (object)messages;
            });
        }

        [HttpPost]
        public ExecutionResult Reply([FromBody] AddTicketMessageRequest request)
        {
            var userId = new UseContext(new HttpContextAccessor()).GetUserId();
            if (userId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "احراز هویت نشده.", 401);

            var result = RunExceptionProof(() =>
            {
                Repository<TicketMessage>.InsertItem(new TicketMessage
                {
                    TicketId     = request.TicketId,
                    Body         = request.Body,
                    FileId       = request.FileId,
                    SenderUserId = userId.Value,
                    CreatedAt    = DateTime.Now,
                });
            });

            if (result.Code == 200)
            {
                var ticket = Repository<Ticket>.GetLast(t => t.Id == request.TicketId);
                if (ticket != null)
                {
                    var mobile = GetCustomerMobile(ticket.CustomerProfileId);
                    if (!string.IsNullOrEmpty(mobile))
                    {
                        var smsMobile = mobile.StartsWith("0") ? "+98" + mobile.Substring(1) : mobile;
                        _ = SmsService.SendAsync(smsMobile, "پاسخ جدیدی برای تیکت شما در سامانه متین پاور ثبت شد. لطفاً وارد سامانه شوید.");
                    }
                }
            }

            return result;
        }

        private static string? GetCustomerMobile(int customerProfileId)
        {
            var real = Repository<CustomersReal>.GetLast(r => r.Id == customerProfileId);
            if (real != null) return real.Mobile;

            var legal = Repository<CustomersLegal>.GetLast(l => l.Id == customerProfileId);
            return legal?.CeoMobile;
        }

        [HttpPut]
        public ExecutionResult SetStatus([FromBody] UpdateTicketStatusRequest request)
        {
            var ticket = Repository<Ticket>.GetLast(i => i.Id == request.TicketId);
            if (ticket == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "تیکت یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                ticket.StatusId = request.StatusId;
                Repository<Ticket>.UpdateItem(ticket);
            });
        }
    }
}
