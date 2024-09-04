namespace TimeSaver.Models
{
    public class SettingsDto
    {
        public string AccountEmailAddress { get; set; } = null!;

        public string AccountPassword { get; set; } = null!;

        public int? LastContentPackageId { get; set; }

        public int? LastPublicationId { get; set; }
    }
}
