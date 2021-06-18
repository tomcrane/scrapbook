using System.Net.Http;
using System.Threading.Tasks;
using IIIF.Presentation.V3;
using IIIF.Presentation.V3.Strings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Scrapbook
{
    public class SheetReader
    {
        private ScrapbookConfig config;
        private IHttpClientFactory httpClientFactory;
        
        public SheetReader(
            IOptions<ScrapbookConfig> options,
            IHttpClientFactory httpClientFactory)
        {
            config = options.Value;
            this.httpClientFactory = httpClientFactory;
        }

        public string ContentType => config.ContentType;

        public async Task<StructureBase> GetManifest(string sheetId)
        {
            // first get the sheet
            var httpClient = httpClientFactory.CreateClient();
            var url = $"{config.GoogleSheetsRoot}{sheetId}?key={config.GoogleSheetsApiKey}";
            var s = await httpClient.GetStringAsync(url);
            var info = JObject.Parse(s);
            var title = info["properties"]?.Value<string>("title");
            var locale = info["properties"]?.Value<string>("locale") ?? "none";
            if (locale.Contains("_"))
            {
                locale = locale[..2];
            }
            // we could get the first sheet title and the grid props from this, but we can shortcut for now
            url = $"{config.GoogleSheetsRoot}{sheetId}/values/A1:C1000?key={config.GoogleSheetsApiKey}";
            s = await httpClient.GetStringAsync(url);
            var sheet = JsonConvert.DeserializeObject<GoogleSheet>(s);
            return await MakeManifest(title, locale, sheet.Values);
        }

        private async Task<StructureBase> MakeManifest(string title, string locale, string[][] values)
        {
            var manifest = new Manifest
            {
                Label = new LanguageMap(locale, title)
            };
            return manifest;
        }

        public string Parse(string identifier)
        {
            // if this is a full sheets URL, pull out the Id.
            if (identifier.StartsWith("http"))
            {
                var parts = identifier.Split('/');
                return parts[^2];
            }
            return identifier;
        }
    }
}