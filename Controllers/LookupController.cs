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
        {
            using var db = DbContextProvider.CreateContext();
            return db.Provinces.OrderBy(p => p.Name).Select(p => new { p.Id, p.Name }).ToList();
        });

    [HttpGet]
    public ExecutionResult GetPowerEntityTypes() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.EnumPowerEntityTypes.Select(t => new { t.Id, t.Title }).ToList();
        });

    [HttpGet]
    public ExecutionResult GetCities() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.Cities
                .OrderBy(c => c.Title)
                .Select(c => new { c.Id, c.Title })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetPowerEntities() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.PowerEntities
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, Province = p.Province.Name })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetGuaranteeTypes() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.EnumGuaranteeTypes
                .Select(t => new { t.Id, t.Title })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetCustomerTypes() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.EnumCustomerTypes
                .Select(t => new { t.Id, t.Title })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetTariffTypes() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.EnumTariffTypes
                .Select(t => new { t.Id, t.Title })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetContractStatuses() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.EnumContractStatuses
                .Select(s => new { s.Id, s.Title })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetAllTariffs() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.Tariffs
                .OrderBy(t => t.TariffId)
                .Select(t => new { t.TariffId, t.TariffTypeId, t.CustomerTypeId, t.PowerEntitiesId })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetAllSubscriptions() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.Subscriptions
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
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetActiveAnnouncements() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            var now = DateTime.Now;
            return db.Announcements
                .Where(a => a.PublishDate <= now && (a.FinishDate == null || a.FinishDate >= now))
                .OrderByDescending(a => a.PublishDate)
                .Select(a => new { a.Id, a.Title, a.Contents, a.PublishDate })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetTariffCodes() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.TariffCodes
                .OrderBy(t => t.Code)
                .Select(t => new { t.Id, t.Code, t.Title })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetTariffCodeOptions(int tariffCodeId) =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.TariffCodeOptions
                .Where(o => o.TariffCodeId == tariffCodeId)
                .OrderBy(o => o.Title)
                .Select(o => new
                {
                    o.Id,
                    o.Title,
                    o.PenaltyMultiplier,
                    o.CreditMultiplier,
                })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetMyMenu() =>
        RunExceptionProof(() =>
        {
            var userId = new UseContext(new HttpContextAccessor()).GetUserId();
            if (userId == null) return (object)new List<object>();

            using var db = DbContextProvider.CreateContext();

            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return (object)new List<object>();

            bool isAdmin   = !user.CustomerProfileId.HasValue;
            int rootParent = isAdmin ? 10 : 1;

            // همه آیتم‌های منویی سیستم (صرفاً IsInMenu=true)
            var all = db.SiteMaps
                .Where(s => s.IsInMenu)
                .Select(s => new
                {
                    s.Id, s.Title, s.PhysicalPath, s.Icon,
                    s.ParentId, s.Indexer, s.IsSelectable,
                })
                .ToList();

            // تعیین مجموعه آیتم‌های مجاز بر اساس نقش
            HashSet<int>? allowed = null;
            if (isAdmin)
            {
                var ur = db.UserRoles.FirstOrDefault(x => x.UserId == userId);
                if (ur != null)
                {
                    var ids = db.SiteMapRoles
                        .Where(sr => sr.RoleId == ur.RoleId)
                        .Select(sr => sr.SiteMapId)
                        .ToList();
                    allowed = new HashSet<int>(ids);
                }
            }

            // ساخت درخت به‌صورت بازگشتی
            List<object> BuildTree(int parentId)
            {
                var result = new List<object>();
                var children = all.Where(s => s.ParentId == parentId)
                                  .OrderBy(s => s.Indexer)
                                  .ToList();

                foreach (var item in children)
                {
                    var sub = BuildTree(item.Id);

                    if (item.IsSelectable)
                    {
                        // صفحه‌ی واقعی: اگه مجوز داره یا محدودیتی نیست
                        if (allowed == null || allowed.Contains(item.Id))
                            result.Add(new { item.Id, item.Title, Path = item.PhysicalPath, item.Icon, item.IsSelectable, Children = sub });
                    }
                    else
                    {
                        // گروه: نشون بده فقط اگه حداقل یه فرزند مجاز داره
                        if (sub.Any())
                            result.Add(new { item.Id, item.Title, Path = item.PhysicalPath, item.Icon, item.IsSelectable, Children = sub });
                    }
                }
                return result;
            }

            return BuildTree(rootParent);
        });

    [HttpGet]
    public ExecutionResult GetMyPermissions() =>
        RunExceptionProof(() =>
        {
            var userId = new UseContext(new HttpContextAccessor()).GetUserId();
            if (userId == null) return (object)new List<string>();

            using var db = DbContextProvider.CreateContext();
            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return (object)new List<string>();

            bool isAdmin = !user.CustomerProfileId.HasValue;
            if (!isAdmin) return (object)new List<string>(); // مشتری نیازی به ControlKey ندارد

            var ur = db.UserRoles.FirstOrDefault(x => x.UserId == userId);
            if (ur == null)
            {
                // سوپر ادمین (بدون نقش): همه ControlKey ها
                return db.SiteMaps
                    .Where(s => s.ControlKey != null)
                    .Select(s => s.ControlKey!)
                    .ToList();
            }

            // ادمین با نقش: فقط ControlKey های تعریف‌شده در SiteMapRole
            return db.SiteMapRoles
                .Where(sr => sr.RoleId == ur.RoleId && sr.SiteMap.ControlKey != null)
                .Select(sr => sr.SiteMap.ControlKey!)
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetEnergyTypes() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.EnumEnergyTypes.Select(t => new { t.Id, t.Title }).ToList();
        });

    [HttpGet]
    public ExecutionResult GetPaymentMethods() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.EnumPaymentMethods.Select(t => new { t.Id, t.Title }).ToList();
        });

    [HttpGet]
    public ExecutionResult GetOrderStatuses() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.EnumOrderStatuses.Select(t => new { t.Id, t.Title }).ToList();
        });

    [HttpGet]
    public ExecutionResult GetPaymentStatuses() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.EnumPaymentStatuses.Select(t => new { t.Id, t.Title }).ToList();
        });

    [HttpGet]
    public ExecutionResult GetRoles() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.Roles
                .OrderBy(r => r.Id)
                .Select(r => new { r.Id, r.Title, r.Description })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetSiteMaps() =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            return db.SiteMaps
                .OrderBy(s => s.ParentId).ThenBy(s => s.Indexer)
                .Select(s => new { s.Id, s.Title, s.ParentId, s.IsInMenu, s.IsSelectable, s.Description, s.ControlKey })
                .ToList();
        });

    [HttpGet]
    public ExecutionResult GetCustomerFullDetail(int profileId) =>
        RunExceptionProof(() =>
        {
            using var db = DbContextProvider.CreateContext();
            var profile = db.CustomerProfiles.FirstOrDefault(p => p.Id == profileId);
            var addresses = db.Addresses
                .Where(a => a.CustomerProfileId == profileId)
                .Select(a => new
                {
                    a.Id,
                    a.MainAddress,
                    a.PostalCode,
                    City    = a.City.Title,
                    Province = a.City.Province.Name,
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
            return new
            {
                IdentityDocFileId = profile != null && profile.IdentityDocFileId.HasValue
                    ? profile.IdentityDocFileId.ToString()
                    : (string?)null,
                Addresses     = addresses,
                Subscriptions = subscriptions,
            };
        });
}
