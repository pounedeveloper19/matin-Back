using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Customer
{
    public class CustomerProfileController : BaseController
    {
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
                // 1. ساخت CustomerProfile
                var profile = Repository<CustomerProfile>.InsertItem(new CustomerProfile
                {
                    CustomerTypeId = 2,  // حقوقی
                    IsActive = true,
                    FamiliarityType = customer.FamiliarityType > 0 ? customer.FamiliarityType : (int?)null,
                });

                // 2. ساخت CustomersLegal با همان Id پروفایل (shared PK)
                Repository<Models.CustomersLegal>.InsertItem(new Models.CustomersLegal
                {
                    Id = profile.Id,
                    NationalId = customer.NationalId,
                    CompanyName = customer.CompanyName,
                    EconomicCode = customer.EconomicCode,
                    CeoFullName = customer.CEO_FullName,
                    CeoMobile = customer.CEO_Mobile,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    FamiliarityType = customer.FamiliarityType > 0 ? customer.FamiliarityType : (int?)null,
                    CustomerTypeId = 2,
                });

                // 3. لینک کردن کاربر جاری به این پروفایل
                var currentMobile = new UseContext(new HttpContextAccessor()).GetUserName();
                if (!string.IsNullOrEmpty(currentMobile))
                {
                    var user = Repository<User>.GetLast(i => i.Mobile == currentMobile);
                    if (user != null && user.CustomerProfileId == null)
                    {
                        user.CustomerProfileId = profile.Id;
                        Repository<User>.UpdateItem(user);
                    }
                }

                return new ExecutionResult(ResultType.Success, null, null, 200, profile.Id);
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
                // 1. ساخت CustomerProfile
                var profile = Repository<CustomerProfile>.InsertItem(new CustomerProfile
                {
                    CustomerTypeId = 1,  // حقیقی
                    IsActive = true,
                    FamiliarityType = customer.FamiliarityType > 0 ? customer.FamiliarityType : (int?)null,
                });

                // 2. ساخت CustomersReal با همان Id پروفایل (shared PK)
                Repository<CustomersReal>.InsertItem(new CustomersReal
                {
                    Id = profile.Id,
                    NationalCode = customer.NationalCode,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Mobile = customer.Mobile,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    FamiliarityType = customer.FamiliarityType > 0 ? customer.FamiliarityType : (int?)null,
                    CustomerTypeId = 1,
                });

                // 3. لینک کردن کاربر جاری به این پروفایل
                var currentMobile = new UseContext(new HttpContextAccessor()).GetUserName();
                if (!string.IsNullOrEmpty(currentMobile))
                {
                    var user = Repository<User>.GetLast(i => i.Mobile == currentMobile);
                    if (user != null && user.CustomerProfileId == null)
                    {
                        user.CustomerProfileId = profile.Id;
                        Repository<User>.UpdateItem(user);
                    }
                }

                return new ExecutionResult(ResultType.Success, null, null, 200, profile.Id);
            });
        }
        [HttpPost]
        [Route("[controller]/AddAddress")]
        public ExecutionResult AddAddress([FromBody] AddAddress address)
        {
            var existAddress = Repository<Address>.GetLast(i => i.PostalCode == address.PostalCode);
            if (existAddress != null)
            {
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "این کد پستی قبلا در سیستم ثبت شده است.", 5000);
            }
            return RunExceptionProof(() =>
            {
                var result = Repository<Address>.InsertItem(new Address
                {
                    CustomerProfileId = address.CustomerProfileId,
                    PostalCode = address.PostalCode,
                    CityId = address.CityId,
                    PowerEntityId = address.PowerEntityId,
                    MainAddress = address.MainAddress
                });
                return new ExecutionResult(ResultType.Success, null, null, 200, result);
            });

        }
        [HttpGet]
        [Route("[controller]/GetLegalCustomer")]
        public ExecutionResult GetCustomer()
        {
            var customerId = new UseContext(new HttpContextAccessor()).GetCustomerId();
            var customerType = Repository<CustomerProfile>.GetLast(i => i.Id == customerId);
            if (customerType == null)
            {
                return new ExecutionResult(ResultType.Danger, "خطای ورود اطلاعات", "مشتری یافت نگردید، لطفا ابتدا نسبت به تکمیل پروفایل خود اقدام فرمائید.", 5000);
            }
            // CustomerTypeId 1 = حقیقی (Real)، CustomerTypeId 2 = حقوقی (Legal)
            if (customerType.CustomerTypeId == 1)
            {
                return RunExceptionProof(() =>
                {
                    var result = Repository<CustomersReal>.GetSelectiveList(i => new CustomerReal
                    {
                        NationalCode = i.NationalCode,
                        FirstName = i.FirstName,
                        LastName = i.LastName,
                        Mobile = i.Mobile,
                    }, i => i.Id == customerId);
                    return new ExecutionResult(ResultType.Success, null, null, 200, result);
                });
            }

            if (customerType.CustomerTypeId == 2)
            {
                return RunExceptionProof(() =>
                {
                    var result = Repository<CustomersLegal>.GetSelectiveList(i => new CustomerLegal
                    {
                        CEO_FullName = i.CeoFullName,
                        CEO_Mobile = i.CeoMobile,
                        CompanyName = i.CompanyName,
                        EconomicCode = i.EconomicCode,
                        NationalId = i.NationalId
                    }, i => i.Id == customerId);
                    return new ExecutionResult(ResultType.Success, null, null, 200, result);
                });
            }
            return new ExecutionResult(ResultType.Danger, "خطا", "نوع مشتری نامعتبر است.", 400);
        }
        [HttpGet]
        [Route("[controller]/GetCustomerAddresses")]
        public ExecutionResult GetCustomerAddresses()
        {
            var customerId = new UseContext(new HttpContextAccessor()).GetCustomerId();
            var result = Repository<Address>.GetSelectiveList(i => new AddressResult
            {
                Id           = i.Id,
                CityTitle    = i.City.Title,
                MainAddress  = i.MainAddress,
                PostalCode   = i.PostalCode,
                ProvinceTitle = i.City.Province.Name,
                PowerEntityName = i.PowerEntity.Name,
            }, i => i.CustomerProfileId == customerId);
            return RunExceptionProof(() =>
            {
                return new ExecutionResult(ResultType.Success, null, null, 200, result);
            });
        }

        [HttpGet]
        [Route("[controller]/GetSubscriptions")]
        public ExecutionResult GetSubscriptions()
        {
            var customerId = new UseContext(new HttpContextAccessor()).GetCustomerId();
            var result = Repository<Subscription>.GetSelectiveList(i => new
            {
                i.Id,
                i.BillIdentifier,
                i.ContractCapacityKw,
                AddressId    = i.AddressId,
                MainAddress  = i.Address.MainAddress,
                PowerEntity  = i.Address.PowerEntity.Name,
            }, i => i.Address.CustomerProfileId == customerId);
            return RunExceptionProof(() =>
            {
                return new ExecutionResult(ResultType.Success, null, null, 200, result);
            });
        }

        [HttpPost]
        [Route("[controller]/AddSubscription")]
        public ExecutionResult AddSubscription([FromBody] AddSubscriptionRequest request)
        {
            var customerId = new UseContext(new HttpContextAccessor()).GetCustomerId();

            // تأیید اینکه آدرس متعلق به همین مشتری است
            var address = Repository<Address>.GetLast(i => i.Id == request.AddressId && i.CustomerProfileId == customerId);
            if (address == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "آدرس یافت نشد یا به این حساب تعلق ندارد.", 403);

            var existing = Repository<Subscription>.GetLast(i => i.BillIdentifier == request.BillIdentifier);
            if (existing != null)
                return new ExecutionResult(ResultType.Danger, "خطا", "این شناسه قبض قبلاً ثبت شده است.", 400);

            return RunExceptionProof(() =>
            {
                var sub = Repository<Subscription>.InsertItem(new Subscription
                {
                    AddressId          = request.AddressId,
                    BillIdentifier     = request.BillIdentifier,
                    ContractCapacityKw = request.ContractCapacityKw,
                });
                return new ExecutionResult(ResultType.Success, null, null, 200, sub.Id);
            });
        }

        [HttpPut]
        [Route("[controller]/UpdateRealCustomer")]
        public ExecutionResult UpdateRealCustomer([FromBody] CustomerReal customer)
        {
            var customerId = new UseContext(new HttpContextAccessor()).GetCustomerId();
            return RunExceptionProof(() =>
            {
                var real = Repository<CustomersReal>.GetLast(i => i.Id == customerId);
                if (real == null)
                    return new ExecutionResult(ResultType.Danger, "خطا", "مشتری یافت نشد.", 404);
                real.FirstName   = customer.FirstName;
                real.LastName    = customer.LastName;
                real.NationalCode = customer.NationalCode;
                real.Mobile      = customer.Mobile;
                Repository<CustomersReal>.UpdateItem(real);
                return new ExecutionResult(ResultType.Success, null, null, 200);
            });
        }

        [HttpPut]
        [Route("[controller]/UpdateLegalCustomer")]
        public ExecutionResult UpdateLegalCustomer([FromBody] CustomerLegal customer)
        {
            var customerId = new UseContext(new HttpContextAccessor()).GetCustomerId();
            return RunExceptionProof(() =>
            {
                var legal = Repository<CustomersLegal>.GetLast(i => i.Id == customerId);
                if (legal == null)
                    return new ExecutionResult(ResultType.Danger, "خطا", "مشتری یافت نشد.", 404);
                legal.CompanyName   = customer.CompanyName;
                legal.EconomicCode  = customer.EconomicCode;
                legal.CeoFullName   = customer.CEO_FullName;
                legal.CeoMobile     = customer.CEO_Mobile;
                Repository<CustomersLegal>.UpdateItem(legal);
                return new ExecutionResult(ResultType.Success, null, null, 200);
            });
        }
        [HttpPost]
        [Route("[controller]/RegisterCustomerAgent")]
        public ExecutionResult RegisterCustomerAgent([FromBody] CustomerAgent agent)
        {
            var result = Repository<User>.InsertItem(new User
            {
                FullName = agent.FullName,
                Mobile = agent.Mobile,
                Password = agent.Password,
                IsActive = false
            });
            return RunExceptionProof(() =>
            {
                return new ExecutionResult(ResultType.Success, null, null, 200, result);
            });
        }
        [HttpGet]
        [Route("[controller]/GetCustomerAgent")]
        public ExecutionResult GetCustomerAgent()
        {
            var customerId = new UseContext(new HttpContextAccessor()).GetCustomerId();
            var result = Repository<User>.GetLast(i => i.CustomerProfileId == customerId);
            return RunExceptionProof(() =>
            {
                return new ExecutionResult(ResultType.Success, null, null, 200, result);
            });
        }
        [HttpGet]
        [Route("[controller]/GetTicket")]
        public ExecutionResult GetTicket()
        {
            var customerId = new UseContext(new HttpContextAccessor()).GetCustomerId();
            var result = Repository<Ticket>.GetSelectiveList(i => new CustomerTicket
            {
                Status = i.Status.Title,
                Subject = i.Subject

            }, i => i.CustomerProfileId == customerId);
            return RunExceptionProof(() =>
            {
                return new ExecutionResult(ResultType.Success, null, null, 200, result);
            });
        }
        [HttpGet]
        [Route("[controller]/GetTicketById/{ticketId}")]
        public ExecutionResult GetTicketById(int ticketId)
        {
            var result = Repository<TicketMessage>.GetSelectiveList(i => new CustomerTicket
            {
                Body = i.Body!

            }, i => i.TicketId == ticketId);
            return RunExceptionProof(() =>
            {
                return new ExecutionResult(ResultType.Success, null, null, 200, result);
            });
        }
    }
}
