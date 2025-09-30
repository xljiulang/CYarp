using System.Text.Json;

namespace CYarp.Tests.Integration;

/// <summary>
/// Multi-client integration tests with real connections
/// </summary>
[Collection("Integration Tests")]
public class MultiClientIntegrationTests : RealConnectionTestBase
{
    [Fact]
    public async Task MultipleClients_ShouldBeIndependent()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartBackendSiteAsync("site2", BackendSite2Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site2", BackendSite2Port);
        
        // Act
        var client1 = CreateProxyClient();
        client1.DefaultRequestHeaders.Add("HOST", "site1");
        var response1 = await client1.GetAsync("/api/test");
        var content1 = await response1.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<JsonElement>(content1);
        
        var client2 = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        client2.DefaultRequestHeaders.Add("HOST", "site2");
        var response2 = await client2.GetAsync("/api/test");
        var content2 = await response2.Content.ReadAsStringAsync();
        var result2 = JsonSerializer.Deserialize<JsonElement>(content2);
        
        // Assert
        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Equal("Hello from site1", result1.GetProperty("message").GetString());
        Assert.Equal("Hello from site2", result2.GetProperty("message").GetString());
    }

    [Fact]
    public async Task MultipleClients_ParallelRequests_ShouldRouteCorrectly()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartBackendSiteAsync("site2", BackendSite2Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site2", BackendSite2Port);
        
        var client1 = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        client1.DefaultRequestHeaders.Add("HOST", "site1");
        
        var client2 = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        client2.DefaultRequestHeaders.Add("HOST", "site2");
        
        // Act
        var tasks = new List<Task<(string siteId, HttpResponseMessage response)>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(client1.GetAsync("/api/test").ContinueWith(t => ("site1", t.Result)));
            tasks.Add(client2.GetAsync("/api/test").ContinueWith(t => ("site2", t.Result)));
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.True(r.response.IsSuccessStatusCode));
        
        // Verify each response came from the correct site
        foreach (var (siteId, response) in results)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.Contains(siteId, result.GetProperty("message").GetString());
        }
        
        client1.Dispose();
        client2.Dispose();
    }

    [Fact]
    public async Task MultipleClients_OneDisconnects_OtherShouldContinueWorking()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartBackendSiteAsync("site2", BackendSite2Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site2", BackendSite2Port);
        
        var client = CreateProxyClient();
        
        // Helper to make request with specific HOST header
        async Task<HttpResponseMessage> GetWithHost(string host, string path)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("HOST", host);
            return await client.SendAsync(request);
        }
        
        // Verify both work
        var response1 = await GetWithHost("site1", "/api/test");
        Assert.True(response1.IsSuccessStatusCode);
        
        var response2 = await GetWithHost("site2", "/api/test");
        Assert.True(response2.IsSuccessStatusCode);
        
        // Act - Disconnect site1
        ClientCancellationTokens[0].Cancel();
        await Task.Delay(500);
        
        // Assert - site2 should still work
        var response3 = await GetWithHost("site2", "/api/test");
        Assert.True(response3.IsSuccessStatusCode);
    }

    [Fact]
    public async Task MultipleClients_LoadBalancing_ShouldDistributeRequests()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartBackendSiteAsync("site2", BackendSite2Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site2", BackendSite2Port);
        
        var client1 = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        client1.DefaultRequestHeaders.Add("HOST", "site1");
        
        var client2 = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        client2.DefaultRequestHeaders.Add("HOST", "site2");
        
        // Act - Send 20 requests total (10 to each)
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(client1.GetAsync("/api/test"));
            tasks.Add(client2.GetAsync("/api/test"));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert
        Assert.Equal(20, responses.Length);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));
        
        client1.Dispose();
        client2.Dispose();
    }
}
