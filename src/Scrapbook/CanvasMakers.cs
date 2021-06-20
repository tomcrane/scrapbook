using System.Collections.Generic;
using IIIF;
using IIIF.ImageApi.Service;
using IIIF.Presentation.V3;
using IIIF.Presentation.V3.Annotation;
using IIIF.Presentation.V3.Content;
using Newtonsoft.Json.Linq;

namespace Scrapbook
{
    /// <summary>
    /// This is all horrible, better to stay as JSON rather than parsing backwards and forwards.
    /// </summary>
    public static class CanvasMakers
    {
        public static Canvas Canvas3FromJsonImage2(JObject imageApi)
        {
            var id = imageApi["@id"].Value<string>();
            var canvas = new Canvas
            {
                Id = $"{id}/canvas",
                Width = imageApi["width"].Value<int>(),
                Height = imageApi["height"].Value<int>()
            };
            var body = new Image
            {
                Id = $"{id}/full/max/0/default.jpg",
                Format = "image/jpeg",
                Service = new List<IService>
                {
                    new ImageService2 {Id = id, Profile = imageApi["profile"][0].Value<string>()}
                }
            };

            SetItemsFromBody(canvas, body);
            return canvas;
        }

        public static Canvas Canvas3FromJson2(JToken jCanvas)
        {
            // ideally we just have Canvas.Parse() here...
            var canvas = new Canvas
            {
                Id = jCanvas["@id"].Value<string>(),
                Width = jCanvas["width"].Value<int>(),
                Height = jCanvas["height"].Value<int>()
            };
            var image = jCanvas["images"][0]["resource"];
            var body = new Image
            {
                Id = image["@id"].Value<string>(),
                Format = image["format"]?.Value<string>()
            };
            if (image["service"] != null)
            {
                body.Service = new List<IService>
                {
                    new ImageService2
                    {
                        Id = image["service"]["@id"].Value<string>(),
                        Profile = GetValueOrFirstChild(image["service"]["profile"])?.Value<string>()
                    }
                };
            }
            
            SetItemsFromBody(canvas, body);
            return canvas;
        }

        private static JToken GetValueOrFirstChild(JToken jToken)
        {
            return jToken switch
            {
                null => null,
                JArray => jToken[0],
                _ => jToken
            };
        }

        public static Canvas Canvas3FromJson3(JToken jCanvas)
        {
            // ideally we just have Canvas.Parse() here...
            var canvas = new Canvas
            {
                Id = jCanvas["id"].Value<string>(),
                Width = jCanvas["width"].Value<int>(),
                Height = jCanvas["height"].Value<int>()
            };
            var image = jCanvas["items"][0]["items"][0]["body"];
            var body = new Image
            {
                Id = image["id"].Value<string>(),
                Format = image["format"].Value<string>()
            };
            if (image["service"] != null)
            {
                body.Service = new List<IService>
                {
                    new ImageService2()
                    {
                        Id = image["service"][0]["@id"].Value<string>(),
                        Profile = image["service"][0]["profile"].Value<string>()
                    }
                };
            }

            SetItemsFromBody(canvas, body);
            return canvas;
        }

        private static void SetItemsFromBody(Canvas canvas, Image body)
        {
            canvas.Items = new List<AnnotationPage>
            {
                new()
                {
                    Items = new List<IAnnotation>
                    {
                        new PaintingAnnotation
                        {
                            Body = body,
                            Target = new Canvas
                            {
                                Id = canvas.Id
                            }
                        }
                    }
                }
            };
        }
    }
}