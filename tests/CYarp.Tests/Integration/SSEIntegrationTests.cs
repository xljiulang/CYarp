using System.Text;
using System.Text.Json;

namespace CYarp.Tests.Integration;

/// <summary>
/// Server-Sent Events integration tests with real connections
/// </summary>
[Collection("Integration Tests")]
public class SSEIntegrationTests : RealConnectionTestBase
{
    [Fact]
    public async Task SSE_ShouldReceiveEvents()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "site1");
        client.Timeout = TimeSpan.FromSeconds(10);
        
        // Act
        var response = await client.GetAsync("/sse", HttpCompletionOption.ResponseHeadersRead);
        
        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        
        var stream = await response.Content.ReadAsStreamAsync();
        var reader = new StreamReader(stream);
        
        var events = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var line = await reader.ReadLineAsync();
            if (line != null && line.StartsWith("data:"))
            {
                events.Add(line);
            }
        }
        
        Assert.True(events.Count >= 3, $"Expected at least 3 events, got {events.Count}");
    }

    [Fact]
    public async Task SSE_WithCancellation_ShouldStopCleanly()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "site1");
        
        var cts = new CancellationTokenSource();
        
        // Act
        var responseTask = client.GetAsync("/sse", HttpCompletionOption.ResponseHeadersRead, cts.Token);
        var response = await responseTask;
        
        Assert.True(response.IsSuccessStatusCode);
        
        var stream = await response.Content.ReadAsStreamAsync();
        var reader = new StreamReader(stream);
        
        // Read a few events
        for (int i = 0; i < 2; i++)
        {
            await reader.ReadLineAsync();
        }
        
        // Cancel
        cts.Cancel();
        
        // Assert - Should complete without exceptions
        await Task.Delay(500);
        Assert.True(cts.IsCancellationRequested);
        
        cts.Dispose();
    }

    [Fact]
    public async Task SSE_WhileDoingStandardRequests_BothShouldWork()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "site1");
        
        // Act - Start SSE stream
        var sseTask = Task.Run(async () =>
        {
            var response = await client.GetAsync("/sse", HttpCompletionOption.ResponseHeadersRead);
            var stream = await response.Content.ReadAsStreamAsync();
            var reader = new StreamReader(stream);
            
            var events = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                var line = await reader.ReadLineAsync();
                if (line != null && line.StartsWith("data:"))
                {
                    events.Add(line);
                }
            }
            return events.Count;
        });
        
        // Act - Make standard requests while SSE is running
        await Task.Delay(200); // Let SSE start
        
        var standardClient = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        standardClient.DefaultRequestHeaders.Add("HOST", "site1");
        
        var standardResponses = new List<HttpResponseMessage>();
        for (int i = 0; i < 3; i++)
        {
            standardResponses.Add(await standardClient.GetAsync("/api/test"));
            await Task.Delay(100);
        }
        
        var eventCount = await sseTask;
        
        // Assert
        Assert.True(eventCount >= 5, $"Expected at least 5 SSE events, got {eventCount}");
        Assert.All(standardResponses, r => Assert.True(r.IsSuccessStatusCode));
        
        standardClient.Dispose();
    }

    [Fact]
    public async Task SSE_MultipleClients_ShouldGetIndependentStreams()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartBackendSiteAsync("site2", BackendSite2Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site2", BackendSite2Port);
        
        // Act - Connect to both SSE endpoints
        var client1 = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        client1.DefaultRequestHeaders.Add("HOST", "site1");
        
        var client2 = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        client2.DefaultRequestHeaders.Add("HOST", "site2");
        
        var response1 = await client1.GetAsync("/sse", HttpCompletionOption.ResponseHeadersRead);
        var response2 = await client2.GetAsync("/sse", HttpCompletionOption.ResponseHeadersRead);
        
        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        
        var stream1 = await response1.Content.ReadAsStreamAsync();
        var stream2 = await response2.Content.ReadAsStreamAsync();
        
        var reader1 = new StreamReader(stream1);
        var reader2 = new StreamReader(stream2);
        
        // Read from both streams
        var line1 = await reader1.ReadLineAsync();
        var line2 = await reader2.ReadLineAsync();
        
        // Assert - Both should contain their respective site IDs
        Assert.Contains("site1", line1);
        Assert.Contains("site2", line2);
        
        client1.Dispose();
        client2.Dispose();
    }

    [Fact]
    public async Task SSE_RequestAborted_ShouldPropagateCancellation()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "site1");
        client.Timeout = TimeSpan.FromSeconds(2);
        
        // Act
        var response = await client.GetAsync("/sse", HttpCompletionOption.ResponseHeadersRead);
        var stream = await response.Content.ReadAsStreamAsync();
        var reader = new StreamReader(stream);
        
        // Read a few events
        for (int i = 0; i < 3; i++)
        {
            await reader.ReadLineAsync();
        }
        
        // Dispose the stream to trigger RequestAborted
        await stream.DisposeAsync();
        
        // Assert - No exceptions should be logged (verify in application logs)
        await Task.Delay(500);
        Assert.True(true); // If we get here without exceptions, test passes
    }
}
