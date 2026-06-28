using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Customer
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TooltipController : BaseController
    {
        [HttpGet]
        public ExecutionResult GetByPage([FromQuery] string pageKey) =>
            RunExceptionProof(() => Repository<PageTooltip>.GetSelectiveList(
                t => new { t.Id, t.FieldKey, t.Title, t.Content },
                t => t.PageKey == pageKey && t.IsActive));
    }
}
