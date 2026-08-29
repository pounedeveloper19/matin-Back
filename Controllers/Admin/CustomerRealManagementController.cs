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
                CreatedAt = PersianDateConverter.ToPersianDate(i.CreatedAt, "yyyy/MM/dd"),
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
            if (string.IsNullOrWhiteSpace(item.Password))
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "رمز عبور الزامی است.", 400);

            var existCustomer = Repository<Models.CustomersReal>.GetLast(i => i.NationalCode == item.NationalCode && i.CustomerProfile.IsActive == true);
            if (existCustomer != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این کد ملی قبلا در سیستم ثبت شده است.", 5000);

            var existUser = Repository<User>.GetLast(i => i.Mobile == item.Mobile && i.IsActive == true);
            if (existUser != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شماره موبایل قبلاً در سیستم ثبت شده است.", 400);

            try
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

                Repository<User>.InsertItem(new User
                {
                    FullName = $"{item.FirstName} {item.LastName}",
                    Mobile = item.Mobile,
                    Password = item.Password,
                    IsActive = item.IsActive ?? true,
                    CustomerProfileId = profile.Id,
                });

                return new ExecutionResult(ResultType.Success, "موفق", "", 200, profile.Id.ToString());
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        protected override Models.CustomersReal GetItem(int id)
        {
            if (id == 0) return new Models.CustomersReal();
            var item = Repository<Models.CustomersReal>.GetItemById(id);
            if (item != null)
            {
                var profile = Repository<CustomerProfile>.GetItemById(id);
                if (profile != null)
                {
                    item.IsActive = profile.IsActive;
                    item.FamiliarityType = profile.FamiliarityType;
                    item.CustomerTypeId = profile.CustomerTypeId;
                }
            }
            return item;
        }

        protected override Models.CustomersReal PrepareUpdateItem(Models.CustomersReal item)
        {
            var duplicate = Repository<Models.CustomersReal>.GetLast(i => i.NationalCode == item.NationalCode && i.Id != item.Id && i.CustomerProfile.IsActive == true);
            if (duplicate != null)
                throw new Exception("این کد ملی قبلاً در سیستم ثبت شده است.");

            var profile = Repository<CustomerProfile>.GetItemById(item.Id);
            if (profile != null)
            {
                profile.IsActive = item.IsActive ?? profile.IsActive;
                if ((item.FamiliarityType ?? 0) > 0)
                    profile.FamiliarityType = item.FamiliarityType;
                Repository<CustomerProfile>.UpdateItem(profile);

                // کاربر مشتری هم باید هم‌زمان با پروفایل فعال/غیرفعال شود تا امکان ورود با وضعیت واقعی مغایرت نداشته باشد
                var linkedUser = Repository<User>.GetLast(u => u.CustomerProfileId == item.Id);
                if (linkedUser != null && linkedUser.IsActive != profile.IsActive)
                {
                    linkedUser.IsActive = profile.IsActive;
                    Repository<User>.UpdateItem(linkedUser);
                }
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
            return RunExceptionProof(() =>
            {
                var profile = Repository<CustomerProfile>.GetItemById(id);
                if (profile != null)
                {
                    profile.IsActive = false;
                    Repository<CustomerProfile>.UpdateItem(profile);
                }

                var linkedUser = Repository<User>.GetLast(u => u.CustomerProfileId == id);
                if (linkedUser != null)
                {
                    linkedUser.IsActive = false;
                    Repository<User>.UpdateItem(linkedUser);
                }
            });
        }
    }
}
