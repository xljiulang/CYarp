using CYarp.Client;
using CYarp.Client.AspNetCore;
using CYarp.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;

namespace CYarp.Tests.Integration;

/// <summary>
/// Base class for integration tests with real server and client connections
/// </summary>
public class RealConnectionTestBase : IAsyncLifetime
{
    protected WebApplication? ReverseProxyApp;
    protected IClientViewer? ClientViewer;
    protected readonly List<WebApplication> BackendApps = new();
    protected readonly List<Task> ClientTasks = new();
    protected readonly List<CancellationTokenSource> ClientCancellationTokens = new();
    protected HttpClient? ProxyClient;
    private bool _disposing = false;
    
    protected const int ReverseProxyPort = 15000;
    protected const int BackendSite1Port = 15001;
    protected const int BackendSite2Port = 15002;
    
    public virtual async Task InitializeAsync()
    {
        // Initialization happens in individual tests
        await Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        _disposing = true;
        
        // Stop all clients
        foreach (var cts in ClientCancellationTokens)
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        }
        
        // Give a moment for graceful shutdown
        await Task.Delay(100);
        
        try
        {
            await Task.WhenAll(ClientTasks.Where(t => t != null));
        }
        catch
        {
            // Ignore cancellation exceptions during cleanup
        }
        
        foreach (var cts in ClientCancellationTokens)
        {
            cts.Dispose();
        }
        
        // Dispose HTTP client
        ProxyClient?.Dispose();
        
        // Stop all backend apps
        foreach (var app in BackendApps)
        {
            try
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        // Stop reverse proxy
        if (ReverseProxyApp != null)
        {
            try
            {
                await ReverseProxyApp.StopAsync();
                await ReverseProxyApp.DisposeAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    protected async Task<WebApplication> StartReverseProxyAsync()
    {
        var builder = WebApplication.CreateBuilder();
        
        builder.WebHost.UseUrls($"http://localhost:{ReverseProxyPort}");
        
        builder.Services.AddCYarp();
        
        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        var app = builder.Build();
        
        // Get ClientViewer for checking connection status
        ClientViewer = app.Services.GetRequiredService<IClientViewer>();
        
        app.UseCYarp();
        app.MapCYarp((HttpContext context) =>
        {
            // Extract client ID from HOST header sent during CYarp connection
            // The client sends its ID in the HOST header when connecting
            var host = context.Request.Headers["HOST"].ToString();
            return ValueTask.FromResult<string?>(host);
        });
        
        // Add catch-all route to forward HTTP requests through CYarp tunnels
        app.Map("/{**path}", async (HttpContext context, IClientViewer clientViewer) =>
        {
            // Extract client ID from HOST header (same as CYarp connection)
            var clientId = context.Request.Headers["HOST"].ToString();
            
            if (!string.IsNullOrEmpty(clientId) && clientViewer.TryGetValue(clientId, out var client))
            {
                await client.ForwardHttpAsync(context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        });

        await app.StartAsync();
        ReverseProxyApp = app;
        return app;
    }

    protected async Task<WebApplication> StartBackendSiteAsync(string siteId, int port, Action<WebApplication>? configureEndpoints = null)
    {
        var builder = WebApplication.CreateBuilder();
        
        builder.Services.Configure<CYarpEndPoint>(options =>
        {
            options.ServerUri = new Uri($"http://localhost:{ReverseProxyPort}");
            options.TargetUri = new Uri($"http://localhost:{port}");
            options.ConnectHeaders["HOST"] = siteId;
        });
        
        builder.Services.AddCYarpListener();
        
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            // Only listen on regular HTTP port - the CYarp client will be started separately
            kestrel.ListenLocalhost(port);
        });
        
        builder.Services.AddSignalR();
        
        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        var app = builder.Build();
        
        // Default endpoints
        app.MapGet("/api/test", async (HttpContext context) =>
        {
            await context.Response.WriteAsJsonAsync(new { Message = $"Hello from {siteId}", Timestamp = DateTime.UtcNow });
        });
        
        app.MapGet("/api/long-operation", async (HttpContext context) =>
        {
            var duration = int.TryParse(context.Request.Query["duration"], out var d) ? d : 1000;
            await Task.Delay(duration, context.RequestAborted);
            await context.Response.WriteAsJsonAsync(new { Message = "Operation completed", Duration = duration, ClientId = siteId });
        });
        
        app.MapGet("/sse", async (HttpContext context) =>
        {
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");
            
            var counter = 0;
            try
            {
                while (!context.RequestAborted.IsCancellationRequested)
                {
                    counter++;
                    var message = $"data: {{\"counter\": {counter}, \"siteId\": \"{siteId}\", \"time\": \"{DateTime.Now:HH:mm:ss.fff}\"}}\n\n";
                    await context.Response.WriteAsync(message, context.RequestAborted);
                    await context.Response.Body.FlushAsync(context.RequestAborted);
                    await Task.Delay(100, context.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when client disconnects
            }
        });
        
        app.MapHub<TestHub>("/hubs/test");
        
        // Allow custom endpoint configuration
        configureEndpoints?.Invoke(app);

        await app.StartAsync();
        BackendApps.Add(app);
        return app;
    }

    protected async Task StartClientConnectionAsync(string siteId, int port)
    {
        var cts = new CancellationTokenSource();
        ClientCancellationTokens.Add(cts);
        
        var tcs = new TaskCompletionSource<bool>();
        
        var clientTask = Task.Run(async () =>
        {
            try
            {
                var options = new CYarpClientOptions
                {
                    ServerUri = new Uri($"http://localhost:{ReverseProxyPort}"),
                    TargetUri = new Uri($"http://localhost:{port}")
                };
                options.ConnectHeaders["HOST"] = siteId;
                
                using var httpHandler = new SocketsHttpHandler 
                { 
                    EnableMultipleHttp2Connections = true 
                };
                
                var loggerFactory = new LoggerFactory();
                loggerFactory.AddProvider(new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider());
                var logger = loggerFactory.CreateLogger<CYarpClient>();
                
                using var client = new CYarpClient(options, logger, httpHandler);
                
                // Start transport in background
                var transportTask = Task.Run(async () => 
                {
                    try
                    {
                        await client.TransportAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when test ends
                    }
                }, cts.Token);
                
                // Wait for connection to establish by checking ClientViewer
                for (int i = 0; i < 50; i++) // 5 seconds total
                {
                    if (cts.Token.IsCancellationRequested)
                        break;
                        
                    if (ClientViewer != null && ClientViewer.TryGetValue(siteId, out _))
                    {
                        tcs.TrySetResult(true);
                        break;
                    }
                    await Task.Delay(100, cts.Token);
                }
                
                if (!tcs.Task.IsCompleted)
                {
                    tcs.TrySetResult(false);
                }
                
                // Now wait for cancellation
                await transportTask;
            }
            catch (OperationCanceledException) when (_disposing || cts.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {siteId} error: {ex.Message}");
                tcs.TrySetException(ex);
            }
        }, cts.Token);
        
        ClientTasks.Add(clientTask);
        
        // Wait for connection confirmation or timeout
        var connected = await Task.WhenAny(tcs.Task, Task.Delay(10000)) == tcs.Task && await tcs.Task;
        
        if (!connected && !_disposing)
        {
            var clientCount = ClientViewer?.Count ?? -1;
            throw new TimeoutException($"Client {siteId} failed to connect within timeout. ClientViewer count: {clientCount}");
        }
        
        // Give a bit more time for full initialization
        if (connected)
        {
            await Task.Delay(300);
        }
    }

    protected HttpClient CreateProxyClient()
    {
        ProxyClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        return ProxyClient;
    }
}

/// <summary>
/// Test SignalR Hub
/// </summary>
public class TestHub : Hub
{
    private static readonly ConcurrentDictionary<string, List<string>> Groups = new();
    
    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
    
    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("ReceiveMessage", message);
    }
    
    public async Task JoinGroup(string groupName)
    {
        Groups.AddOrUpdate(
            groupName,
            _ => new List<string> { Context.ConnectionId },
            (_, list) => { list.Add(Context.ConnectionId); return list; }
        );
        await base.Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }
    
    public async Task LeaveGroup(string groupName)
    {
        if (Groups.TryGetValue(groupName, out var list))
        {
            list.Remove(Context.ConnectionId);
        }
        await base.Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
