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
                CreatedAt = PersianDateConverter.ToPersianDate(i.CreatedAt, "yyyy/MM/dd"),
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
            if (string.IsNullOrWhiteSpace(item.AgentFullName))
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "نام کامل نماینده الزامی است.", 400);

            if (string.IsNullOrWhiteSpace(item.AgentMobile))
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "موبایل نماینده الزامی است.", 400);

            if (string.IsNullOrWhiteSpace(item.Password))
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "رمز عبور نماینده الزامی است.", 400);

            var existCustomer = Repository<Models.CustomersLegal>.GetLast(i => i.NationalId == item.NationalId && i.CustomerProfile.IsActive == true);
            if (existCustomer != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شناسه ملی قبلا در سیستم ثبت شده است.", 5000);

            var existUser = Repository<User>.GetLast(i => i.Mobile == item.AgentMobile && i.IsActive == true);
            if (existUser != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شماره موبایل نماینده قبلاً در سیستم ثبت شده است.", 400);

            try
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

                Repository<User>.InsertItem(new User
                {
                    FullName = item.AgentFullName,
                    Mobile = item.AgentMobile,
                    Password = item.Password,
                    IsActive = item.IsActive ?? true,
                    CustomerProfileId = profile.Id,
                });

                return new ExecutionResult(ResultType.Success, "موفق", "", 200, profile.Id.ToString());
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        protected override Models.CustomersLegal GetItem(int id)
        {
            if (id == 0) return new Models.CustomersLegal();
            var item = Repository<Models.CustomersLegal>.GetItemById(id);
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

        protected override Models.CustomersLegal PrepareUpdateItem(Models.CustomersLegal item)
        {
            var duplicate = Repository<Models.CustomersLegal>.GetLast(i => i.NationalId == item.NationalId && i.Id != item.Id && i.CustomerProfile.IsActive == true);
            if (duplicate != null)
                throw new Exception("این شناسه ملی قبلاً در سیستم ثبت شده است.");

            var profile = Repository<CustomerProfile>.GetItemById(item.Id);
            if (profile != null)
            {
                profile.IsActive = item.IsActive ?? profile.IsActive;
                if ((item.FamiliarityType ?? 0) > 0)
                    profile.FamiliarityType = item.FamiliarityType;
                Repository<CustomerProfile>.UpdateItem(profile);

                // کاربر نماینده هم باید هم‌زمان با پروفایل مشتری فعال/غیرفعال شود تا امکان ورود با وضعیت واقعی مغایرت نداشته باشد
                var linkedUser = Repository<User>.GetLast(u => u.CustomerProfileId == item.Id);
                if (linkedUser != null && linkedUser.IsActive != profile.IsActive)
                {
                    linkedUser.IsActive = profile.IsActive;
                    Repository<User>.UpdateItem(linkedUser);
                }
            }

            var existing = Repository<Models.CustomersLegal>.GetItemById(item.Id);
            existing.CompanyName    = item.CompanyName;
            existing.NationalId    = item.NationalId;
            existing.EconomicCode  = item.EconomicCode;
            existing.CeoFullName   = item.CeoFullName;
            existing.CeoMobile     = item.CeoMobile;
            existing.RegisterNumber = item.RegisterNumber;
            existing.CeoNationalId  = item.CeoNationalId;
            existing.GazetteDate    = item.GazetteDate;
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
