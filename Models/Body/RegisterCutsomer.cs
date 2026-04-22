namespace MatinPower.Server.Models.Body
{
    public class RegisterCutsomer
    {
        public int CustomerTypeId { get; set; }
        public int FamiliarityType { get; set; }
    }
    public class CustomerLegal : RegisterCutsomer
    {
        public string CompanyName { get; set; }
        public string NationalId { get; set; }
        public string EconomicCode { get; set; }
        public string CEO_FullName { get; set; }
        public string CEO_Mobile { get; set; }

    }
    public class CustomerReal : RegisterCutsomer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NationalCode { get; set; }
        public string Mobile { get; set; }
    }
    public class AddAddress
    {
        public int CustomerProfileId { get; set; }
        public int PowerEntityId { get; set; }
        public int CityId { get; set; }
        public string MainAddress { get; set; }
        public string PostalCode { get; set; }
    }
    public class AddressResult
    {
        public int Id { get; set; }
        public string PowerEntityName { get; set; }
        public string CityTitle { get; set; }
        public string ProvinceTitle { get; set; }
        public string MainAddress { get; set; }
        public string PostalCode { get; set; }
    }
    public class CustomerAgent
    {
        public int CustomerProfileId { get; set; }
        public string FullName { get; set; }
        public string Mobile { get; set; }
        public string Password { get; set; }
    }

    // Public self-registration bodies (no auth required)
    public class PublicRegisterReal
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NationalCode { get; set; }
        public string Mobile { get; set; }
        public string Password { get; set; }
        public int FamiliarityType { get; set; }
    }

    public class PublicRegisterLegal
    {
        public string CompanyName { get; set; }
        public string NationalId { get; set; }
        public string EconomicCode { get; set; }
        public string CeoFullName { get; set; }
        public string CeoMobile { get; set; }
        public string Mobile { get; set; }
        public string Password { get; set; }
        public int FamiliarityType { get; set; }
    }
    public class AddSubscriptionRequest
    {
        public int AddressId { get; set; }
        public string BillIdentifier { get; set; }
        public decimal? ContractCapacityKw { get; set; }
    }

    public class CustomerTicket
    {
        public string Subject { get; set; }
        public string Status { get; set; }
        public string Body { get; set; }
    }
}
