using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class CustomerLegalManagementController : BaseManageController<Models.CustomersLegal>
    {
        protected override PaginationResult GridDataSource(Expression<Func<Models.CustomersLegal, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<Models.CustomersLegal>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.NationalId,
                i.CeoFullName,
                i.CompanyName,
                i.CeoMobile,
                i.CustomerProfile.IsActive,
                i.CustomerProfile.FamiliarityType,
                i.CustomerProfile.CustomerTypeId,
            }, filter, predicate, sortExpression: "CreatedAt", sortDirection: System.Web.Helpers.SortDirection.Descending, includes: new[] { "CustomerProfile" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<CustomersLegal, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<Models.CustomersLegal, bool>> result = i => true;
                string? companyName = UrlArgument<string?>("Search_CompanyName");
                if (!string.IsNullOrEmpty(companyName))
                    result = result.AppendCondition(s => s.CompanyName.Contains(companyName), false);
                string? nationalId = UrlArgument<string?>("Search_NationalId");
                if (!string.IsNullOrEmpty(nationalId))
                    result = result.AppendCondition(s => s.NationalId.Contains(nationalId), false);
                string? ceoFullName = UrlArgument<string?>("Search_CeoFullName");
                if (!string.IsNullOrEmpty(ceoFullName))
                    result = result.AppendCondition(s => s.CeoFullName.Contains(ceoFullName), false);
                string? isActive = UrlArgument<string?>("Search_IsActive");
                if (isActive == "true")
                    result = result.AppendCondition(s => s.CustomerProfile.IsActive == true, false);
                else if (isActive == "false")
                    result = result.AppendCondition(s => s.CustomerProfile.IsActive != true, false);
                return result;
            }
        }

        [Route("[controller]/Insert")]
        [HttpPost]
        public override ExecutionResult Insert([FromBody] Models.CustomersLegal item)
        {
            var existCustomer = Repository<Models.CustomersLegal>.GetLast(i => i.NationalId == item.NationalId);
            if (existCustomer != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شناسه ملی قبلا در سیستم ثبت شده است.", 5000);

            return RunExceptionProof(() =>
            {
                var profile = Repository<CustomerProfile>.InsertItem(new CustomerProfile
                {
                    CustomerTypeId = 2,
                    IsActive = item.IsActive ?? true,
                    FamiliarityType = (item.FamiliarityType ?? 0) > 0 ? item.FamiliarityType : null,
                });

                item.Id = profile.Id;
                item.CreatedAt = DateTime.Now;
                Repository<Models.CustomersLegal>.InsertItem(item);
                return (object)profile.Id.ToString();
            });
        }

        protected override Models.CustomersLegal PrepareUpdateItem(Models.CustomersLegal item)
        {
            var duplicate = Repository<Models.CustomersLegal>.GetLast(i => i.NationalId == item.NationalId && i.Id != item.Id);
            if (duplicate != null)
                throw new Exception("این شناسه ملی قبلاً در سیستم ثبت شده است.");

            var profile = Repository<CustomerProfile>.GetItemById(item.Id);
            if (profile != null)
            {
                profile.IsActive = item.IsActive ?? profile.IsActive;
                if ((item.FamiliarityType ?? 0) > 0)
                    profile.FamiliarityType = item.FamiliarityType;
                Repository<CustomerProfile>.UpdateItem(profile);
            }

            var existing = Repository<Models.CustomersLegal>.GetItemById(item.Id);
            existing.CompanyName = item.CompanyName;
            existing.NationalId = item.NationalId;
            existing.EconomicCode = item.EconomicCode;
            existing.CeoFullName = item.CeoFullName;
            existing.CeoMobile = item.CeoMobile;
            return existing;
        }

        [Route("[controller]/Delete/{id}")]
        [HttpDelete]
        public override ExecutionResult Delete(int id)
        {
            var hasAddress = Repository<Address>.GetLast(i => i.CustomerProfileId == id) != null;
            if (hasAddress)
                return new ExecutionResult(ResultType.Danger, "خطا", "این مشتری دارای آدرس یا انشعاب فعال است. ابتدا اطلاعات وابسته را حذف کنید.", 400);

            return RunExceptionProof(() =>
            {
                var entity = Repository<Models.CustomersLegal>.GetItemById(id);
                if (entity != null)
                    Repository<Models.CustomersLegal>.DeleteItem(entity);

                var profile = Repository<CustomerProfile>.GetItemById(id);
                if (profile != null)
                    Repository<CustomerProfile>.DeleteItem(profile);
            });
        }
    }
}
