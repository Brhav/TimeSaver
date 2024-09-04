namespace TimeSaver.Models.Requests
{
    public class ConfirmDownloadDto
    {
        public string Device { get; set; } = null!;

        public int DownloadId { get; set; }

        public DownloadPublicationStatusHistoryDto DownloadPublicationStatusHistory { get; set; } = null!;
        
        public string DownloadStatus { get; set; } = null!;

        public int SessionId { get; set; }

        public int UserId { get; set; }

        public string Version { get; set; } = null!;
    }
}
