using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers;

[Authorize]
[Route("[controller]/[action]")]
public class LookupController : BaseController
{
    [HttpGet]
    public ExecutionResult GetProvinces() =>
        RunExceptionProof(() =>
            Repository<Province>.Query(db =>
                db.Provinces.OrderBy(p => p.Name).Select(p => new { p.Id, p.Name }).ToList()));

    [HttpGet]
    public ExecutionResult GetPowerEntityTypes() =>
        RunExceptionProof(() =>
            Repository<EnumPowerEntityType>.Query(db =>
                db.EnumPowerEntityTypes.Select(t => new { t.Id, t.Title }).ToList()));

    [HttpGet]
    public ExecutionResult GetCities() =>
        RunExceptionProof(() =>
            Repository<City>.Query(db =>
                db.Cities.OrderBy(c => c.Title).Select(c => new { c.Id, c.Title }).ToList()));

    [HttpGet]
    public ExecutionResult GetPowerEntities() =>
        RunExceptionProof(() =>
            Repository<PowerEntity>.Query(db =>
                db.PowerEntities
                    .Where(p => p.IsActive == true)
                    .OrderBy(p => p.Name)
                    .Select(p => new { p.Id, p.Name, Province = p.Province.Name })
                    .ToList()));

    [HttpGet]
    public ExecutionResult GetGuaranteeTypes() =>
        RunExceptionProof(() =>
            Repository<EnumGuaranteeType>.Query(db =>
                db.EnumGuaranteeTypes.Select(t => new { t.Id, t.Title }).ToList()));

    [HttpGet]
    public ExecutionResult GetCustomerTypes() =>
        RunExceptionProof(() =>
            Repository<EnumCustomerType>.Query(db =>
                db.EnumCustomerTypes.Select(t => new { t.Id, t.Title }).ToList()));

    [HttpGet]
    public ExecutionResult GetTariffTypes() =>
        RunExceptionProof(() =>
            Repository<EnumTariffType>.Query(db =>
                db.EnumTariffTypes.Select(t => new { t.Id, t.Title }).ToList()));

    [HttpGet]
    public ExecutionResult GetContractStatuses() =>
        RunExceptionProof(() =>
            Repository<EnumContractStatus>.Query(db =>
                db.EnumContractStatuses.Select(s => new { s.Id, s.Title }).ToList()));

    [HttpGet]
    public ExecutionResult GetAllTariffs() =>
        RunExceptionProof(() =>
            Repository<Tariff>.Query(db =>
                db.Tariffs
                    .OrderBy(t => t.TariffId)
                    .Select(t => new { t.TariffId, t.TariffTypeId, t.CustomerTypeId, t.PowerEntitiesId })
                    .ToList()));

    [HttpGet]
    public ExecutionResult GetAllSubscriptions() =>
        RunExceptionProof(() =>
            Repository<Subscription>.Query(db =>
                db.Subscriptions
                    .OrderBy(s => s.BillIdentifier)
                    .Select(s => new
                    {
                        s.Id,
                        s.BillIdentifier,
                        Address = s.Address.MainAddress ?? "",
                        CustomerName = s.Address.CustomerProfile.CustomersReal != null
                            ? s.Address.CustomerProfile.CustomersReal.FirstName + " " + s.Address.CustomerProfile.CustomersReal.LastName
                            : s.Address.CustomerProfile.CustomersLegal != null
                                ? s.Address.CustomerProfile.CustomersLegal.CompanyName ?? ""
                                : "",
                    })
                    .ToList()));

    [HttpGet]
    public ExecutionResult GetActiveAnnouncements() =>
        RunExceptionProof(() =>
        {
            var now = DateTime.Now;
            return Repository<Announcement>.Query(db =>
                db.Announcements
                    .Where(a => a.PublishDate <= now && (a.FinishDate == null || a.FinishDate >= now))
                    .OrderByDescending(a => a.PublishDate)
                    .Select(a => new { a.Id, a.Title, a.Contents, a.PublishDate })
                    .ToList());
        });

    [HttpGet]
    public ExecutionResult GetTariffCodes() =>
        RunExceptionProof(() =>
            Repository<TariffCode>.Query(db =>
                db.TariffCodes
                    .OrderBy(t => t.Code)
                    .Select(t => new { t.Id, t.Code, t.Title })
                    .ToList()));

    [HttpGet]
    public ExecutionResult GetTariffCodeOptions(int tariffCodeId) =>
        RunExceptionProof(() =>
            Repository<TariffCodeOption>.Query(db =>
                db.TariffCodeOptions
                    .Where(o => o.TariffCodeId == tariffCodeId)
                    .OrderBy(o => o.Title)
                    .Select(o => new { o.Id, o.Title, o.PenaltyMultiplier, o.CreditMultiplier })
                    .ToList()));

    [HttpGet]
    public ExecutionResult GetMyMenu() =>
        RunExceptionProof(() =>
        {
            var userId = new UseContext(new HttpContextAccessor()).GetUserId();
            if (userId == null) return (object)new List<object>();

            return Repository<User>.Query(db =>
            {
                var user = db.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null) return (object)new List<object>();

                bool isAdmin   = !user.CustomerProfileId.HasValue;
                int rootParent = isAdmin ? 10 : 1;

                var all = db.SiteMaps
                    .Where(s => s.IsInMenu)
                    .Select(s => new
                    {
                        s.Id, s.Title, s.PhysicalPath, s.Icon,
                        s.ParentId, s.Indexer, s.IsSelectable,
                    })
                    .ToList();

                HashSet<int>? allowed = null;
                var ur = db.UserRoles.FirstOrDefault(x => x.UserId == userId);
                if (ur != null)
                {
                    var ids = db.SiteMapRoles
                        .Where(sr => sr.RoleId == ur.RoleId)
                        .Select(sr => sr.SiteMapId)
                        .ToList();
                    if (ids.Count > 0)
                        allowed = new HashSet<int>(ids);
                }

                List<object> BuildTree(int parentId)
                {
                    var result = new List<object>();
                    var children = all.Where(s => s.ParentId == parentId).OrderBy(s => s.Indexer).ToList();
                    foreach (var item in children)
                    {
                        var sub = BuildTree(item.Id);
                        if (item.IsSelectable)
                        {
                            if (allowed == null || allowed.Contains(item.Id))
                                result.Add(new { item.Id, item.Title, Path = item.PhysicalPath, item.Icon, item.IsSelectable, Children = sub });
                        }
                        else
                        {
                            if (sub.Any())
                                result.Add(new { item.Id, item.Title, Path = item.PhysicalPath, item.Icon, item.IsSelectable, Children = sub });
                        }
                    }
                    return result;
                }

                return (object)BuildTree(rootParent);
            });
        });

    [HttpGet]
    public ExecutionResult GetMyPermissions() =>
        RunExceptionProof(() =>
        {
            var userId = new UseContext(new HttpContextAccessor()).GetUserId();
            if (userId == null) return (object)new List<string>();

            return Repository<User>.Query(db =>
            {
                var user = db.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null) return (object)new List<string>();

                bool isAdmin = !user.CustomerProfileId.HasValue;
                if (!isAdmin) return (object)new List<string>();

                var ur = db.UserRoles.FirstOrDefault(x => x.UserId == userId);
                if (ur == null)
                {
                    return (object)db.SiteMaps
                        .Where(s => s.ControlKey != null)
                        .Select(s => s.ControlKey!)
                        .ToList();
                }

                return (object)db.SiteMapRoles
                    .Where(sr => sr.RoleId == ur.RoleId && sr.SiteMap.ControlKey != null)
                    .Select(sr => sr.SiteMap.ControlKey!)
                    .ToList();
            });
        });

    [HttpGet]
    public ExecutionResult GetEnergyTypes() =>
        RunExceptionProof(() =>
            Repository<EnumEnergyType>.Query(db =>
                db.EnumEnergyTypes.Select(t => new { t.Id, t.Title }).ToList()));

    [HttpGet]
    public ExecutionResult GetPaymentMethods() =>
        RunExceptionProof(() =>
            Repository<EnumPaymentMethod>.Query(db =>
                db.EnumPaymentMethods.Select(t => new { t.Id, t.Title }).ToList()));

    [HttpGet]
    public ExecutionResult GetOrderStatuses() =>
        RunExceptionProof(() =>
            Repository<EnumOrderStatus>.Query(db =>
                db.EnumOrderStatuses.Select(t => new { t.Id, t.Title }).ToList()));

    [HttpGet]
    public ExecutionResult GetPaymentStatuses() =>
        RunExceptionProof(() =>
            Repository<EnumPaymentStatus>.Query(db =>
                db.EnumPaymentStatuses.Select(t => new { t.Id, t.Title }).ToList()));

    [HttpGet]
    public ExecutionResult GetRoles() =>
        RunExceptionProof(() =>
            Repository<Role>.Query(db =>
                db.Roles
                    .OrderBy(r => r.Id)
                    .Select(r => new { r.Id, r.Title, r.Description })
                    .ToList()));

    [HttpGet]
    public ExecutionResult GetSiteMaps() =>
        RunExceptionProof(() =>
            Repository<SiteMap>.Query(db =>
                db.SiteMaps
                    .OrderBy(s => s.ParentId).ThenBy(s => s.Indexer)
                    .Select(s => new { s.Id, s.Title, s.ParentId, s.IsInMenu, s.IsSelectable, s.Description, s.ControlKey })
                    .ToList()));

    [HttpGet]
    public ExecutionResult GetCustomerFullDetail(int profileId) =>
        RunExceptionProof(() =>
            Repository<CustomerProfile>.Query(db =>
            {
                var profile = db.CustomerProfiles.FirstOrDefault(p => p.Id == profileId);
                var addresses = db.Addresses
                    .Where(a => a.CustomerProfileId == profileId)
                    .Select(a => new
                    {
                        a.Id,
                        a.MainAddress,
                        a.PostalCode,
                        City        = a.City.Title,
                        Province    = a.City.Province.Name,
                        PowerEntity = a.PowerEntity.Name,
                    }).ToList();
                var subscriptions = db.Subscriptions
                    .Where(s => s.Address.CustomerProfileId == profileId)
                    .Select(s => new
                    {
                        s.Id,
                        s.BillIdentifier,
                        s.ContractCapacityKw,
                        MainAddress = s.Address.MainAddress ?? "",
                    }).ToList();
                return (object)new
                {
                    Addresses     = addresses,
                    Subscriptions = subscriptions,
                };
            }));
}
