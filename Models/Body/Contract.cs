using System.ComponentModel.DataAnnotations.Schema;

namespace MatinPower.Server.Models
{
    public partial class Contract
    {
        [NotMapped]
        public decimal Amount { get; set; }

        [NotMapped]
        public int TypeId { get; set; }
    }

    public class SubmitWarrantyRequest
    {
        public int ContractId { get; set; }
        public decimal Amount { get; set; }
        public int TypeId { get; set; }
        public Guid? FileId { get; set; }
    }
}
