using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using TimeSaver.Models;
using TimeSaver.Models.Responses;

namespace TimeSaver
{
    public class Downloader
    {
        private const string AUTH_BASE_URL = "https://auth.mediafin.be";
        private const string BASE_URL = "https://krant.tijd.be";

        private readonly HttpClient _authHttpClient;
        private readonly HttpClient _httpClient;

        public Downloader()
        {
            _authHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

            _authHttpClient.DefaultRequestHeaders.Add("Origin", AUTH_BASE_URL);

            _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });

            _httpClient.DefaultRequestHeaders.Add("Origin", BASE_URL);
        }

        public void Run()
        {
            Console.WriteLine("Running...");

            var settings = LoadSettings();

            if (string.IsNullOrEmpty(settings.AccountEmailAddress) || string.IsNullOrEmpty(settings.AccountPassword))
            {
                Console.WriteLine("Settings missing");

                return;
            }

            var regionProfileValues = GetProfileValues("Regio");

            var regionProfileValue = regionProfileValues[0];

            var contentPackages = GetContentPackageList(regionProfileValue.Value);

            var contentPackagesToDownload = settings.LastContentPackageId == null ? new[] { contentPackages[0] } :
                contentPackages.Where(cp => cp.ContentPackageId > settings.LastContentPackageId).OrderBy(cp => cp.ContentPackageId).ToArray();

            if (!contentPackagesToDownload.Any())
            {
                Console.WriteLine("No content packages to download");

                return;
            }

            var uniqueId = Guid.NewGuid();

            var anonSession = GetOpenSession(0, uniqueId);

            var authorizeLocation = GetAuthorize();

            var loginUrl = AUTH_BASE_URL + authorizeLocation;

            var resumeLocation = PostLogin(loginUrl, settings.AccountEmailAddress, settings.AccountPassword);

            var resumeUrl = AUTH_BASE_URL + resumeLocation;

            var codeUrl = GetResume(resumeUrl);

            var queryParameters = HttpUtility.ParseQueryString(new Uri(codeUrl).Query);

            var subscriptionToken = queryParameters["code"];

            var validateSubscriptionToken = GetValidateSubscriptionToken(regionProfileValue.Value, anonSession.SessionInfo.SessionId, subscriptionToken!, anonSession.SessionInfo.UserId);

            if (validateSubscriptionToken.Status == "SERVICE_FAILURE")
            {
                Console.WriteLine($"Status is {validateSubscriptionToken.Status}");

                return;
            }

            foreach (var contentPackage in contentPackagesToDownload)
            {
                var session = GetOpenSession(anonSession.SessionInfo.UserId, uniqueId);

                var contentPackagePublications = GetContentPackagePublications(contentPackage.ContentPackageId);

                var contentPackagePublicationsToDownload = settings.LastPublicationId == null ? new[] { contentPackagePublications.ContentPackagePublication[0] } :
                    contentPackagePublications.ContentPackagePublication.Where(cpp => cpp.PublicationId > settings.LastPublicationId).OrderBy(cpp => cpp.PublicationId).ToArray();

                if (!contentPackagePublicationsToDownload.Any())
                {
                    Console.WriteLine("No content package publications to download");

                    return;
                }

                foreach (var contentPackagePublication in contentPackagePublicationsToDownload)
                {
                    var requestOrder = PostRequestOrder(contentPackagePublication.ContentPackageId, session.SessionInfo.SessionId, session.SessionInfo.UserId);

                    var requestDownload = GetRequestDownload(session.SessionInfo.UserId, session.SessionInfo.SessionId, validateSubscriptionToken.SubscriptionId, contentPackagePublication.ContentPackageId, requestOrder.OrderId);

                    var confirmDownload = PostConfirmDownload(requestDownload.DownloadId, contentPackage.ContentPackageId, session.SessionInfo.SessionId, session.SessionInfo.UserId);

                    var downloadCredentials = GetDownloadCredentials(session.SessionInfo.UserId, requestOrder.OrderId, requestDownload.DownloadId);

                    var publicationPages = GetPublicationPages(downloadCredentials.JsonPattern, contentPackagePublication.PublicationId);

                    var filePaths = new List<string>();

                    foreach (var page in publicationPages.Page)
                    {
                        var filePath = Path.GetTempPath() + Guid.NewGuid() + ".pdf";

                        DownloadPublicationPage(downloadCredentials.PdfPattern, page.PublicationPageId, filePath);

                        filePaths.Add(filePath);
                    }

                    Console.WriteLine("Converting into single pdf...");

                    var bytes = PdfCombiner.CombineIntoSinglePdf(filePaths);

                    Console.WriteLine("Converting into single pdf complete");

                    var parsedPublicationDate = DateTime.Parse(contentPackagePublications.PublicationDate);

                    var publicationYear = parsedPublicationDate.ToString("yyyy");

                    var publicationMonth = parsedPublicationDate.ToString("MM");

                    var outputDirectory = $"Downloads/{publicationYear}/{publicationMonth}";

                    if (!Directory.Exists(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    var fullPublicationDate = parsedPublicationDate.ToString("yyyyMMdd");

                    var outputPath = $"{outputDirectory}/{fullPublicationDate}_{contentPackagePublication.PublicationId}_{SanitizeFileName(contentPackagePublication.PublicationName)}.pdf";

                    Console.WriteLine("Writing " + outputPath + "...");

                    File.WriteAllBytes(outputPath, bytes);

                    Console.WriteLine("Writing " + outputPath + " complete");

                    Console.WriteLine("Cleaning up...");

                    foreach (var filePath in filePaths)
                    {
                        File.Delete(filePath);
                    }

                    Console.WriteLine("Cleaning up complete");

                    settings.LastContentPackageId = contentPackagePublication.ContentPackageId;

                    settings.LastPublicationId = contentPackagePublication.PublicationId;

                    SaveSettings(settings);
                }
            }

            Console.WriteLine("Running complete");
        }

        private void DownloadPublicationPage(string pdfPattern, int publicationPageId, string filePath)
        {
            Console.WriteLine($"Downloading publication page ({publicationPageId})...");

            var url = pdfPattern.Replace("%PUBPAGE_ID%", publicationPageId.ToString());

            using var stream = _httpClient.GetStreamAsync(url);

            using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);

            stream.Result.CopyTo(fileStream);

            Console.WriteLine($"Downloading publication page ({publicationPageId}) complete");
        }

        private string GetAuthorize()
        {
            Console.WriteLine($"Getting authorize...");

            var url = $"https://auth.mediafin.be/authorize?client_id=rtU2js35WJrI5rh5quuTceJGM1yDVFDF&response_type=code&ext-agency=tijd&ext-newspaper=true&ui_locales=nl&scope=offline_access&redirect_uri=https%3A%2F%2Fkrant.tijd.be";

            var response = _authHttpClient.GetAsync(url).Result;

            var location = response.Headers.Location!.ToString();

            Console.WriteLine($"Getting authorize complete");

            return location;
        }

        private string GetResume(string resumeUrl)
        {
            Console.WriteLine($"Getting resume...");

            var response = _authHttpClient.GetAsync(resumeUrl).Result;

            var location = response.Headers.Location!.ToString();

            Console.WriteLine($"Getting resume complete");

            return location;
        }

        private ContentPackageDto[] GetContentPackageList(string regionValue)
        {
            Console.WriteLine("Getting content package list...");

            var url = $"https://mfn-tij-production-api.twipecloud.net/Data/DataService.svc/getcontentpackagelist/{regionValue}/0/30";

            var responseBody = _httpClient.GetStringAsync(url).Result;

            var result = JsonConvert.DeserializeObject<ContentPackageDto[]>(responseBody) ?? throw new InvalidOperationException();

            Console.WriteLine("Getting content package list complete");

            return result;
        }

        private ContentPackagePublicationsDto GetContentPackagePublications(int contentPackageId)
        {
            Console.WriteLine($"Getting content package publications ({contentPackageId})...");

            var timestamp = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            var url = $"https://krant.tijd.be/data/{contentPackageId}/data/GetContentPackagePublications-{contentPackageId}-V3.json?t={timestamp}";

            var responseBody = _httpClient.GetStringAsync(url).Result;

            var result = JsonConvert.DeserializeObject<ContentPackagePublicationsDto>(responseBody) ?? throw new InvalidOperationException();

            Console.WriteLine($"Getting content package publications ({contentPackageId}) complete");

            return result;
        }

        private DownloadCredentialsDto GetDownloadCredentials(int userId, int orderId, int downloadId)
        {
            Console.WriteLine("Getting download credentials...");

            var url = $"https://mfn-tij-production-api.twipecloud.net/GetDownloadCredentials/{userId}/{orderId}/{downloadId}";

            var responseBody = _httpClient.GetStringAsync(url).Result;

            var result = JsonConvert.DeserializeObject<DownloadCredentialsDto>(responseBody) ?? throw new InvalidOperationException();

            Console.WriteLine("Getting download credentials complete");

            return result;
        }

        private SessionDto GetOpenSession(int userId, Guid uniqueId)
        {
            Console.WriteLine("Getting open session...");

            var url = $"https://mfn-tij-production-api.twipecloud.net/Session/SessionService.svc/json/OpenSession/webApp/{userId}/{uniqueId}/1.0.0/1.0.0";

            var responseBody = _httpClient.GetStringAsync(url).Result;

            var result = JsonConvert.DeserializeObject<SessionDto>(responseBody) ?? throw new InvalidOperationException();

            Console.WriteLine("Getting open session complete");

            return result;
        }

        private ProfileValueDto[] GetProfileValues(string key)
        {
            Console.WriteLine("Getting profile values...");

            var url = $"https://mfn-tij-production-api.twipecloud.net/Data/DataService.svc/GetProfileValues/{key}";

            var responseBody = _httpClient.GetStringAsync(url).Result;

            var result = JsonConvert.DeserializeObject<ProfileValueDto[]>(responseBody) ?? throw new InvalidOperationException();

            Console.WriteLine("Getting profile values complete");

            return result;
        }

        private PublicationPagesDto GetPublicationPages(string jsonPattern, int publicationId)
        {
            Console.WriteLine($"Getting publication pages ({publicationId})...");

            var url = jsonPattern.Replace("%JSON_NAME%", "GetPublicationPages-" + publicationId);

            var responseBody = _httpClient.GetStringAsync(url).Result;

            var result = JsonConvert.DeserializeObject<PublicationPagesDto>(responseBody) ?? throw new InvalidOperationException();

            Console.WriteLine($"Getting publication pages ({publicationId}) complete");

            return result;
        }

        private RequestDownloadDto GetRequestDownload(int userId, int sessionId, int subscriptionId, int contentPackageId, int orderId)
        {
            Console.WriteLine("Getting request download...");

            var url = $"https://mfn-tij-production-api.twipecloud.net/Session/SessionService.svc/json/RequestDownload/{userId}/{sessionId}/{subscriptionId}/{contentPackageId}/{orderId}/Subscription";

            var responseBody = _httpClient.GetStringAsync(url).Result;

            var result = JsonConvert.DeserializeObject<RequestDownloadDto>(responseBody) ?? throw new InvalidOperationException();

            Console.WriteLine("Getting request download complete");

            return result;
        }

        private ValidateSubscriptionTokenDto GetValidateSubscriptionToken(string downloadToken, int sessionId, string subscriptionToken, int userId)
        {
            Console.WriteLine("Getting validate subscription token...");

            var url = $"https://mfn-tij-production-api.twipecloud.net/Session/SessionService.svc/json/ValidateSubscriptionToken?UserID={userId}&SessionID={sessionId}&subscriptionToken={subscriptionToken}&downloadToken={downloadToken}&subscriptionType=MFN";

            var responseBody = _httpClient.GetStringAsync(url).Result;

            var result = JsonConvert.DeserializeObject<ValidateSubscriptionTokenDto>(responseBody) ?? throw new InvalidOperationException();

            Console.WriteLine("Getting validate subscription token complete");

            return result;
        }

        private ConfirmDownloadDto PostConfirmDownload(int downloadId, int publicationId, int sessionId, int userId)
        {
            Console.WriteLine("Posting confirm download...");

            var url = "https://mfn-tij-production-api.twipecloud.net/Session/SessionService.svc/json/Confirm_Download";

            var dateTime = DateTime.Now;

            var statusTime = dateTime.ToString("yyyy-MM-ddTHH:mm:ss") + dateTime.ToString("zzz").Substring(0, 3) + dateTime.ToString("zzz").Substring(4);

            var postBody = new Models.Requests.ConfirmDownloadDto
            {
                Device = "webApp",
                DownloadId = downloadId,
                DownloadPublicationStatusHistory = new Models.Requests.DownloadPublicationStatusHistoryDto
                {
                    PublicationId = publicationId,
                    PublicationQuality = "Full",
                    RequestedPublicationTitleFormat = "Newspaper",
                    StatusInfo = "",
                    StatusTime = statusTime
                },
                DownloadStatus = "Requested",
                SessionId = sessionId,
                UserId = userId,
                Version = "3.9.3"
            };

            var content = new StringContent(JsonConvert.SerializeObject(postBody), Encoding.UTF8, "application/json");

            var responseBody = _httpClient.PostAsync(url, content).Result.Content.ReadAsStringAsync().Result;

            var result = JsonConvert.DeserializeObject<ConfirmDownloadDto>(responseBody) ?? throw new InvalidOperationException();

            Console.WriteLine("Posting confirm download complete");

            return result;
        }

        private string PostLogin(string loginUrl, string userName, string password)
        {
            Console.WriteLine("Posting login...");

            var queryParameters = HttpUtility.ParseQueryString(new Uri(loginUrl).Query);

            var postBody = new Models.Requests.LoginDto
            {
                Action = "default",
                Password = password,
                State = queryParameters["state"]!,
                Username = userName
            };

            var serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };

            var content = new StringContent(JsonConvert.SerializeObject(postBody, serializerSettings), Encoding.UTF8, "application/json");

            var response = _authHttpClient.PostAsync(loginUrl, content).Result;

            var location = response.Headers.Location!.ToString();

            Console.WriteLine("Posting login complete");

            return location;
        }

        private RequestOrderDto PostRequestOrder(int contentPackageId, int sessionId, int userId)
        {
            Console.WriteLine("Posting request order...");

            var url = "https://mfn-tij-production-api.twipecloud.net/Session/SessionService.svc/json/Request_Order";

            var postBody = new Models.Requests.RequestOrderDto
            {
                ContentPackageId = contentPackageId,
                Device = "webApp",
                PaymentMethod = "Subscription",
                SessionId = sessionId,
                UserId = userId,
                Version = "1.0.0.0"
            };

            var content = new StringContent(JsonConvert.SerializeObject(postBody), Encoding.UTF8, "application/json");

            var responseBody = _httpClient.PostAsync(url, content).Result.Content.ReadAsStringAsync().Result;

            var result = JsonConvert.DeserializeObject<RequestOrderDto>(responseBody) ?? throw new InvalidOperationException();

            Console.WriteLine("Posting request order complete");

            return result;
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = new string(Path.GetInvalidFileNameChars());
            var escapedInvalidChars = Regex.Escape(invalidChars);
            var invalidCharsPattern = $"[{escapedInvalidChars}]";
            var sanitized = Regex.Replace(fileName, invalidCharsPattern, string.Empty);
            var result = sanitized.Replace(" ", "_");
            return result;
        }

        private SettingsDto LoadSettings()
        {
            Console.WriteLine("Loading settings...");

            var json = File.ReadAllText("Config/Settings.json");

            var result = JsonConvert.DeserializeObject<SettingsDto>(json) ?? throw new InvalidOperationException();

            Console.WriteLine("Loading settings complete");

            return result;
        }

        private void SaveSettings(SettingsDto settings)
        {
            Console.WriteLine("Saving settings...");

            string json = JsonConvert.SerializeObject(settings);

            File.WriteAllText("Config/Settings.json", json);

            Console.WriteLine("Saving settings complete");
        }
    }
}
