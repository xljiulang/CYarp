using System.Text.Json;

namespace CYarp.Tests.Integration;

/// <summary>
/// Multi-client integration tests with real connections
/// </summary>
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
        
        var client = CreateProxyClient();
        
        // Act
        client.DefaultRequestHeaders.Host = "site1.test.com";
        var response1 = await client.GetAsync("/api/test");
        var content1 = await response1.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<JsonElement>(content1);
        
        client.DefaultRequestHeaders.Host = "site2.test.com";
        var response2 = await client.GetAsync("/api/test");
        var content2 = await response2.Content.ReadAsStringAsync();
        var result2 = JsonSerializer.Deserialize<JsonElement>(content2);
        
        // Assert
        Assert.True(response1.IsSuccessStatusCode);
        Assert.True(response2.IsSuccessStatusCode);
        Assert.Equal("Hello from site1", result1.GetProperty("Message").GetString());
        Assert.Equal("Hello from site2", result2.GetProperty("Message").GetString());
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
        client1.DefaultRequestHeaders.Host = "site1.test.com";
        
        var client2 = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        client2.DefaultRequestHeaders.Host = "site2.test.com";
        
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
            Assert.Contains(siteId, result.GetProperty("Message").GetString());
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
        
        // Verify both work
        client.DefaultRequestHeaders.Host = "site1.test.com";
        var response1 = await client.GetAsync("/api/test");
        Assert.True(response1.IsSuccessStatusCode);
        
        client.DefaultRequestHeaders.Host = "site2.test.com";
        var response2 = await client.GetAsync("/api/test");
        Assert.True(response2.IsSuccessStatusCode);
        
        // Act - Disconnect site1
        ClientCancellationTokens[0].Cancel();
        await Task.Delay(500);
        
        // Assert - site2 should still work
        client.DefaultRequestHeaders.Host = "site2.test.com";
        var response3 = await client.GetAsync("/api/test");
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
        client1.DefaultRequestHeaders.Host = "site1.test.com";
        
        var client2 = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
        client2.DefaultRequestHeaders.Host = "site2.test.com";
        
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
