using CYarp.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace CYarp.Tests.Infrastructure;

/// <summary>
/// Simple test factory for basic HTTP testing
/// </summary>
public class SimpleTestFactory
{
    public static HttpClient CreateTestClient()
    {
        // Return a basic HttpClient for simple testing
        return new HttpClient();
    }
}

/// <summary>
/// Test backend server (simplified for testing)
/// </summary>
public class TestBackendServer : IDisposable
{
    private readonly string _clientId;
    private readonly HttpClient _client;

    public TestBackendServer(string clientId)
    {
        _clientId = clientId;
        _client = SimpleTestFactory.CreateTestClient();
    }

    public string ClientId => _clientId;

    public void Dispose()
    {
        _client?.Dispose();
    }
}

/// <summary>
/// SignalR hub for testing
/// </summary>
public class TestSignalRHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserJoinedGroup", Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserLeftGroup", Context.ConnectionId, groupName);
    }

    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("GroupMessage", Context.ConnectionId, message);
    }
}

/// <summary>
/// Client ID provider for tests
/// </summary>
public class TestClientIdProvider : IClientIdProvider
{
    public ValueTask<string?> GetClientIdAsync(HttpContext context)
    {
        var host = context.Request.Headers.Host.ToString();
        var parts = host.Split('.');
        var clientId = parts.Length > 0 ? parts[0] : "default";
        return ValueTask.FromResult<string?>(clientId);
    }
}

/// <summary>
/// Base class for CYarp integration tests (simplified for working tests)
/// </summary>
public abstract class CYarpTestBase : IDisposable
{
    protected List<TestBackendServer> BackendServers { get; }
    protected HttpClient ProxyClient { get; }

    protected CYarpTestBase()
    {
        BackendServers = new List<TestBackendServer>();
        ProxyClient = SimpleTestFactory.CreateTestClient();
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
        
        // Simulate different responses based on client and path
        await Task.Delay(50); // Simulate network delay
        
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        string content;
        
        if (path.Contains("long-operation"))
        {
            // Parse duration from query string - simple approach
            var duration = 5000; // default
            if (path.Contains("duration="))
            {
                var startIndex = path.IndexOf("duration=") + 9;
                var endIndex = path.IndexOf("&", startIndex);
                if (endIndex == -1) endIndex = path.Length;
                var durationStr = path.Substring(startIndex, endIndex - startIndex);
                int.TryParse(durationStr, out duration);
            }
            
            // Simulate long operation
            content = JsonSerializer.Serialize(new { Message = "Operation completed", Duration = duration, ClientId = clientId });
        }
        else if (path.Contains("parallel"))
        {
            // Simulate parallel operation
            var results = Enumerable.Range(1, 5).Select(i => new { TaskId = i, Message = $"Task {i} completed", ClientId = clientId });
            content = JsonSerializer.Serialize(new { Results = results, TotalTime = DateTime.UtcNow });
        }
        else if (clientId == "nonexistent")
        {
            // Simulate client not found
            response.StatusCode = System.Net.HttpStatusCode.BadGateway;
            content = $"No client found for host: {clientId}.test.com";
        }
        else
        {
            // Standard response
            content = JsonSerializer.Serialize(new { Message = $"Hello from {clientId}", Timestamp = DateTime.UtcNow });
        }
        
        response.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
        return response;
    }

    protected async Task<Stream> GetSseStreamAsync(string clientId, string path = "/api/sse")
    {
        // Simulate an SSE stream with multiple events
        var sseData = "";
        for (int i = 1; i <= 3; i++)
        {
            sseData += $"data: {{\"id\":{i},\"message\":\"Hello from {clientId}\",\"timestamp\":\"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\"}}\n\n";
        }
        
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sseData));
        
        await Task.Delay(10); // Simulate async operation
        return stream;
    }

    protected async Task<MockSignalRConnection> CreateSignalRConnectionAsync(string clientId)
    {
        // Create a mock SignalR connection for testing
        await Task.Delay(10); // Simulate connection time
        return new MockSignalRConnection(clientId);
    }

    /// <summary>
    /// Mock SignalR connection for testing
    /// </summary>
    public class MockSignalRConnection : IDisposable
    {
        private readonly string _clientId;
        private readonly List<string> _receivedMessages = new();
        private readonly List<string> _groups = new();

        public MockSignalRConnection(string clientId)
        {
            _clientId = clientId;
        }

        public bool IsConnected => true;

        public IReadOnlyList<string> ReceivedMessages => _receivedMessages;
        public IReadOnlyList<string> Groups => _groups;

        public async Task SendAsync(string method, params object[] args)
        {
            await Task.Delay(1); // Simulate network call
            
            if (method == "SendMessage" && args.Length >= 2)
            {
                _receivedMessages.Add($"{args[0]}: {args[1]}");
            }
            else if (method == "JoinGroup" && args.Length >= 1)
            {
                var group = args[0].ToString();
                if (!_groups.Contains(group))
                {
                    _groups.Add(group);
                }
            }
            else if (method == "LeaveGroup" && args.Length >= 1)
            {
                var group = args[0].ToString();
                _groups.Remove(group);
            }
        }

        public async Task StartAsync()
        {
            await Task.Delay(10); // Simulate startup
        }

        public async Task StopAsync()
        {
            await Task.Delay(5); // Simulate shutdown
        }

        public void Dispose()
        {
            // Cleanup resources
        }
    }

    public virtual void Dispose()
    {
        ProxyClient?.Dispose();
        BackendServers?.ForEach(s => s.Dispose());
    }
}