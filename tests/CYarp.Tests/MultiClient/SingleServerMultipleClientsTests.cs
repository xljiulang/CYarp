using CYarp.Tests.Infrastructure;
using System.Text.Json;

namespace CYarp.Tests.MultiClient;

/// <summary>
/// Tests for single server, multiple clients scenarios
/// </summary>
public class SingleServerMultipleClientsTests : CYarpTestBase
{
    [Fact]
    public async Task MultipleClients_IndependentRequests_ShouldAllSucceed()
    {
        // Arrange
        var site1 = AddBackendServer("site1");
        var site2 = AddBackendServer("site2");
        var site3 = AddBackendServer("site3");
        
        await Task.Delay(2000); // Wait for all servers to start
        
        // Act
        var tasks = new[]
        {
            SendRequestAsync("site1", "/api/test"),
            SendRequestAsync("site2", "/api/test"),
            SendRequestAsync("site3", "/api/test")
        };
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        
        var contents = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));
        var results = contents.Select(c => JsonSerializer.Deserialize<JsonElement>(c)).ToArray();
        
        Assert.Equal("Hello from site1", results[0].GetProperty("Message").GetString());
        Assert.Equal("Hello from site2", results[1].GetProperty("Message").GetString());
        Assert.Equal("Hello from site3", results[2].GetProperty("Message").GetString());
    }

    [Fact]
    public async Task MultipleClients_ParallelRequests_ShouldAllSucceed()
    {
        // Arrange
        var site1 = AddBackendServer("site1");
        var site2 = AddBackendServer("site2");
        
        await Task.Delay(2000);
        
        // Act - Send 5 parallel requests to each client
        var site1Tasks = Enumerable.Range(1, 5).Select(_ => SendRequestAsync("site1", "/api/test"));
        var site2Tasks = Enumerable.Range(1, 5).Select(_ => SendRequestAsync("site2", "/api/test"));
        
        var allTasks = site1Tasks.Concat(site2Tasks);
        var responses = await Task.WhenAll(allTasks);
        
        // Assert
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        Assert.Equal(10, responses.Length);
    }

    [Fact]
    public async Task MultipleClients_MixedOperations_ShouldAllSucceed()
    {
        // Arrange
        var site1 = AddBackendServer("site1");
        var site2 = AddBackendServer("site2");
        
        await Task.Delay(2000);
        
        // Act - Mix of operations across clients
        var tasks = new[]
        {
            SendRequestAsync("site1", "/api/test"),
            SendRequestAsync("site2", "/api/long-operation?duration=1000"),
            SendRequestAsync("site1", "/api/parallel", HttpMethod.Post),
            SendRequestAsync("site2", "/api/test"),
            SendRequestAsync("site1", "/api/long-operation?duration=500")
        };
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
    }

    [Fact]
    public async Task MultipleClients_OneFailsOthersSucceed_ShouldBeIndependent()
    {
        // Arrange
        var site1 = AddBackendServer("site1");
        var site2 = AddBackendServer("site2");
        
        await Task.Delay(2000);
        
        // Act
        var tasks = new[]
        {
            SendRequestAsync("site1", "/api/test"),
            SendRequestAsync("site2", "/api/test"),
            SendRequestAsync("nonexistent", "/api/test"), // This should fail
            SendRequestAsync("site1", "/api/test"),
            SendRequestAsync("site2", "/api/test")
        };
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert
        Assert.True(responses[0].IsSuccessStatusCode); // site1
        Assert.True(responses[1].IsSuccessStatusCode); // site2
        Assert.Equal(502, (int)responses[2].StatusCode); // nonexistent - Bad Gateway
        Assert.True(responses[3].IsSuccessStatusCode); // site1
        Assert.True(responses[4].IsSuccessStatusCode); // site2
    }

    [Fact]
    public async Task MultipleClients_LongRunningOperations_ShouldBeIndependent()
    {
        // Arrange
        var site1 = AddBackendServer("site1");
        var site2 = AddBackendServer("site2");
        
        await Task.Delay(2000);
        
        // Act - Start long operations on both clients
        var startTime = DateTime.UtcNow;
        
        var tasks = new[]
        {
            SendRequestAsync("site1", "/api/long-operation?duration=3000"),
            SendRequestAsync("site2", "/api/long-operation?duration=2000"),
            SendRequestAsync("site1", "/api/test"), // Quick operation should not be blocked
            SendRequestAsync("site2", "/api/test")  // Quick operation should not be blocked
        };
        
        var responses = await Task.WhenAll(tasks);
        var endTime = DateTime.UtcNow;
        
        // Assert
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        
        // The quick operations should not be significantly delayed by the long operations
        var totalTime = endTime - startTime;
        Assert.True(totalTime.TotalMilliseconds < 5000); // Should be around 3 seconds max
    }

    [Fact]
    public async Task MultipleClients_LoadBalancing_ShouldDistributeRequests()
    {
        // Arrange
        var site1 = AddBackendServer("site1");
        var site2 = AddBackendServer("site2");
        var site3 = AddBackendServer("site3");
        
        await Task.Delay(2000);
        
        // Act - Send many requests to different clients
        var tasks = new List<Task<HttpResponseMessage>>();
        
        for (int i = 0; i < 30; i++)
        {
            var clientId = $"site{(i % 3) + 1}";
            tasks.Add(SendRequestAsync(clientId, "/api/test"));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
        Assert.Equal(30, responses.Length);
        
        // Verify each client received exactly 10 requests
        var contents = await Task.WhenAll(responses.Select(r => r.Content.ReadAsStringAsync()));
        var site1Count = contents.Count(c => c.Contains("Hello from site1"));
        var site2Count = contents.Count(c => c.Contains("Hello from site2"));
        var site3Count = contents.Count(c => c.Contains("Hello from site3"));
        
        Assert.Equal(10, site1Count);
        Assert.Equal(10, site2Count);
        Assert.Equal(10, site3Count);
    }
}