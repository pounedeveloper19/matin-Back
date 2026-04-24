namespace MatinPower.Server.Models.Body
{
    public class CreateTicketRequest
    {
        public string Subject { get; set; } = null!;
        public string Body    { get; set; } = null!;
    }

    public class AddTicketMessageRequest
    {
        public int    TicketId { get; set; }
        public string Body     { get; set; } = null!;
        public Guid?  FileId   { get; set; }
    }

    public class UpdateTicketStatusRequest
    {
        public int TicketId { get; set; }
        public int StatusId { get; set; }
    }
}
