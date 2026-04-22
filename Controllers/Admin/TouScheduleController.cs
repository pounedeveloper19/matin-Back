using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Route("[controller]")]
    public class TouScheduleController : BaseController
    {
        [HttpGet("GetMonthSchedule")]
        public ExecutionResult GetMonthSchedule()
        {
            return RunExceptionProof(() =>
            {
                int powerEntityId = UrlArgument<int>("powerEntityId");
                int month         = UrlArgument<int>("month");

                var hours = Repository<Touschedule>
                    .GetListExtended(i => i.PowerEntityId == powerEntityId && i.MonthNumber == month)
                    .Select(i => new { i.HourNumber, i.ToutypeId })
                    .OrderBy(i => i.HourNumber)
                    .ToList();

                return new ExecutionResult(ResultType.Success, null, null, 200, hours);
            });
        }

        [HttpPost("SaveSchedule")]
        public ExecutionResult SaveSchedule([FromBody] SaveScheduleRequest request)
        {
            return RunExceptionProof(() =>
            {
                var existing = Repository<Touschedule>
                    .GetListExtended(i => i.PowerEntityId == request.PowerEntityId && i.MonthNumber == request.Month)
                    .ToList();

                foreach (var item in existing)
                    Repository<Touschedule>.DeleteItem(item);

                foreach (var h in request.Hours)
                {
                    Repository<Touschedule>.InsertItem(new Touschedule
                    {
                        PowerEntityId = request.PowerEntityId,
                        MonthNumber   = request.Month,
                        HourNumber    = h.HourNumber,
                        ToutypeId     = h.ToutypeId,
                    });
                }
            });
        }

        [HttpGet("GetPowerEntities")]
        public ExecutionResult GetPowerEntities()
        {
            return RunExceptionProof(() =>
            {
                var list = Repository<PowerEntity>
                    .GetListExtended(i => i.IsActive == true)
                    .Select(i => new { i.Id, i.Name })
                    .OrderBy(i => i.Name)
                    .ToList();

                return new ExecutionResult(ResultType.Success, null, null, 200, list);
            });
        }

        [HttpGet("GetTouTypes")]
        public ExecutionResult GetTouTypes()
        {
            return RunExceptionProof(() =>
            {
                var list = Repository<EnumToutype>
                    .GetListExtended((System.Linq.Expressions.Expression<Func<EnumToutype, bool>>)null)
                    .Select(i => new { i.Id, i.Title })
                    .ToList();

                return new ExecutionResult(ResultType.Success, null, null, 200, list);
            });
        }
    }

    public class SaveScheduleRequest
    {
        public int PowerEntityId { get; set; }
        public int Month         { get; set; }
        public List<HourEntry> Hours { get; set; } = new();
    }

    public class HourEntry
    {
        public int HourNumber { get; set; }
        public int ToutypeId  { get; set; }
    }
}
