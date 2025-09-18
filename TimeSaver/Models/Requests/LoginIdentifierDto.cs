namespace TimeSaver.Models.Requests
{
    public class LoginIdentifierDto
    {
        public string IsBrave { get; set; } = null!;

        public string JsAvailable { get; set; } = null!;

        public string State { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string WebauthnAvailable { get; set; } = null!;

        public string WebauthnPlatformAvailable { get; set; } = null!;
    }
}
