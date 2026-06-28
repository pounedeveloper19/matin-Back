namespace MatinPower.Server.Models
{
    public class UpdateFileRequest
    {
        public Guid? FileId { get; set; }
    }

    public class AddDocumentRequest
    {
        public Guid? FileId { get; set; }
        public string? Title { get; set; }
    }
}
