namespace TimeSaver.Models.Requests
{
    public class LoginPasswordDto
    {
        public string Password { get; set; } = null!;

        public string State { get; set; } = null!;

        public string Username { get; set; } = null!;
    }
}
