using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Spa.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            DotNetEnv.Env.Load();

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost
                .CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                    {
                        var env = context.HostingEnvironment;

                        var spaOptions =
                            context.Configuration
                                .GetSection("Spa")
                                .Get<SpaOptions>();

                        services.AddSingleton(spaOptions);

                        var spaPath =
                            Path.IsPathRooted(spaOptions.RootPath)
                                ? spaOptions.RootPath
                                : Path.Combine(env.ContentRootPath, spaOptions.RootPath);

                        // Files need to be populated before configuration
                        if (spaOptions.PopulateEnvironmentVariablesInFiles?.Count > 0)
                        {
                            foreach (var filePath in spaOptions.PopulateEnvironmentVariablesInFiles)
                            {
                                var currentFilePath =
                                    Path.IsPathRooted(filePath)
                                        ? filePath
                                        : Path.Combine(spaPath, filePath);

                                var content = File.ReadAllText(currentFilePath);

                                content = Environment.ExpandEnvironmentVariables(content);

                                File.WriteAllText(currentFilePath, content);
                            }
                        }

                        services.AddSpaStaticFiles(setup => setup.RootPath = spaPath);
                    }
                )
                .Configure((context, app) =>
                    {
                        var env = context.HostingEnvironment;

                        var isProduction = string.Equals(env.EnvironmentName, "production", StringComparison.InvariantCultureIgnoreCase);

                        var spaOptions = app.ApplicationServices.GetRequiredService<SpaOptions>();

                        app.UseExceptionHandler(
                            builder =>
                                builder.Run(
                                    async context =>
                                    {
                                        context.Response.ContentType = "text/plain";

                                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                                        if (isProduction)
                                        {
                                            await context.Response.WriteAsync("Error occured.");

                                            return;
                                        }

                                        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();

                                        var exception = exceptionHandlerFeature?.Error;

                                        await context.Response.WriteAsync(exception?.ToString() ?? "Error occured.");
                                    }));

                        var staticFileOptions =
                            new StaticFileOptions
                            {
                                OnPrepareResponse =
                                    ctx =>
                                    {
                                        if (ctx.File.Name == spaOptions.DefaultPage)
                                        {
                                            ctx.Context.Response.Headers.Add("Cache-Control", "no-cache");
                                            ctx.Context.Response.Headers.Add("Expires", "Thu, 01 Jan 1970 00:00:01 GMT");
                                        }
                                        else
                                        {
                                            ctx.Context.Response.Headers.Add("Cache-Control", $"public, max-age={spaOptions.CacheDurationInMinutes * 60}");
                                            ctx.Context.Response.Headers.Add("Expires", DateTime.UtcNow.AddMinutes(spaOptions.CacheDurationInMinutes).ToString("R"));
                                        }
                                    }
                            };

                        app.UseSpaStaticFiles(staticFileOptions);

                        app.UseSpa(setup =>
                        {
                            setup.Options.DefaultPage = $"/{spaOptions.DefaultPage}";
                            setup.Options.DefaultPageStaticFileOptions = staticFileOptions;
                        });
                    }
                );
    }
}
