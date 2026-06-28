using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Authorize]
    [ApiController]
    [Route("[controller]/[action]")]
    public class AdminTooltipController : BaseController
    {
        [HttpGet]
        public ExecutionResult GetList() =>
            RunExceptionProof(() =>
                Repository<PageTooltip>.Query(db =>
                    (object)db.PageTooltips.OrderByDescending(t => t.Id).ToList()));

        [HttpPost]
        public ExecutionResult Insert([FromBody] PageTooltip item)
        {
            item.CreatedAt = DateTime.Now;
            item.UpdatedAt = null;
            return RunExceptionProof(() => Repository<PageTooltip>.InsertItem(item));
        }

        [HttpPut]
        public ExecutionResult Update([FromBody] PageTooltip item)
        {
            var existing = Repository<PageTooltip>.GetLast(t => t.Id == item.Id);
            if (existing == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "راهنما یافت نشد.", 404);

            existing.PageKey   = item.PageKey;
            existing.FieldKey  = item.FieldKey;
            existing.Title     = item.Title;
            existing.Content   = item.Content;
            existing.IsActive  = item.IsActive;
            existing.UpdatedAt = DateTime.Now;
            return RunExceptionProof(() => Repository<PageTooltip>.UpdateItem(existing));
        }

        [HttpDelete("{id}")]
        public ExecutionResult Delete(int id)
        {
            var item = Repository<PageTooltip>.GetLast(t => t.Id == id);
            if (item == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "راهنما یافت نشد.", 404);
            return RunExceptionProof(() => Repository<PageTooltip>.DeleteItem(item));
        }
    }
}
