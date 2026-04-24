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
            int powerEntityId = UrlArgument<int>("powerEntityId");
            int month         = UrlArgument<int>("month");

            return RunExceptionProof(() =>
            {
                var hours = Repository<Touschedule>
                    .GetListExtended(i => i.PowerEntityId == powerEntityId && i.MonthNumber == month)
                    .Select(i => new { hourNumber = i.HourNumber, toutypeId = i.ToutypeId })
                    .OrderBy(i => i.hourNumber)
                    .ToList();
                return (object)hours;
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
                    .Select(i => new { id = i.Id, name = i.Name })
                    .OrderBy(i => i.name)
                    .ToList();
                return (object)list;
            });
        }

        [HttpPost("CopyFromMonth")]
        public ExecutionResult CopyFromMonth([FromBody] CopyTouRequest request)
        {
            var sourceHours = Repository<Touschedule>
                .GetListExtended(i => i.PowerEntityId == request.PowerEntityId && i.MonthNumber == request.SourceMonth)
                .ToList();

            if (!sourceHours.Any())
                return new ExecutionResult(ResultType.Warning, "هشدار", "ماه مبدأ برنامه‌ای ندارد.", 400);

            return RunExceptionProof(() =>
            {
                var existing = Repository<Touschedule>
                    .GetListExtended(i => i.PowerEntityId == request.PowerEntityId && i.MonthNumber == request.TargetMonth)
                    .ToList();
                foreach (var item in existing)
                    Repository<Touschedule>.DeleteItem(item);

                foreach (var h in sourceHours)
                {
                    Repository<Touschedule>.InsertItem(new Touschedule
                    {
                        PowerEntityId = request.PowerEntityId,
                        MonthNumber   = request.TargetMonth,
                        HourNumber    = h.HourNumber,
                        ToutypeId     = h.ToutypeId,
                    });
                }
            });
        }

        [HttpGet("GetTouTypes")]
        public ExecutionResult GetTouTypes()
        {
            return RunExceptionProof(() =>
            {
                var list = Repository<EnumToutype>
                    .GetListExtended((System.Linq.Expressions.Expression<Func<EnumToutype, bool>>)null)
                    .Select(i => new { id = i.Id, title = i.Title })
                    .ToList();
                return (object)list;
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

    public class CopyTouRequest
    {
        public int PowerEntityId { get; set; }
        public int SourceMonth   { get; set; }
        public int TargetMonth   { get; set; }
    }
}
