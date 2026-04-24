using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
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
                    i.CreatedAt,
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
                    i.CreatedAt,
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

            return RunExceptionProof(() =>
            {
                Repository<TicketMessage>.InsertItem(new TicketMessage
                {
                    TicketId = request.TicketId,
                    Body = request.Body,
                    FileId = request.FileId,
                    SenderUserId = userId.Value,
                    CreatedAt = DateTime.Now,
                });
            });
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
