using CYarp.Tests.Infrastructure;
using System.Text.Json;

namespace CYarp.Tests.Basic;

/// <summary>
/// Tests for single server, single client scenarios
/// </summary>
public class SingleServerSingleClientTests : CYarpTestBase
{
    [Fact]
    public async Task SingleRequest_StandardEndpoint_ShouldReturnSuccess()
    {
        // Arrange
        var server = AddBackendServer("site1");
        
        // Wait for server to start
        await Task.Delay(1000);
        
        // Act
        var response = await SendRequestAsync("site1", "/api/test");
        
        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.Equal("Hello from site1", result.GetProperty("Message").GetString());
    }

    [Fact]
    public async Task MultipleSequentialRequests_ShouldAllSucceed()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var response = await SendRequestAsync("site1", "/api/test");
            Assert.True(response.IsSuccessStatusCode);
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            Assert.Equal("Hello from site1", result.GetProperty("Message").GetString());
        }
    }

    [Fact]
    public async Task MultipleParallelRequests_ShouldAllSucceed()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        // Act
        var tasks = Enumerable.Range(1, 10).Select(async i =>
        {
            var response = await SendRequestAsync("site1", "/api/test");
            return new { Index = i, IsSuccess = response.IsSuccessStatusCode };
        });
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(results, r => Assert.True(r.IsSuccess));
    }

    [Fact]
    public async Task LongRunningOperation_ShouldComplete()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        // Act
        var response = await SendRequestAsync("site1", "/api/long-operation?duration=2000");
        
        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        Assert.Equal("Operation completed", result.GetProperty("Message").GetString());
        Assert.Equal(2000, result.GetProperty("Duration").GetInt32());
    }

    [Fact]
    public async Task LongRunningOperation_WithCancellation_ShouldBeCancelled()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        using var cts = new CancellationTokenSource();
        
        // Cancel after 1 second
        cts.CancelAfter(1000);
        
        try
        {
            // Act - Use SendRequestAsync helper which builds URI correctly
            var response = await SendRequestAsync("site1", "/api/long-operation?duration=10000", cts.Token);
            
            // If we get here, the operation wasn't cancelled as expected
            Assert.Fail("Operation should have been cancelled");
        }
        catch (OperationCanceledException)
        {
            // Expected - operation was cancelled
            Assert.True(true);
        }
    }

    [Fact]
    public async Task PostRequest_WithParallelProcessing_ShouldSucceed()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        // Act
        var response = await SendRequestAsync("site1", "/api/parallel", HttpMethod.Post);
        
        // Assert
        Assert.True(response.IsSuccessStatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);
        
        var results = result.GetProperty("Results").EnumerateArray().ToList();
        Assert.Equal(5, results.Count);
        
        foreach (var taskResult in results)
        {
            Assert.Equal("site1", taskResult.GetProperty("ClientId").GetString());
            Assert.Contains("Task", taskResult.GetProperty("Message").GetString());
        }
    }
}