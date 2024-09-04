namespace TimeSaver.Models.Requests
{
    public class RequestOrderDto
    {
        public int ContentPackageId { get; set; }

        public string Device { get; set; } = null!;

        public string PaymentMethod { get; set; } = null!;

        public int SessionId { get; set; }

        public int UserId { get; set; }

        public string Version { get; set; } = null!;
    }
}
