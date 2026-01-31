using System.IO.Pipes;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Umbraco.Ai.DemoSite.Middleware;

/// <summary>
/// Middleware that exposes the demo site's port via a named pipe for API client generation.
/// This enables Claude Code and local developers to automatically discover the running port.
/// </summary>
public class PortDiscoveryMiddleware
{
    private static bool _pipeStarted;
    private static CancellationTokenSource? _pipeCts;
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _hostEnvironment;

    public PortDiscoveryMiddleware(
        RequestDelegate next,
        IHostEnvironment hostEnvironment,
        IHostApplicationLifetime lifetime)
    {
        _next = next;
        _hostEnvironment = hostEnvironment;

        // Stop pipe server when app shuts down
        lifetime.ApplicationStopping.Register(() => _pipeCts?.Cancel());
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_pipeStarted && _hostEnvironment.IsDevelopment() && context.Request.Host.Port.HasValue)
        {
            _pipeStarted = true;
            var port = context.Request.Host.Port.Value;

            // Start pipe server in background
            _pipeCts = new CancellationTokenSource();
            _ = Task.Run(() => StartPipeServer(port, _pipeCts.Token));
        }

        await _next(context);
    }

    private static async Task StartPipeServer(int port, CancellationToken ct)
    {
        var identifier = GetUniqueIdentifier();
        var pipeName = $"umbraco-ai-demo-port-{identifier}";
        Console.WriteLine($"Port discovery pipe: {pipeName} (port {port})");

        var tasks = new List<Task>();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Spawn a new server instance to handle the next client
                tasks.Add(HandleClientAsync(pipeName, port, ct));

                // Clean up completed tasks to prevent memory leaks
                tasks.RemoveAll(t => t.IsCompleted);

                // Small delay to prevent tight loop
                await Task.Delay(10, ct);
            }
            catch (OperationCanceledException)
            {
                break; // Clean shutdown
            }
        }

        // Wait for all active connections to complete
        try
        {
            await Task.WhenAll(tasks);
        }
        catch
        {
            // Ignore exceptions during shutdown
        }
    }

    private static async Task HandleClientAsync(string pipeName, int port, CancellationToken ct)
    {
        try
        {
            // Create named pipe (auto-cleaned up when process dies)
            using var pipeServer = new NamedPipeServerStream(
                pipeName,
                PipeDirection.Out,
                NamedPipeServerStream.MaxAllowedServerInstances, // Allow multiple concurrent connections
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            // Wait for client to connect
            await pipeServer.WaitForConnectionAsync(ct);

            // Send port number
            var portBytes = Encoding.UTF8.GetBytes(port.ToString());
            await pipeServer.WriteAsync(portBytes, ct);
            await pipeServer.FlushAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Clean shutdown
        }
        catch
        {
            // Silently fail - individual client failures shouldn't crash the server
        }
    }

    private static string GetUniqueIdentifier()
    {
        try
        {
            var repoRoot = FindRepoRoot();
            var gitDir = GetGitDirectory(repoRoot);

            // Check if this is a worktree (git dir contains "worktrees")
            if (gitDir.Contains("worktrees"))
            {
                // Extract worktree name from path: .git/worktrees/{name}
                var parts = gitDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var worktreeIndex = Array.FindIndex(parts, p => p == "worktrees");
                if (worktreeIndex >= 0 && worktreeIndex + 1 < parts.Length)
                {
                    return SanitizePipeName(parts[worktreeIndex + 1]);
                }
            }

            // Main worktree - use branch name
            return GetGitBranch(repoRoot);
        }
        catch
        {
            return "default";
        }
    }

    private static string GetGitDirectory(string repoRoot)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --git-dir",
                    WorkingDirectory = repoRoot,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var gitDir = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return Path.GetFullPath(Path.Combine(repoRoot, gitDir));
        }
        catch
        {
            return Path.Combine(repoRoot, ".git");
        }
    }

    private static string GetGitBranch(string repoRoot)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "branch --show-current",
                    WorkingDirectory = repoRoot,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var branch = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return string.IsNullOrEmpty(branch) ? "detached" : SanitizePipeName(branch);
        }
        catch
        {
            return "default";
        }
    }

    private static string SanitizePipeName(string name)
    {
        // Remove characters invalid for pipe names (keep alphanumeric, dash, underscore)
        var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
        return string.IsNullOrEmpty(sanitized) ? "default" : sanitized;
    }

    private static string FindRepoRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null && !File.Exists(Path.Combine(current, "package.json")))
        {
            current = Directory.GetParent(current)?.FullName;
        }
        return current ?? Directory.GetCurrentDirectory();
    }
}

/// <summary>
/// Composer that automatically registers the PortDiscoveryMiddleware.
/// No manual Program.cs changes required.
/// </summary>
public class PortDiscoveryComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(
                "PortDiscovery",
                applicationBuilder => { },
                applicationBuilder => { applicationBuilder.UseMiddleware<PortDiscoveryMiddleware>(); },
                applicationBuilder => { }
            ));
        });
    }
}
