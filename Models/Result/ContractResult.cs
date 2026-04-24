namespace MatinPower.Server.Models.Result
{
    public class ContractResult
    {
        public int Id { get; set; }

        public string Subscription { get; set; }

        public string? ContractNumber { get; set; }

        public decimal ContractRate { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public int StatusId { get; set; }
        public string Status { get; set; }
        public string Address { get; set; }
        public decimal WarrantyAmount { get; set; }
        public string WarrantyType { get; set; }
        public int WarrantyTypeId { get; set; }
        public string? WarrantyFileId { get; set; }
    }
}
