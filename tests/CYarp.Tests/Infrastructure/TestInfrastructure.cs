using CYarp.Client.AspNetCore;
using CYarp.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CYarp.Tests.Infrastructure;

/// <summary>
/// Test server that acts as the reverse proxy (CYarp Server)
/// </summary>
public class TestReverseProxy : WebApplicationFactory<Program>
{
    private readonly int _port;

    public TestReverseProxy(int port = 0)
    {
        _port = port;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddCYarp();
            services.Configure<CYarpOptions>(options =>
            {
                // Configure test options
            });
            services.AddSignalR();
        });

        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseCYarp();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapCYarp<TestClientIdProvider>();
                endpoints.MapHub<TestSignalRHub>("/signalr");
                
                // Fallback for testing
                endpoints.Map("/{**any}", async (HttpContext context, IClientViewer clientViewer) =>
                {
                    var host = context.Request.Headers.Host.ToString();
                    var clientId = ExtractClientIdFromHost(host);
                    
                    if (clientViewer.TryGetValue(clientId, out var client))
                    {
                        await client.ForwardHttpAsync(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 502; // Bad Gateway
                        await context.Response.WriteAsync($"No client found for host: {host}");
                    }
                });
            });
        });

        if (_port > 0)
        {
            builder.UseUrls($"http://localhost:{_port}");
        }
    }

    private static string ExtractClientIdFromHost(string host)
    {
        // Extract client ID from host (e.g., "site1.test.com" -> "site1")
        var parts = host.Split('.');
        return parts.Length > 0 ? parts[0] : "default";
    }
}

/// <summary>
/// Test client ID provider
/// </summary>
public class TestClientIdProvider : IClientIdProvider
{
    public ValueTask<string?> GetClientIdAsync(HttpContext context)
    {
        var host = context.Request.Headers.Host.ToString();
        var clientId = host.Split('.')[0];
        return ValueTask.FromResult<string?>(clientId);
    }
}

/// <summary>
/// Test SignalR Hub
/// </summary>
public class TestSignalRHub : Hub
{
    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", Context.ConnectionId, message);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserJoined", Context.ConnectionId);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserLeft", Context.ConnectionId);
    }
}

/// <summary>
/// Test backend server (acts as the target server behind CYarp Client)
/// </summary>
public class TestBackendServer : WebApplicationFactory<Program>
{
    private readonly string _clientId;
    private readonly int _port;

    public TestBackendServer(string clientId, int port = 0)
    {
        _clientId = clientId;
        _port = port;
    }

    public string ClientId => _clientId;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.Configure<CYarpEndPoint>(options =>
            {
                options.ServerUri = new Uri("ws://localhost:5000/cyarp");
                options.TargetUri = new Uri("http://localhost");
                options.ConnectHeaders = new Dictionary<string, string>
                {
                    ["Host"] = $"{_clientId}.test.com"
                };
            });
            services.AddCYarpListener();
            services.AddSignalR();
        });

        builder.ConfigureKestrel(kestrel =>
        {
            var endPoint = new CYarpEndPoint
            {
                ServerUri = new Uri("ws://localhost:5000/cyarp"),
                TargetUri = new Uri("http://localhost"),
                ConnectHeaders = new Dictionary<string, string>
                {
                    ["Host"] = $"{_clientId}.test.com"
                }
            };
            kestrel.ListenCYarp(endPoint);
        });

        builder.Configure(app =>
        {
            app.UseRouting();
            
            app.UseEndpoints(endpoints =>
            {
                // Standard REST endpoint
                endpoints.MapGet("/api/test", () => new { Message = "Hello from " + _clientId, Timestamp = DateTime.UtcNow });
                
                // Long-running operation
                endpoints.MapGet("/api/long-operation", async (HttpContext context) =>
                {
                    var duration = context.Request.Query["duration"].FirstOrDefault();
                    var delayMs = int.TryParse(duration, out var d) ? d : 5000;
                    
                    try
                    {
                        await Task.Delay(delayMs, context.RequestAborted);
                        await context.Response.WriteAsJsonAsync(new { Message = "Operation completed", Duration = delayMs });
                    }
                    catch (OperationCanceledException)
                    {
                        context.Response.StatusCode = 499; // Client Closed Request
                        await context.Response.WriteAsJsonAsync(new { Message = "Operation cancelled" });
                    }
                });

                // SSE endpoint
                endpoints.MapGet("/api/sse", async (HttpContext context) =>
                {
                    context.Response.Headers.Append("Content-Type", "text/event-stream");
                    context.Response.Headers.Append("Cache-Control", "no-cache");
                    context.Response.Headers.Append("Connection", "keep-alive");
                    
                    var counter = 0;
                    while (!context.RequestAborted.IsCancellationRequested)
                    {
                        counter++;
                        var data = $"data: {{\"id\":{counter},\"message\":\"Hello from {_clientId}\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n";
                        await context.Response.WriteAsync(data);
                        await context.Response.Body.FlushAsync();
                        await Task.Delay(1000, context.RequestAborted);
                    }
                });

                // Parallel processing endpoint
                endpoints.MapPost("/api/parallel", async (HttpContext context) =>
                {
                    var tasks = Enumerable.Range(1, 5).Select(async i =>
                    {
                        await Task.Delay(1000 + i * 100, context.RequestAborted);
                        return new { TaskId = i, Message = $"Task {i} completed", ClientId = _clientId };
                    });

                    var results = await Task.WhenAll(tasks);
                    await context.Response.WriteAsJsonAsync(new { Results = results, TotalTime = DateTime.UtcNow });
                });

                // SignalR Hub
                endpoints.MapHub<TestSignalRHub>("/signalr");
            });
        });

        if (_port > 0)
        {
            builder.UseUrls($"http://localhost:{_port}");
        }
    }
}

/// <summary>
/// Base class for CYarp integration tests
/// </summary>
public abstract class CYarpTestBase : IDisposable
{
    protected TestReverseProxy ReverseProxy { get; }
    protected List<TestBackendServer> BackendServers { get; }
    protected HttpClient ProxyClient { get; }

    protected CYarpTestBase()
    {
        ReverseProxy = new TestReverseProxy(5000);
        BackendServers = new List<TestBackendServer>();
        ProxyClient = ReverseProxy.CreateClient();
    }

    protected TestBackendServer AddBackendServer(string clientId)
    {
        var server = new TestBackendServer(clientId);
        BackendServers.Add(server);
        return server;
    }

    protected async Task<HttpResponseMessage> SendRequestAsync(string clientId, string path, HttpMethod? method = null)
    {
        method ??= HttpMethod.Get;
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Host", $"{clientId}.test.com");
        return await ProxyClient.SendAsync(request);
    }

    protected async Task<Stream> GetSseStreamAsync(string clientId, string path = "/api/sse")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Host", $"{clientId}.test.com");
        request.Headers.Add("Accept", "text/event-stream");
        
        var response = await ProxyClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        return await response.Content.ReadAsStreamAsync();
    }

    public virtual void Dispose()
    {
        ProxyClient?.Dispose();
        ReverseProxy?.Dispose();
        BackendServers?.ForEach(s => s.Dispose());
    }
}

/// <summary>
/// Dummy Program class for WebApplicationFactory
/// </summary>
public class Program
{
}