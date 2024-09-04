namespace TimeSaver.Models.Requests
{
    public class ValidateSubscriptionDto
    {
        public string Device { get; set; } = null!;

        public string DownloadToken { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public int SessionId { get; set; }

        public string SubscriptionType { get; set; } = null!;

        public int UserId { get; set; }

        public string Version { get; set; } = null!;
    }
}
