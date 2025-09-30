using Microsoft.AspNetCore.SignalR.Client;

namespace CYarp.Tests.Integration;

/// <summary>
/// SignalR integration tests with real connections
/// </summary>
[Collection("Integration Tests")]
public class SignalRIntegrationTests : RealConnectionTestBase
{
    [Fact]
    public async Task SignalR_BasicConnection_ShouldWork()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var hubUrl = $"http://localhost:{ReverseProxyPort}/hubs/test";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.Headers.Add("Host", "site1.test.com");
            })
            .Build();
        
        // Act
        await connection.StartAsync();
        
        // Assert
        Assert.Equal(HubConnectionState.Connected, connection.State);
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task SignalR_SendMessage_ShouldReceive()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var hubUrl = $"http://localhost:{ReverseProxyPort}/hubs/test";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.Headers.Add("Host", "site1.test.com");
            })
            .Build();
        
        var receivedMessages = new List<string>();
        connection.On<string>("ReceiveMessage", message =>
        {
            receivedMessages.Add(message);
        });
        
        await connection.StartAsync();
        
        // Act
        await connection.InvokeAsync("SendMessage", "Hello SignalR");
        await Task.Delay(500);
        
        // Assert
        Assert.Contains("Hello SignalR", receivedMessages);
        
        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task SignalR_Groups_ShouldWorkCorrectly()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var hubUrl = $"http://localhost:{ReverseProxyPort}/hubs/test";
        
        var connection1 = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.Headers.Add("Host", "site1.test.com"); })
            .Build();
        
        var connection2 = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.Headers.Add("Host", "site1.test.com"); })
            .Build();
        
        var messages1 = new List<string>();
        var messages2 = new List<string>();
        
        connection1.On<string>("ReceiveMessage", msg => messages1.Add(msg));
        connection2.On<string>("ReceiveMessage", msg => messages2.Add(msg));
        
        await connection1.StartAsync();
        await connection2.StartAsync();
        
        // Act
        await connection1.InvokeAsync("JoinGroup", "TestGroup");
        await Task.Delay(200);
        
        await connection1.InvokeAsync("SendToGroup", "TestGroup", "Group message");
        await Task.Delay(500);
        
        // Assert
        Assert.Contains("Group message", messages1);
        Assert.DoesNotContain("Group message", messages2); // Connection2 is not in the group
        
        await connection1.StopAsync();
        await connection2.StopAsync();
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Fact]
    public async Task SignalR_MultipleClients_ShouldBeIndependent()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartBackendSiteAsync("site2", BackendSite2Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site2", BackendSite2Port);
        
        var hubUrl1 = $"http://localhost:{ReverseProxyPort}/hubs/test";
        var connection1 = new HubConnectionBuilder()
            .WithUrl(hubUrl1, options => { options.Headers.Add("Host", "site1.test.com"); })
            .Build();
        
        var hubUrl2 = $"http://localhost:{ReverseProxyPort}/hubs/test";
        var connection2 = new HubConnectionBuilder()
            .WithUrl(hubUrl2, options => { options.Headers.Add("Host", "site2.test.com"); })
            .Build();
        
        var messages1 = new List<string>();
        var messages2 = new List<string>();
        
        connection1.On<string>("ReceiveMessage", msg => messages1.Add(msg));
        connection2.On<string>("ReceiveMessage", msg => messages2.Add(msg));
        
        // Act
        await connection1.StartAsync();
        await connection2.StartAsync();
        
        await connection1.InvokeAsync("SendMessage", "From site1");
        await connection2.InvokeAsync("SendMessage", "From site2");
        await Task.Delay(500);
        
        // Assert
        Assert.Contains("From site1", messages1);
        Assert.Contains("From site2", messages2);
        
        await connection1.StopAsync();
        await connection2.StopAsync();
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    [Fact]
    public async Task SignalR_WithSSE_BothShouldWork()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var hubUrl = $"http://localhost:{ReverseProxyPort}/hubs/test";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.Headers.Add("Host", "site1.test.com"); })
            .Build();
        
        var signalRMessages = new List<string>();
        connection.On<string>("ReceiveMessage", msg => signalRMessages.Add(msg));
        
        await connection.StartAsync();
        
        // Act - Start SSE stream
        var sseClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        sseClient.DefaultRequestHeaders.Add("HOST", "site1");
        
        var sseTask = Task.Run(async () =>
        {
            var response = await sseClient.GetAsync("/sse", HttpCompletionOption.ResponseHeadersRead);
            var stream = await response.Content.ReadAsStreamAsync();
            var reader = new StreamReader(stream);
            
            var events = 0;
            for (int i = 0; i < 3; i++)
            {
                var line = await reader.ReadLineAsync();
                if (line != null && line.StartsWith("data:"))
                {
                    events++;
                }
            }
            return events;
        });
        
        // Send SignalR messages while SSE is running
        await Task.Delay(200);
        await connection.InvokeAsync("SendMessage", "SignalR message 1");
        await Task.Delay(200);
        await connection.InvokeAsync("SendMessage", "SignalR message 2");
        
        var sseEvents = await sseTask;
        await Task.Delay(200);
        
        // Assert
        Assert.True(sseEvents >= 3, $"Expected at least 3 SSE events, got {sseEvents}");
        Assert.Contains("SignalR message 1", signalRMessages);
        Assert.Contains("SignalR message 2", signalRMessages);
        
        await connection.StopAsync();
        await connection.DisposeAsync();
        sseClient.Dispose();
    }

    [Fact]
    public async Task SignalR_WithStandardRequests_AllShouldWork()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var hubUrl = $"http://localhost:{ReverseProxyPort}/hubs/test";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.Headers.Add("Host", "site1.test.com"); })
            .Build();
        
        var signalRMessages = new List<string>();
        connection.On<string>("ReceiveMessage", msg => signalRMessages.Add(msg));
        
        await connection.StartAsync();
        
        // Act - Make standard HTTP requests
        var httpClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        httpClient.DefaultRequestHeaders.Add("HOST", "site1");
        
        var httpResponses = new List<HttpResponseMessage>();
        for (int i = 0; i < 3; i++)
        {
            httpResponses.Add(await httpClient.GetAsync("/api/test"));
            await connection.InvokeAsync("SendMessage", $"Message {i}");
            await Task.Delay(100);
        }
        
        await Task.Delay(300);
        
        // Assert
        Assert.All(httpResponses, r => Assert.True(r.IsSuccessStatusCode));
        Assert.True(signalRMessages.Count >= 3, $"Expected at least 3 SignalR messages, got {signalRMessages.Count}");
        
        await connection.StopAsync();
        await connection.DisposeAsync();
        httpClient.Dispose();
    }
}
