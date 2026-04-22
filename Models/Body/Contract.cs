namespace MatinPower.Server.Models
{
    public partial class Contract
    {
        public decimal Amount { get; set; }
        public int TypeId { get; set; }
        public Guid FileId { get; set; }
    }
}
