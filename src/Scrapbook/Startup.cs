using IIIF.Serialisation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Scrapbook
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.Configure<ScrapbookConfig>(Configuration.GetSection("ScrapbookConfig"));
            services.AddSingleton<SheetReader>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("{**identifier}", async context =>
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    var reader = app.ApplicationServices.GetService<SheetReader>();
                    var identifier = context.Request.RouteValues["identifier"] as string;
                    if ("favicon.ico" == identifier)
                    {
                        context.Response.Redirect("https://iiif.io/favicon.ico");
                        return;
                    }
                    if (!string.IsNullOrWhiteSpace(identifier))
                    {
                        var sheetId = reader!.Parse(identifier);
                        if (sheetId != identifier)
                        {
                            context.Response.Redirect($"/{sheetId}");
                            return;
                        }

                        var manifestUrl = context.Request.GetDisplayUrl();
                        var iiif = await reader.GetManifest(sheetId, manifestUrl);
                        if (iiif == null)
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync("No sheet found for " + identifier);
                        }
                        else
                        {
                            context.Response.ContentType = reader.ContentType;
                            await context.Response.WriteAsync(iiif.AsJson());
                        }
                    }
                    else
                    {
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync("Please visit <a href='https://github.com/tomcrane/scrapbook/blob/main/README.md'>Scrapbook</a> for info");
                    }
                });
            });
        }
    }
}