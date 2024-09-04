namespace TimeSaver.Models.Responses
{
    public class ContentPackagePublicationDto
    {
        public int ContentPackageId { get; set; }

        public int PublicationId { get; set; }

        public string PublicationName { get; set; } = null!;
    }
}
