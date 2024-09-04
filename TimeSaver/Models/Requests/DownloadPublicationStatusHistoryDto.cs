namespace TimeSaver.Models.Requests
{
    public class DownloadPublicationStatusHistoryDto
    {
        public int PublicationId { get; set; }

        public string PublicationQuality { get; set; } = null!;

        public string RequestedPublicationTitleFormat { get; set; } = null!;
        
        public string StatusInfo { get; set; } = null!;

        public string StatusTime { get; set; } = null!;
    }
}
