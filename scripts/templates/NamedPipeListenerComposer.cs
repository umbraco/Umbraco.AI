using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Umbraco.AI.DemoSite.Composers;

/// <summary>
/// Configures Kestrel to listen on a named pipe for the demo site.
/// This enables tools to connect via HTTP over named pipes without port discovery.
/// </summary>
public class NamedPipeListenerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register Kestrel configuration
        builder.Services.AddSingleton<IConfigureOptions<KestrelServerOptions>, NamedPipeKestrelConfiguration>();

        // Register site address endpoint
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter("SiteAddressEndpointFilter")
            {
                Endpoints = app =>
                {
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/site-address", async context =>
                        {
                            var server = context.RequestServices.GetRequiredService<IServer>();
                            var addressesFeature = server.Features.Get<IServerAddressesFeature>();
                            var address = addressesFeature?.Addresses
                                .FirstOrDefault(a => a.StartsWith("https") && !a.Contains("pipe:"))
                                ?? "https://localhost:44355";

                            context.Response.ContentType = "text/plain";
                            await context.Response.WriteAsync(address);
                        });
                    });
                }
            });
        });
    }
}

/// <summary>
/// Configures Kestrel server options to add a named pipe listener.
/// </summary>
public class NamedPipeKestrelConfiguration(IHostEnvironment hostEnvironment, IConfiguration configuration)
    : IConfigureOptions<KestrelServerOptions>
{
    public void Configure(KestrelServerOptions options)
    {
        if (!hostEnvironment.IsDevelopment())
            return;

        options.ListenNamedPipe($"umbraco.demosite.{GetUniqueIdentifier()}");

        // Read URLs from configuration or use dynamic HTTPS
        var urls = configuration["ASPNETCORE_URLS"] ?? configuration["urls"];
        if (string.IsNullOrEmpty(urls))
        {
            options.Listen(IPAddress.Loopback, 0, o => o.UseHttps());
        }
        else
        {
            foreach (var url in urls.Split(';'))
            {
                var uri = new Uri(url);
                options.Listen(IPAddress.Loopback, uri.Port, o =>
                {
                    if (uri.Scheme == "https")
                        o.UseHttps();
                });
            }
        }
    }

    public static string GetUniqueIdentifier()
    {
        static string Sanitize(string name) =>
            string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.')) is { Length: > 0 } s ? s : "default";

        static string RunGit(string args)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                return process?.StandardOutput.ReadToEnd().Trim() ?? "";
            }
            catch
            {
                return "";
            }
        }

        try
        {
            var gitDir = RunGit("rev-parse --git-dir");

            // Check if this is a worktree
            if (gitDir.Contains("worktrees"))
            {
                var parts = gitDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var worktreeIndex = Array.FindIndex(parts, p => p == "worktrees");
                if (worktreeIndex >= 0 && worktreeIndex + 1 < parts.Length)
                    return Sanitize(parts[worktreeIndex + 1]);
            }

            // Main worktree - use branch name
            return Sanitize(RunGit("branch --show-current") is { Length: > 0 } branch ? branch : "default");
        }
        catch
        {
            return "default";
        }
    }
}
