using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AdminBillAnalysisController : BaseController
    {
        /// <summary>لیست اشتراک‌های یک مشتری برای ادمین</summary>
        [HttpGet("{profileId}")]
        public ExecutionResult GetSubscriptions(int profileId)
        {
            return RunExceptionProof(() =>
            {
                var subs = Repository<Subscription>.GetSelectiveList(
                    s => new
                    {
                        s.Id,
                        s.BillIdentifier,
                        s.ContractCapacityKw,
                        MainAddress   = s.Address.MainAddress,
                        PowerEntity   = s.Address.PowerEntity != null ? s.Address.PowerEntity.Name : "",
                        PowerEntityId = (int?)s.Address.PowerEntityId,
                        HasTariff     = s.Address.CustomerProfile.TariffCodeOptionId != null,
                    },
                    s => s.Address.CustomerProfileId == profileId,
                    includes: new[] { "Address", "Address.PowerEntity", "Address.CustomerProfile" }
                );
                return (object)subs;
            });
        }
    }
}
