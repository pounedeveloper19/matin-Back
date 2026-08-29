namespace MatinPower.Server.Models.Body
{
    public class UpdateTariffCodeRequest
    {
        public int? TariffCodeOptionId { get; set; }
    }

    public class SetTariffForMonthRequest
    {
        public int Year  { get; set; }
        public int Month { get; set; }
        public int TariffCodeOptionId { get; set; }
    }

    public class CustomerDocumentDto
    {
        public int    Id        { get; set; }
        public string FileId    { get; set; }
        public string Title     { get; set; }
        public string CreatedAt { get; set; }
    }
}
