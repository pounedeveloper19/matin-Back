using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
