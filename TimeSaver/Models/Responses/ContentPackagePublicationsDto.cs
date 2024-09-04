namespace TimeSaver.Models.Responses
{
    public class ContentPackagePublicationsDto
    {
        public string PublicationDate { get; set; } = null!;

        public ContentPackagePublicationDto[] ContentPackagePublication { get; set; } = null!;
    }
}
