namespace TimeSaver.Models.Responses
{
    public class ValidateSubscriptionTokenDto
    {
        public string Status { get; set; } = null!;

        public int SubscriptionId { get; set; }
    }
}
