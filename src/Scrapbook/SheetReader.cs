using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IIIF;
using IIIF.ImageApi.Service;
using IIIF.Presentation.V3;
using IIIF.Presentation.V3.Annotation;
using IIIF.Presentation.V3.Content;
using IIIF.Presentation.V3.Strings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Range = IIIF.Presentation.V3.Range;

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

        public async Task<StructureBase> GetManifest(string sheetId, string manifestUrl)
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
            return await MakeManifest(title, locale, sheet.Values, manifestUrl);
        }

        private async Task<StructureBase> MakeManifest(string title, string locale, string[][] values, string manifestUrl)
        {
            var manifest = new Manifest
            {
                Id = manifestUrl,
                Label = new LanguageMap(locale, title)
            };

            var structures = new List<Range>();
            var currentRange = new Range();
            structures.Add(currentRange);
            
            // TODO - process all the rows in parallel, so we're not waiting on HTTP traffic
            foreach (var row in values)
            {
                // is it:
                if (row.Length == 0) continue;
                if (row[0].StartsWith("http"))
                {
                    // a link to something that can be turned into a canvas
                    string labelString = "-";
                    string labelLocale = "none";
                    if (row.Length > 1 && !string.IsNullOrWhiteSpace(row[1]))
                    {
                        labelString = row[1];
                        labelLocale = locale;
                    }

                    var label = new LanguageMap(labelLocale, labelString);
                    var canvas = await GetCanvas(row[0], label, manifestUrl);
                    
                    if (canvas != null)
                    {
                        manifest.Items ??= new List<Canvas>();
                        manifest.Items.Add(canvas);
                        currentRange.Items ??= new List<IStructuralLocation>();
                        currentRange.Items.Add(canvas);
                    }
                } 
                else if (!string.IsNullOrWhiteSpace(row[0]))
                {
                    // a row to be used to create structure
                    if (currentRange.Items != null)
                    {
                        // this is a new range to be started
                        currentRange = new Range
                        {
                            Id = $"{manifestUrl}/{structures.Count}",
                            Label = new LanguageMap(locale, row[0])
                        };
                        structures.Add(currentRange);
                    }

                    currentRange.Label = new LanguageMap(locale, row[0]);
                }
                // all other kinds of rows are ignored, do what you like in them
            }

            if (structures.Count > 1)
            {
                // only put the ranges in if there is more than one.
                manifest.Structures = structures;
            }
            return manifest;
        }

        private async Task<Canvas> GetCanvas(string sourceUrl, LanguageMap label, string manifestUrl)
        {
            if (sourceUrl.StartsWith("https://commons.wikimedia.org/wiki/File:"))
            {
                return await GetMediaWikiFileCanvas(sourceUrl, label, manifestUrl);
            }
            
            // TODO - a much more comprehensive pipeline of processors here, for common image sources
            
            // Mostly method not allowed so bit of a waste to call this
            // var imageCanvas = await TryCanvasFromImageHeadRequest(sourceUrl);
            // if (imageCanvas != null)
            // {
            //     imageCanvas.Label = label;
            //     return imageCanvas;
            // }

            // We're going to have to get the whole response
            var client = httpClientFactory.CreateClient();
            try
            {
                var response = await client.GetAsync(sourceUrl);
                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType == null)
                {
                    return null;
                }
                if (mediaType.StartsWith("image/"))
                {
                    // now we can measure the image dimensions
                    await using Stream stream = await response.Content.ReadAsStreamAsync();
                    using var image = await SixLabors.ImageSharp.Image.LoadAsync(stream);
                    return CanvasFromImageUrl(sourceUrl, mediaType, image.Width, image.Height);
                }

                if(mediaType.Contains("/json") || mediaType.Contains("/ld+json"))
                {
                    // let's hope it's IIIF of some sort...
                    var s = await response.Content.ReadAsStringAsync();
                    var jObj = JObject.Parse(s);
                    string context = null;
                    if(!jObj.TryGetValue("@context", out var jContext))
                    {
                        return null;
                    }
                    context = jContext.Value<string>();
                    if (context == null)
                    {
                        return null;
                    }

                    Canvas canvas;
                    
                    if (context.Contains("iiif.io/api/presentation/3"))
                    {
                        canvas = CanvasMakers.Canvas3FromJson3(jObj["items"]?[0]);
                    }
                    else if (context.Contains("iiif.io/api/presentation/2"))
                    {
                        canvas = CanvasMakers.Canvas3FromJson2(jObj["sequences"]?[0]?["canvases"]?[0]);
                    }
                    else if (context.Contains("iiif.io/api/image/2"))
                    {
                        canvas = CanvasMakers.Canvas3FromJsonImage2(jObj);
                    }
                    else
                    {
                        canvas = null;
                    }

                    return canvas;
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private async Task<Canvas> GetMediaWikiFileCanvas(string sourceUrl, LanguageMap label, string manifestUrl)
        {
            var client = httpClientFactory.CreateClient();
            var proxyManifestUrl = $"{config.MediaWikiProxy}{sourceUrl}";
            var s = await client.GetStringAsync(proxyManifestUrl);
            var jObj = JObject.Parse(s);
            return CanvasMakers.Canvas3FromJson2(jObj["sequences"]?[0]?["canvases"]?[0]);
        }


        private async Task<Canvas> TryCanvasFromImageHeadRequest(string sourceUrl)
        {
            var client = httpClientFactory.CreateClient();
            try
            {
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, sourceUrl));
                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType != null && mediaType.StartsWith("image/"))
                {
                    // the downside of this is that we don't know the width and height
                    return CanvasFromImageUrl(sourceUrl, mediaType, 5000, 5000);
                }
            }
            catch (Exception e)
            {
                // catch a method not allowed; we'll have to get the whole lot
            }

            return null;
        }

        private static Canvas CanvasFromImageUrl(string sourceUrl, string mediaType, int width, int height)
        {
            return new()
            {
                Id = $"{sourceUrl}/canvas",
                Width = width,
                Height = height,
                Items = new List<AnnotationPage>
                {
                    new()
                    {
                        Items = new List<IAnnotation>
                        {
                            new PaintingAnnotation
                            {
                                Body = new Image
                                {
                                    Id = sourceUrl,
                                    Format = mediaType
                                },
                                Target = new Canvas
                                {
                                    Id = $"{sourceUrl}/canvas"
                                }
                            }
                        }
                    }
                }
            };
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