using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using MatinPower.Server.Services;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MatinPower.Server.Controllers.Customer
{
    public class CustomerProfileController : BaseController
    {
        private int? GetCustomerProfileId()
        {
            var userId = new UseContext(new HttpContextAccessor()).GetUserId();
            if (userId == null) return null;
            var user = Repository<User>.GetLast(i => i.Id == userId.Value);
            return user?.CustomerProfileId;
        }

        [HttpPost]
        [Route("[controller]/CustomerAthorization")]
        public ExecutionResult CustomerAthorization()
        {
            return null;
        }

        [HttpPost]
        [Route("[controller]/RegisterLegalCustomer")]
        public ExecutionResult RegisterLegalCustomer([FromBody] CustomerLegal customer)
        {
            var existCustomer = Repository<Models.CustomersLegal>.GetLast(i => i.NationalId == customer.NationalId);
            if (existCustomer != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این شناسه ملی قبلا در سیستم ثبت شده است.", 5000);

            return RunExceptionProof(() =>
            {
                var profile = Repository<CustomerProfile>.InsertItem(new CustomerProfile
                {
                    CustomerTypeId = 2,
                    IsActive = true,
                    FamiliarityType = customer.FamiliarityType > 0 ? customer.FamiliarityType : (int?)null,
                });

                Repository<Models.CustomersLegal>.InsertItem(new Models.CustomersLegal
                {
                    Id = profile.Id,
                    NationalId = customer.NationalId,
                    CompanyName = customer.CompanyName,
                    EconomicCode = customer.EconomicCode,
                    CeoFullName = customer.CEO_FullName,
                    CeoMobile = customer.CEO_Mobile,
                    CreatedAt = DateTime.Now,
                });

                var userId = new UseContext(new HttpContextAccessor()).GetUserId();
                if (userId != null)
                {
                    var user = Repository<User>.GetLast(i => i.Id == userId.Value);
                    if (user != null && user.CustomerProfileId == null)
                    {
                        user.CustomerProfileId = profile.Id;
                        Repository<User>.UpdateItem(user);
                    }
                }

                return (object)profile.Id;
            });
        }

        [HttpPost]
        [Route("[controller]/RegisterRealCustomer")]
        public ExecutionResult RegisterRealCustomer([FromBody] CustomerReal customer)
        {
            var existCustomer = Repository<CustomersReal>.GetLast(i => i.NationalCode == customer.NationalCode);
            if (existCustomer != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این کد ملی قبلا در سیستم ثبت شده است.", 5000);

            return RunExceptionProof(() =>
            {
                var profile = Repository<CustomerProfile>.InsertItem(new CustomerProfile
                {
                    CustomerTypeId = 1,
                    IsActive = true,
                    FamiliarityType = customer.FamiliarityType > 0 ? customer.FamiliarityType : (int?)null,
                });

                Repository<CustomersReal>.InsertItem(new CustomersReal
                {
                    Id = profile.Id,
                    NationalCode = customer.NationalCode,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Mobile = customer.Mobile,
                    CreatedAt = DateTime.Now,
                });

                var userId = new UseContext(new HttpContextAccessor()).GetUserId();
                if (userId != null)
                {
                    var user = Repository<User>.GetLast(i => i.Id == userId.Value);
                    if (user != null && user.CustomerProfileId == null)
                    {
                        user.CustomerProfileId = profile.Id;
                        Repository<User>.UpdateItem(user);
                    }
                }

                return (object)profile.Id;
            });
        }

        [HttpPost]
        [Route("[controller]/AddAddress")]
        public ExecutionResult AddAddress([FromBody] AddAddress address)
        {
            var existAddress = Repository<Address>.GetLast(i => i.PostalCode == address.PostalCode);
            if (existAddress != null)
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این کد پستی قبلا در سیستم ثبت شده است.", 5000);

            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            return RunExceptionProof(() =>
            {
                var result = Repository<Address>.InsertItem(new Address
                {
                    CustomerProfileId = customerId.Value,
                    PostalCode = address.PostalCode,
                    CityId = address.CityId,
                    PowerEntityId = address.PowerEntityId,
                    MainAddress = address.MainAddress
                });
                return (object)result;
            });
        }

        [HttpGet]
        [Route("[controller]/GetLegalCustomer")]
        public ExecutionResult GetCustomer()
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var customerType = new UseContext(new HttpContextAccessor()).GetCustomerType();
            if (customerType == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "پروفایل مشتری یافت نشد.", 404);

            var profile = Repository<CustomerProfile>.GetListExtended(i => i.Id == customerId.Value,
                includes: new[] { "TariffCodeOption", "TariffCodeOption.TariffCode" }).LastOrDefault();
            var tariffInfo = profile?.TariffCodeOption == null ? null : new
            {
                tariffCodeOptionId    = profile.TariffCodeOption.Id,
                tariffCodeId          = profile.TariffCodeOption.TariffCodeId,
                tariffCodeTitle       = profile.TariffCodeOption.TariffCode?.Title,
                tariffCodeOptionTitle = profile.TariffCodeOption.Title,
            };

            if (customerType == 1)
            {
                var real = Repository<CustomersReal>.GetLast(i => i.Id == customerId.Value);
                if (real == null)
                    return new ExecutionResult(ResultType.Danger, "خطا", "اطلاعات مشتری حقیقی یافت نشد.", 404);
                return new ExecutionResult(ResultType.Success, null, null, 200, new
                {
                    type = "real",
                    firstName = real.FirstName,
                    lastName = real.LastName,
                    nationalCode = real.NationalCode,
                    mobile = real.Mobile,
                    tariff = tariffInfo,
                });
            }

            if (customerType == 2)
            {
                var legal = Repository<CustomersLegal>.GetLast(i => i.Id == customerId.Value);
                if (legal == null)
                    return new ExecutionResult(ResultType.Danger, "خطا", "اطلاعات مشتری حقوقی یافت نشد.", 404);
                return new ExecutionResult(ResultType.Success, null, null, 200, new
                {
                    type = "legal",
                    companyName    = legal.CompanyName,
                    nationalId     = legal.NationalId,
                    economicCode   = legal.EconomicCode,
                    ceo_FullName   = legal.CeoFullName,
                    ceo_Mobile     = legal.CeoMobile,
                    registerNumber = legal.RegisterNumber,
                    ceoNationalId  = legal.CeoNationalId,
                    gazetteDate    = legal.GazetteDate.HasValue
                                     ? PersianDateConverter.ToPersianDate(legal.GazetteDate, "yyyy/MM/dd")
                                     : null,
                    tariff = tariffInfo,
                });
            }

            return new ExecutionResult(ResultType.Danger, "خطا", "نوع مشتری نامعتبر است.", 400);
        }

        [HttpPut]
        [Route("[controller]/UpdateTariffCode")]
        public ExecutionResult UpdateTariffCode([FromBody] UpdateTariffCodeRequest request)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            return RunExceptionProof(() =>
            {
                var profile = Repository<CustomerProfile>.GetLast(i => i.Id == customerId.Value);
                if (profile == null)
                    return;
                profile.TariffCodeOptionId = request.TariffCodeOptionId;
                Repository<CustomerProfile>.UpdateItem(profile);

                // ثبت تاریخچه: تعرفه از این ماه/سال شمسی به بعد همین مقدار است.
                // اگر همین ماه قبلاً یک انتخاب داشته، جایگزین می‌شود (نه رکورد جدید)
                // تا تحلیل ماه‌های قبلی همچنان با تعرفه‌ی واقعاً معتبر آن‌ها انجام شود.
                if (request.TariffCodeOptionId.HasValue)
                {
                    var pc = new System.Globalization.PersianCalendar();
                    var now = DateTime.Now;
                    int year = pc.GetYear(now), month = pc.GetMonth(now);

                    var existing = Repository<CustomerTariffOptionHistory>.GetLast(h =>
                        h.CustomerProfileId == customerId.Value && h.EffectiveYear == year && h.EffectiveMonth == month);

                    if (existing != null)
                    {
                        existing.TariffCodeOptionId = request.TariffCodeOptionId.Value;
                        Repository<CustomerTariffOptionHistory>.UpdateItem(existing);
                    }
                    else
                    {
                        Repository<CustomerTariffOptionHistory>.InsertItem(new CustomerTariffOptionHistory
                        {
                            CustomerProfileId = customerId.Value,
                            TariffCodeOptionId = request.TariffCodeOptionId.Value,
                            EffectiveYear = year,
                            EffectiveMonth = month,
                            CreatedAt = DateTime.Now,
                        });
                    }
                }
            });
        }

        [HttpGet]
        [Route("[controller]/GetTariffForMonth/{year}/{month}")]
        public ExecutionResult GetTariffForMonth(int year, int month)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            if (month < 1 || month > 12)
                return new ExecutionResult(ResultType.Danger, "خطای ورود", "ماه باید بین ۱ تا ۱۲ باشد.", 400);

            return RunExceptionProof(() =>
            {
                var optionId = TariffResolver.ResolveTariffCodeOptionId(customerId.Value, year, month);
                if (optionId == null)
                    return (object)null;

                var option = Repository<TariffCodeOption>.GetListExtended(i => i.Id == optionId.Value,
                    includes: new[] { "TariffCode" }).LastOrDefault();

                return (object)new
                {
                    tariffCodeOptionId    = option?.Id,
                    tariffCodeId          = option?.TariffCodeId,
                    tariffCodeTitle       = option?.TariffCode?.Title,
                    tariffCodeOptionTitle = option?.Title,
                };
            });
        }

        [HttpPut]
        [Route("[controller]/SetTariffForMonth")]
        public ExecutionResult SetTariffForMonth([FromBody] SetTariffForMonthRequest request)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            if (request.Month < 1 || request.Month > 12)
                return new ExecutionResult(ResultType.Danger, "خطای ورود", "ماه باید بین ۱ تا ۱۲ باشد.", 400);

            return RunExceptionProof(() =>
            {
                // ثبت/جایگزینی تاریخچه: تعرفه از این ماه/سال شمسی مشخص به بعد همین مقدار
                // است — مستقل از «کد تعرفه» پروفایل که فقط پیش‌فرض زنده (ماه جاری به بعد) را نگه می‌دارد.
                var existing = Repository<CustomerTariffOptionHistory>.GetLast(h =>
                    h.CustomerProfileId == customerId.Value &&
                    h.EffectiveYear == request.Year && h.EffectiveMonth == request.Month);

                if (existing != null)
                {
                    existing.TariffCodeOptionId = request.TariffCodeOptionId;
                    Repository<CustomerTariffOptionHistory>.UpdateItem(existing);
                }
                else
                {
                    Repository<CustomerTariffOptionHistory>.InsertItem(new CustomerTariffOptionHistory
                    {
                        CustomerProfileId  = customerId.Value,
                        TariffCodeOptionId = request.TariffCodeOptionId,
                        EffectiveYear      = request.Year,
                        EffectiveMonth     = request.Month,
                        CreatedAt          = DateTime.Now,
                    });
                }
            });
        }

        [HttpGet]
        [Route("[controller]/GetCustomerAddresses")]
        public ExecutionResult GetCustomerAddresses()
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            return RunExceptionProof(() =>
            {
                var result = Repository<Address>.GetSelectiveList(i => new AddressResult
                {
                    Id = i.Id,
                    CityTitle = i.City.Title,
                    MainAddress = i.MainAddress,
                    PostalCode = i.PostalCode,
                    ProvinceTitle = i.City.Province.Name,
                    PowerEntityName = i.PowerEntity.Name,
                }, i => i.CustomerProfileId == customerId.Value, includes: new[] { "City.Province", "PowerEntity" });
                return (object)result;
            });
        }

        [HttpGet]
        [Route("[controller]/GetSubscriptions")]
        public ExecutionResult GetSubscriptions()
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            return RunExceptionProof(() =>
            {
                var result = Repository<Subscription>.GetSelectiveList(i => new
                {
                    i.Id,
                    i.BillIdentifier,
                    i.ContractCapacityKw,
                    i.AddressId,
                    MainAddress = i.Address.MainAddress,
                    PowerEntity = i.Address.PowerEntity.Name,
                }, i => i.Address.CustomerProfileId == customerId.Value, includes: new[] { "Address.PowerEntity", "Address" });
                return (object)result;
            });
        }

        [HttpPost]
        [Route("[controller]/AddSubscription")]
        public ExecutionResult AddSubscription([FromBody] AddSubscriptionRequest request)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var address = Repository<Address>.GetLast(i => i.Id == request.AddressId && i.CustomerProfileId == customerId.Value);
            if (address == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "آدرس یافت نشد یا به این حساب تعلق ندارد.", 403);

            var existing = Repository<Subscription>.GetLast(i => i.BillIdentifier == request.BillIdentifier);
            if (existing != null)
                return new ExecutionResult(ResultType.Danger, "خطا", "این شناسه قبض قبلاً ثبت شده است.", 400);

            return RunExceptionProof(() =>
            {
                var sub = Repository<Subscription>.InsertItem(new Subscription
                {
                    AddressId = request.AddressId,
                    BillIdentifier = request.BillIdentifier,
                    ContractCapacityKw = request.ContractCapacityKw,
                });
                return (object)sub.Id;
            });
        }

        [HttpPut]
        [Route("[controller]/UpdateSubscription")]
        public ExecutionResult UpdateSubscription([FromBody] UpdateSubscriptionRequest request)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var sub = Repository<Subscription>.GetListExtended(
                i => i.Id == request.Id && i.Address.CustomerProfileId == customerId.Value,
                includes: new[] { "Address" }).LastOrDefault();
            if (sub == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "شناسه یافت نشد یا به این حساب تعلق ندارد.", 404);

            var duplicate = Repository<Subscription>.GetLast(i => i.BillIdentifier == request.BillIdentifier && i.Id != request.Id);
            if (duplicate != null)
                return new ExecutionResult(ResultType.Danger, "خطا", "این شناسه قبض قبلاً ثبت شده است.", 400);

            return RunExceptionProof(() =>
            {
                sub.BillIdentifier = request.BillIdentifier;
                sub.ContractCapacityKw = request.ContractCapacityKw;
                return Repository<Subscription>.UpdateItem(sub);
            });
        }

        [HttpDelete]
        [Route("[controller]/DeleteSubscription/{id}")]
        public ExecutionResult DeleteSubscription(int id)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var sub = Repository<Subscription>.GetListExtended(
                i => i.Id == id && i.Address.CustomerProfileId == customerId.Value,
                includes: new[] { "Address" }).LastOrDefault();
            if (sub == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "شناسه یافت نشد.", 404);

            var hasBills = Repository<BillAnalysisReport>.GetLast(i => i.SubscriptionId == id);
            if (hasBills != null)
                return new ExecutionResult(ResultType.Danger, "خطا", "این شناسه دارای سابقه تحلیل قبض است و قابل حذف نیست.", 400);

            return RunExceptionProof(() => Repository<Subscription>.DeleteItem(sub));
        }

        [HttpPut]
        [Route("[controller]/UpdateRealCustomer")]
        public ExecutionResult UpdateRealCustomer([FromBody] CustomerReal customer)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var real = Repository<CustomersReal>.GetLast(i => i.Id == customerId.Value);
            if (real == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "مشتری یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                real.FirstName = customer.FirstName;
                real.LastName = customer.LastName;
                real.NationalCode = customer.NationalCode;
                real.Mobile = customer.Mobile;
                Repository<CustomersReal>.UpdateItem(real);
            });
        }

        [HttpPut]
        [Route("[controller]/UpdateLegalCustomer")]
        public ExecutionResult UpdateLegalCustomer([FromBody] CustomerLegal customer)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var legal = Repository<CustomersLegal>.GetLast(i => i.Id == customerId.Value);
            if (legal == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "مشتری یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                legal.CompanyName = customer.CompanyName;
                legal.EconomicCode = customer.EconomicCode;
                legal.CeoFullName = customer.CEO_FullName;
                legal.CeoMobile = customer.CEO_Mobile;
                Repository<CustomersLegal>.UpdateItem(legal);
            });
        }

        [HttpPost]
        [Route("[controller]/RegisterCustomerAgent")]
        public ExecutionResult RegisterCustomerAgent([FromBody] CustomerAgent agent)
        {
            return RunExceptionProof(() =>
            {
                var result = Repository<User>.InsertItem(new User
                {
                    FullName = agent.FullName,
                    Mobile = agent.Mobile,
                    Password = agent.Password,
                    IsActive = false
                });
                return (object)result;
            });
        }

        [HttpPut]
        [Route("[controller]/UpdateCustomerAgent")]
        public ExecutionResult UpdateCustomerAgent([FromBody] CustomerAgent agent)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var user = Repository<User>.GetLast(i => i.CustomerProfileId == customerId.Value);
            if (user == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "نماینده یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                user.FullName = agent.FullName;
                user.Mobile = agent.Mobile;
                if (!string.IsNullOrWhiteSpace(agent.Password))
                    user.Password = agent.Password;
                Repository<User>.UpdateItem(user);
            });
        }

        [HttpGet]
        [Route("[controller]/GetCustomerAgent")]
        public ExecutionResult GetCustomerAgent()
        {
            var customerId = GetCustomerProfileId();
            return RunExceptionProof(() =>
            {
                var result = Repository<User>.GetLast(i => i.CustomerProfileId == customerId);
                return (object)result;
            });
        }

        [HttpDelete]
        [Route("[controller]/DeleteAddress/{id}")]
        public ExecutionResult DeleteAddress(int id)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var address = Repository<Address>.GetLast(i => i.Id == id && i.CustomerProfileId == customerId.Value);
            if (address == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "آدرس یافت نشد.", 404);

            var hasSub = Repository<Subscription>.GetLast(i => i.AddressId == id);
            if (hasSub != null)
                return new ExecutionResult(ResultType.Danger, "خطا", "این آدرس دارای انشعاب فعال است و قابل حذف نیست.", 400);

            return RunExceptionProof(() =>
            {
                Repository<Address>.DeleteItem(address);
            });
        }

        [HttpPost]
        [Route("[controller]/CreateTicket")]
        public ExecutionResult CreateTicket([FromBody] CreateTicketRequest request)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            int? newTicketId = null;
            var result = RunExceptionProof(() =>
            {
                var ticket = Repository<Ticket>.InsertItem(new Ticket
                {
                    CustomerProfileId = customerId.Value,
                    Subject           = request.Subject,
                    StatusId          = 1,
                    CreatedAt         = DateTime.Now,
                });
                newTicketId = ticket.Id;

                var userId = new UseContext(new HttpContextAccessor()).GetUserId();
                Repository<TicketMessage>.InsertItem(new TicketMessage
                {
                    TicketId     = ticket.Id,
                    Body         = request.Body,
                    SenderUserId = userId ?? 0,
                    CreatedAt    = DateTime.Now,
                });

                return (object)ticket.Id;
            });

            if (result.Code == 200 && newTicketId.HasValue)
                _ = NotifyTicketAdminsAsync(newTicketId.Value, request.Subject);

            return result;
        }

        private static readonly Logger _smsLog = LogManager.GetLogger("TicketSmsNotify");

        private static async Task NotifyTicketAdminsAsync(int ticketId, string subject)
        {
            try
            {
                var mobiles = Repository<User>.Query(db =>
                {
                    // Find SiteMap entries related to the admin tickets page
                    var ticketSiteIds = db.SiteMaps
                        .Where(s => s.PhysicalPath != null &&
                                    s.PhysicalPath.ToLower().Contains("ticket"))
                        .Select(s => s.Id)
                        .ToList();

                    if (!ticketSiteIds.Any())
                        return new List<string>();

                    // Roles that have access to any of those SiteMap entries
                    var roleIds = db.SiteMapRoles
                        .Where(sr => ticketSiteIds.Contains(sr.SiteMapId))
                        .Select(sr => sr.RoleId)
                        .Distinct()
                        .ToList();

                    // User IDs that hold one of those roles
                    var userIds = db.UserRoles
                        .Where(ur => roleIds.Contains(ur.RoleId))
                        .Select(ur => ur.UserId)
                        .Distinct()
                        .ToList();

                    // Active admin users (CustomerProfileId == null) with a mobile
                    return db.Users
                        .Where(u => u.IsActive == true
                                 && u.CustomerProfileId == null
                                 && userIds.Contains(u.Id)
                                 && u.Mobile != null)
                        .Select(u => u.Mobile)
                        .ToList();
                });

                AppDbLogger.Info("TicketSmsNotify", "Notify.Start",
                    $"ticketId={ticketId} | subject={subject} | adminCount={mobiles.Count}");

                foreach (var mobile in mobiles)
                {
                    var normalized = mobile.StartsWith("0") ? "+98" + mobile.Substring(1) : mobile;
                    await SmsService.SendAsync(normalized,
                        $"تیکت جدید در سامانه متین‌پاور ثبت شد.\nشماره تیکت: {ticketId}\nموضوع: {subject}");
                }
            }
            catch (Exception ex)
            {
                _smsLog.Error(ex, "TicketSmsNotify failed for ticketId={0}", ticketId);
                AppDbLogger.Error("TicketSmsNotify", "Notify.Exception",
                    $"ticketId={ticketId} | {ex.Message}", ex);
            }
        }

        [HttpPost]
        [Route("[controller]/AddTicketMessage")]
        public ExecutionResult AddTicketMessage([FromBody] AddTicketMessageRequest request)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var ticket = Repository<Ticket>.GetLast(i => i.Id == request.TicketId && i.CustomerProfileId == customerId.Value);
            if (ticket == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "تیکت یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                var userId = new UseContext(new HttpContextAccessor()).GetUserId();
                Repository<TicketMessage>.InsertItem(new TicketMessage
                {
                    TicketId     = request.TicketId,
                    Body         = request.Body,
                    FileId       = request.FileId,
                    SenderUserId = userId ?? 0,
                    CreatedAt    = DateTime.Now,
                });
            });
        }

        [HttpGet]
        [Route("[controller]/GetTicket")]
        public ExecutionResult GetTicket()
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            return RunExceptionProof(() =>
            {
                var result = Repository<Ticket>.GetSelectiveList(i => new
                {
                    i.Id,
                    i.Subject,
                    Status    = i.Status.Title,
                    i.StatusId,
                    CreatedAt = PersianDateConverter.ToPersianDate(i.CreatedAt, "yyyy/MM/dd"),
                    MessageCount = i.TicketMessages.Count,
                }, i => i.CustomerProfileId == customerId.Value, includes: new[] { "Status", "TicketMessages" });
                return (object)result;
            });
        }

        [HttpGet]
        [Route("[controller]/GetProfileMeta")]
        public ExecutionResult GetProfileMeta()
        {
            return ExecutionResult.Success;
        }

        [HttpPost]
        [Route("[controller]/UpdateIdentityDoc")]
        public ExecutionResult UpdateIdentityDoc([FromBody] UpdateFileRequest request)
        {
            return ExecutionResult.Success;
        }

        // ─── Customer Documents (max 5) ──────────────────────────────────────────

        [HttpGet]
        [Route("[controller]/GetDocuments")]
        public ExecutionResult GetDocuments()
        {
            var customerId = GetCustomerProfileId();
            try
            {
                Expression<Func<CustomerDocument, bool>> docsPred =
                    d => d.CustomerProfileId == customerId.Value && !d.IsDeleted;
                var docs = customerId.HasValue
                    ? Repository<CustomerDocument>.GetListExtended(docsPred)
                          .OrderBy(d => d.CreatedAt)
                          .Select(d => new CustomerDocumentDto
                          {
                              Id        = d.Id,
                              FileId    = d.FileId.ToString(),
                              Title     = d.Title,
                              CreatedAt = d.CreatedAt.ToString("yyyy-MM-dd"),
                          })
                          .ToList()
                    : new List<CustomerDocumentDto>();

                return new ExecutionResult(ResultType.Success, "موفق", "", 200, docs);
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        [HttpPost]
        [Route("[controller]/AddDocument")]
        public ExecutionResult AddDocument([FromBody] AddDocumentRequest request)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);
            if (request?.FileId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "شناسه فایل الزامی است.", 400);

            Expression<Func<CustomerDocument, bool>> activePred =
                d => d.CustomerProfileId == customerId.Value && !d.IsDeleted;
            var count = Repository<CustomerDocument>.GetListExtended(activePred).Count();
            if (count >= 5)
                return new ExecutionResult(ResultType.Warning, "محدودیت", "حداکثر ۵ مدرک مجاز است.", 400);

            try
            {
                Repository<CustomerDocument>.InsertItem(new CustomerDocument
                {
                    CustomerProfileId = customerId.Value,
                    FileId            = request.FileId.Value,
                    Title             = request.Title?.Trim(),
                    CreatedAt         = DateTime.Now,
                    IsDeleted         = false,
                });
                return ExecutionResult.Success;
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        [HttpDelete]
        [Route("[controller]/DeleteDocument/{id}")]
        public ExecutionResult DeleteDocument(int id)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            Expression<Func<CustomerDocument, bool>> docPred =
                d => d.Id == id && d.CustomerProfileId == customerId.Value && !d.IsDeleted;
            var doc = Repository<CustomerDocument>.GetLast(docPred);
            if (doc == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "مدرک یافت نشد.", 404);

            try
            {
                doc.IsDeleted = true;
                Repository<CustomerDocument>.UpdateItem(doc);
                return ExecutionResult.Success;
            }
            catch (Exception ex) { return HandleException(ex); }
        }

        [HttpGet]
        [Route("[controller]/GetTicketById/{ticketId}")]
        public ExecutionResult GetTicketById(int ticketId)
        {
            return RunExceptionProof(() =>
            {
                var result = Repository<TicketMessage>.GetSelectiveList(i => new
                {
                    i.Id,
                    i.Body,
                    FileId     = i.FileId.HasValue ? i.FileId.ToString() : (string?)null,
                    SenderName = i.SenderUser.FullName ?? "پشتیبانی",
                    IsAdmin    = i.SenderUser.CustomerProfileId == null,
                    i.CreatedAt,
                }, i => i.TicketId == ticketId, includes: new[] { "SenderUser" });
                return (object)result;
            });
        }
    }
}
