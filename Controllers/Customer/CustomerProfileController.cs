using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

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
                    companyName = legal.CompanyName,
                    nationalId = legal.NationalId,
                    economicCode = legal.EconomicCode,
                    ceo_FullName = legal.CeoFullName,
                    ceo_Mobile = legal.CeoMobile,
                });
            }

            return new ExecutionResult(ResultType.Danger, "خطا", "نوع مشتری نامعتبر است.", 400);
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

            return RunExceptionProof(() =>
            {
                var ticket = Repository<Ticket>.InsertItem(new Ticket
                {
                    CustomerProfileId = customerId.Value,
                    Subject           = request.Subject,
                    StatusId          = 1,
                    CreatedAt         = DateTime.Now,
                });

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
                    i.CreatedAt,
                    MessageCount = i.TicketMessages.Count,
                }, i => i.CustomerProfileId == customerId.Value, includes: new[] { "Status", "TicketMessages" });
                return (object)result;
            });
        }

        [HttpGet]
        [Route("[controller]/GetProfileMeta")]
        public ExecutionResult GetProfileMeta()
        {
            var customerId = GetCustomerProfileId();
            return RunExceptionProof(() =>
            {
                var profile = customerId.HasValue
                    ? Repository<CustomerProfile>.GetLast(i => i.Id == customerId.Value)
                    : null;
                return (object)new
                {
                    identityDocFileId = profile?.IdentityDocFileId.HasValue == true
                        ? profile.IdentityDocFileId.ToString()
                        : (string?)null
                };
            });
        }

        [HttpPost]
        [Route("[controller]/UpdateIdentityDoc")]
        public ExecutionResult UpdateIdentityDoc([FromBody] UpdateFileRequest request)
        {
            var customerId = GetCustomerProfileId();
            if (customerId == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "کاربر احراز هویت نشده.", 401);

            var profile = Repository<CustomerProfile>.GetLast(i => i.Id == customerId.Value);
            if (profile == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "پروفایل یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                profile.IdentityDocFileId = request.FileId;
                Repository<CustomerProfile>.UpdateItem(profile);
            });
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
