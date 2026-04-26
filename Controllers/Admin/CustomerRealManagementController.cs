using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class CustomerRealManagementController : BaseManageController<Models.CustomersReal>
    {
        protected override PaginationResult GridDataSource(Expression<Func<Models.CustomersReal, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<Models.CustomersReal>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.NationalCode,
                i.FirstName,
                i.LastName,
                i.Mobile,
                i.CustomerProfile.IsActive,
                i.CustomerProfile.FamiliarityType,
                i.CustomerProfile.CustomerTypeId,
            }, filter, predicate, sortExpression: "CreatedAt", sortDirection: System.Web.Helpers.SortDirection.Descending, includes: new[] { "CustomerProfile" });

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<CustomersReal, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<Models.CustomersReal, bool>> result = i => true;
                result = result.AppendCondition(s => s.CustomerProfile.CustomerTypeId == 1, false);
                string? name = UrlArgument<string?>("Search_Name");
                if (!string.IsNullOrEmpty(name))
                    result = result.AppendCondition(s => s.FirstName.Contains(name) || s.LastName.Contains(name), false);
                string? nationalCode = UrlArgument<string?>("Search_NationalCode");
                if (!string.IsNullOrEmpty(nationalCode))
                    result = result.AppendCondition(s => s.NationalCode.Contains(nationalCode), false);
                string? mobile = UrlArgument<string?>("Search_Mobile");
                if (!string.IsNullOrEmpty(mobile))
                    result = result.AppendCondition(s => s.Mobile.Contains(mobile), false);
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
        public override ExecutionResult Insert([FromBody] Models.CustomersReal item)
        {
            var existCustomer = Repository<Models.CustomersReal>.GetLast(i => i.NationalCode == item.NationalCode);
            if (existCustomer != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این کد ملی قبلا در سیستم ثبت شده است.", 5000);

            return RunExceptionProof(() =>
            {
                var profile = Repository<CustomerProfile>.InsertItem(new CustomerProfile
                {
                    CustomerTypeId = 1,
                    IsActive = item.IsActive ?? true,
                    FamiliarityType = (item.FamiliarityType ?? 0) > 0 ? item.FamiliarityType : null,
                });

                item.Id = profile.Id;
                item.CreatedAt = DateTime.Now;
                Repository<Models.CustomersReal>.InsertItem(item);
                return (object)profile.Id.ToString();
            });
        }

        protected override Models.CustomersReal PrepareUpdateItem(Models.CustomersReal item)
        {
            var duplicate = Repository<Models.CustomersReal>.GetLast(i => i.NationalCode == item.NationalCode && i.Id != item.Id);
            if (duplicate != null)
                throw new Exception("این کد ملی قبلاً در سیستم ثبت شده است.");

            var profile = Repository<CustomerProfile>.GetItemById(item.Id);
            if (profile != null)
            {
                profile.IsActive = item.IsActive ?? profile.IsActive;
                if ((item.FamiliarityType ?? 0) > 0)
                    profile.FamiliarityType = item.FamiliarityType;
                Repository<CustomerProfile>.UpdateItem(profile);
            }

            var existing = Repository<Models.CustomersReal>.GetItemById(item.Id);
            existing.FirstName = item.FirstName;
            existing.LastName = item.LastName;
            existing.NationalCode = item.NationalCode;
            existing.Mobile = item.Mobile;
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
                var entity = Repository<Models.CustomersReal>.GetItemById(id);
                if (entity != null)
                    Repository<Models.CustomersReal>.DeleteItem(entity);

                var profile = Repository<CustomerProfile>.GetItemById(id);
                if (profile != null)
                    Repository<CustomerProfile>.DeleteItem(profile);
            });
        }
    }
}
