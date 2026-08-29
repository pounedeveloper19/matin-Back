namespace MatinPower.Server.Models.Body
{
    public class CreateOrderRequest
    {
        public int SubscriptionId { get; set; }
        public decimal RequestedKwh { get; set; }
        public int EnergyTypeId { get; set; }
        public bool IsPriceRequest { get; set; } = false;
        public int? Year { get; set; }
        public int? Month { get; set; }
        public bool IsGreenEnergy { get; set; } = false;
    }

    public class SubmitPaymentRequest
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public int MethodId { get; set; }
        public string? ReferenceNumber { get; set; }
        public Guid? ReceiptFileId { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public int OrderId { get; set; }
        public int StatusId { get; set; }
        public decimal? PriceAtMoment { get; set; }
    }

    public class ConfirmPaymentRequest
    {
        public int PaymentId { get; set; }
        public int StatusId { get; set; }
    }
}
