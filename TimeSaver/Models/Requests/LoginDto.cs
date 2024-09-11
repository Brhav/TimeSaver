namespace TimeSaver.Models.Requests
{
    public class LoginDto
    {
        public string Action { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string State { get; set; } = null!;

        public string Username { get; set; } = null!;
    }
}
