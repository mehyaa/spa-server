using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using SpaOptions=Spa.Server.SpaOptions;
using StaticFileOptions=Spa.Server.StaticFileOptions;

DotNetEnv.Env.Load();

var host = CreateWebHostBuilder(args).Build();

await host.RunAsync();

static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
    WebHost
        .CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
            {
                var env = context.HostingEnvironment;

                var spaOptions =
                    context.Configuration
                        .GetSection("Spa")
                        .Get<SpaOptions>();

                var staticFileOptions =
                    context.Configuration
                        .GetSection("StaticFiles")
                        .Get<StaticFileOptions[]>();

                services.AddSingleton(spaOptions ?? new SpaOptions());
                services.AddSingleton(staticFileOptions ?? new StaticFileOptions[0]);

                var spaRootPath =
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
                                : Path.Combine(spaRootPath, filePath);

                        var content = File.ReadAllText(currentFilePath);

                        content = Environment.ExpandEnvironmentVariables(content);

                        File.WriteAllText(currentFilePath, content);
                    }
                }

                services.AddSpaStaticFiles(setup => setup.RootPath = spaRootPath);
            }
        )
        .Configure((context, app) =>
            {
                var env = context.HostingEnvironment;

                var isProduction = string.Equals(env.EnvironmentName, "production", StringComparison.InvariantCultureIgnoreCase);

                var spaOptions = app.ApplicationServices.GetRequiredService<SpaOptions>();
                var staticFileOptions = app.ApplicationServices.GetRequiredService<StaticFileOptions[]>();

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

                app.UsePathBase(spaOptions.UrlBasePath ?? "/");

                var spaStaticFileOptions =
                    new Microsoft.AspNetCore.Builder.StaticFileOptions
                    {
                        OnPrepareResponse =
                            context =>
                            {
                                if (context.File.Name == spaOptions.DefaultPage)
                                {
                                    context.Context.Response.Headers.Add("Cache-Control", "no-cache");
                                    context.Context.Response.Headers.Add("Expires", "Thu, 01 Jan 1970 00:00:01 GMT");
                                }
                                else
                                {
                                    context.Context.Response.Headers.Add("Cache-Control", $"public, max-age={(spaOptions.CacheDurationInMinutes * 60).ToString()}");
                                    context.Context.Response.Headers.Add("Expires", DateTime.UtcNow.AddMinutes(spaOptions.CacheDurationInMinutes).ToString("R"));
                                }
                            }
                    };

                if (staticFileOptions?.Length > 0)
                {
                    foreach (var staticFileOption in staticFileOptions)
                    {
                        var rootPath =
                            Path.IsPathRooted(staticFileOption.RootPath)
                                ? staticFileOption.RootPath
                                : Path.Combine(env.ContentRootPath, staticFileOption.RootPath);

                        app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
                        {
                            FileProvider = new PhysicalFileProvider(rootPath),
                            RequestPath = staticFileOption.RequestPath,
                            ServeUnknownFileTypes = true,
                            OnPrepareResponse = staticFileOption.Download ? MarkDownloadable : _ => { }
                        });
                    }
                }

                app.UseSpaStaticFiles(spaStaticFileOptions);

                app.UseSpa(builder =>
                {
                    builder.Options.DefaultPage = $"/{spaOptions.DefaultPage}";
                    builder.Options.DefaultPageStaticFileOptions = spaStaticFileOptions;
                });
            }
        );

static void MarkDownloadable(StaticFileResponseContext context)
{
    context.Context.Response.Headers.Add("Content-Disposition", $"attachment; filename={context.File.Name}");
}
