namespace Scrapbook
{
    public class ScrapbookConfig
    {
        public string GoogleSheetsApiKey { get; set; }
        public string ContentType = "application/ld+json;profile=\"http://iiif.io/api/presentation/3/context.json\"";
        public string GoogleSheetsRoot = "https://sheets.googleapis.com/v4/spreadsheets/";
        public string MediaWikiProxy = "https://iiifmediawiki.herokuapp.com/";
    }
}