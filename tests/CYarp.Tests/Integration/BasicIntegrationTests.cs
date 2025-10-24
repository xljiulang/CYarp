using System.Net;
using System.Text.Json;

namespace CYarp.Tests.Integration;

/// <summary>
/// Collection definition to prevent parallel execution of integration tests
/// </summary>
[CollectionDefinition("Integration Tests", DisableParallelization = true)]
public class IntegrationTestCollection
{
}

/// <summary>
/// Basic integration tests with real server and client connections
/// </summary>
[Collection("Integration Tests")]
public class BasicIntegrationTests : RealConnectionTestBase
{
    [Fact]
    public async Task SingleServerSingleClient_StandardRequest_ShouldWork()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        // Use the same client ID as registered
        client.DefaultRequestHeaders.Add("HOST", "site1");
        
        // Act
        var response = await client.GetAsync("/api/test");
        
        // Debug output
        Console.WriteLine($"Response StatusCode: {response.StatusCode}");
        Console.WriteLine($"Response Content: {await response.Content.ReadAsStringAsync()}");
        
        // Assert
        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.Equal("Hello from site1", result.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SingleServerSingleClient_MultipleParallelRequests_ShouldAllSucceed()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "site1");
        
        // Act
        var tasks = Enumerable.Range(1, 10).Select(i => client.GetAsync("/api/test"));
        var responses = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        Assert.Equal(10, responses.Length);
    }

    [Fact]
    public async Task SingleServerSingleClient_LongRunningOperation_ShouldComplete()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "site1");
        
        // Act
        var response = await client.GetAsync("/api/long-operation?duration=2000");
        
        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.Equal("Operation completed", result.GetProperty("message").GetString());
        Assert.Equal(2000, result.GetProperty("duration").GetInt32());
    }

    [Fact]
    public async Task SingleServerSingleClient_LongRunningOperationWithCancellation_ShouldBeCancelled()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "site1");
        client.Timeout = TimeSpan.FromSeconds(1);
        
        // Act & Assert
        await Assert.ThrowsAnyAsync<TaskCanceledException>(async () =>
        {
            await client.GetAsync("/api/long-operation?duration=5000");
        });
    }

    [Fact]
    public async Task NonExistentClient_ShouldReturn502()
    {
        // Arrange
        await StartReverseProxyAsync();
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "nonexistent");
        
        // Act
        var response = await client.GetAsync("/api/test");
        
        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
