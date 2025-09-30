using CYarp.Tests.Infrastructure;
using System.Text;
using System.Text.Json;

namespace CYarp.Tests.SSE;

/// <summary>
/// Tests for Server-Sent Events functionality and cancellation
/// </summary>
public class ServerSentEventsTests : CYarpTestBase
{
    [Fact]
    public async Task SSE_BasicConnection_ShouldReceiveEvents()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        // Act
        using var stream = await GetSseStreamAsync("site1");
        using var reader = new StreamReader(stream);
        
        var events = new List<string>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        try
        {
            while (!cts.Token.IsCancellationRequested && events.Count < 3)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line) && line.StartsWith("data:"))
                {
                    events.Add(line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when timeout occurs
        }
        
        // Assert
        Assert.True(events.Count >= 2, $"Expected at least 2 events, got {events.Count}");
        
        foreach (var eventData in events)
        {
            Assert.Contains("Hello from site1", eventData);
            Assert.Contains("timestamp", eventData);
        }
    }

    [Fact]
    public async Task SSE_EarlyCancellation_ShouldCloseCleanly()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        // Act & Assert - Connection should close cleanly when cancelled early
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        
        Exception? caughtException = null;
        try
        {
            using var stream = await GetSseStreamAsync("site1");
            using var reader = new StreamReader(stream);
            
            var events = new List<string>();
            while (!cts.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line) && line.StartsWith("data:"))
                {
                    events.Add(line);
                }
            }
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }
        
        // Should not throw unhandled exceptions when cancelling SSE
        if (caughtException != null && !(caughtException is OperationCanceledException))
        {
            Assert.Fail($"Unexpected exception during SSE cancellation: {caughtException}");
        }
    }

    [Fact]
    public async Task SSE_MultipleParallelConnections_ShouldAllWork()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        // Act - Create multiple parallel SSE connections
        var connectionTasks = Enumerable.Range(1, 3).Select(async i =>
        {
            using var stream = await GetSseStreamAsync("site1");
            using var reader = new StreamReader(stream);
            
            var events = new List<string>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            
            try
            {
                while (!cts.Token.IsCancellationRequested && events.Count < 2)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line) && line.StartsWith("data:"))
                    {
                        events.Add(line);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            
            return new { ConnectionId = i, EventCount = events.Count };
        });
        
        var results = await Task.WhenAll(connectionTasks);
        
        // Assert
        Assert.All(results, r => Assert.True(r.EventCount >= 1, $"Connection {r.ConnectionId} should receive at least 1 event"));
    }

    [Fact]
    public async Task SSE_WithStandardRequests_ShouldNotInterfere()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        // Act - Start SSE stream and make standard requests in parallel
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        var sseTask = Task.Run(async () =>
        {
            using var stream = await GetSseStreamAsync("site1");
            using var reader = new StreamReader(stream);
            
            var events = new List<string>();
            try
            {
                while (!cts.Token.IsCancellationRequested && events.Count < 3)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line) && line.StartsWith("data:"))
                    {
                        events.Add(line);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            return events.Count;
        });
        
        var standardRequestTasks = Enumerable.Range(1, 5).Select(async i =>
        {
            await Task.Delay(i * 200); // Stagger requests
            var response = await SendRequestAsync("site1", "/api/test");
            return response.IsSuccessStatusCode;
        });
        
        // Wait for both SSE and standard requests
        await Task.WhenAll(sseTask.ContinueWith(_ => { }));
        var requestResults = await Task.WhenAll(standardRequestTasks);
        
        // Assert
        var sseEventCount = await sseTask;
        Assert.True(sseEventCount >= 2, $"SSE should receive events, got {sseEventCount}");
        Assert.All(requestResults, success => Assert.True(success, "All standard requests should succeed"));
    }

    [Fact]
    public async Task SSE_MultipleClients_ShouldBeIndependent()
    {
        // Arrange
        var site1 = AddBackendServer("site1");
        var site2 = AddBackendServer("site2");
        await Task.Delay(2000);
        
        // Act - Start SSE on both clients
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        
        var sseTask1 = Task.Run(async () =>
        {
            using var stream = await GetSseStreamAsync("site1");
            using var reader = new StreamReader(stream);
            
            var events = new List<string>();
            try
            {
                while (!cts.Token.IsCancellationRequested && events.Count < 2)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line) && line.StartsWith("data:"))
                    {
                        events.Add(line);
                    }
                }
            }
            catch (OperationCanceledException) { }
            return events;
        });
        
        var sseTask2 = Task.Run(async () =>
        {
            using var stream = await GetSseStreamAsync("site2");
            using var reader = new StreamReader(stream);
            
            var events = new List<string>();
            try
            {
                while (!cts.Token.IsCancellationRequested && events.Count < 2)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line) && line.StartsWith("data:"))
                    {
                        events.Add(line);
                    }
                }
            }
            catch (OperationCanceledException) { }
            return events;
        });
        
        await Task.WhenAll(sseTask1.ContinueWith(_ => { }), sseTask2.ContinueWith(_ => { }));
        
        var site1Events = await sseTask1;
        var site2Events = await sseTask2;
        
        // Assert
        Assert.True(site1Events.Count >= 1, "Site1 should receive SSE events");
        Assert.True(site2Events.Count >= 1, "Site2 should receive SSE events");
        
        Assert.All(site1Events, e => Assert.Contains("Hello from site1", e));
        Assert.All(site2Events, e => Assert.Contains("Hello from site2", e));
    }

    [Fact]
    public async Task SSE_RequestAbortedCancellation_ShouldWork()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        // Act - Test that HttpContext.RequestAborted works properly
        var startTime = DateTime.UtcNow;
        using var cts = new CancellationTokenSource();
        
        var sseTask = Task.Run(async () =>
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sse");
                request.Headers.Add("Host", "site1.test.com");
                request.Headers.Add("Accept", "text/event-stream");
                
                using var response = await ProxyClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                
                var events = new List<string>();
                while (!cts.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line) && line.StartsWith("data:"))
                    {
                        events.Add(line);
                        if (events.Count >= 2)
                        {
                            // Cancel after receiving some events
                            cts.Cancel();
                        }
                    }
                }
                return events.Count;
            }
            catch (OperationCanceledException)
            {
                return -1; // Indicates proper cancellation
            }
        });
        
        var result = await sseTask;
        var endTime = DateTime.UtcNow;
        
        // Assert
        Assert.True(result == -1 || result >= 2, "SSE should be cancelled properly or receive events before cancellation");
        
        // Should not take too long due to proper cancellation
        var elapsed = endTime - startTime;
        Assert.True(elapsed.TotalSeconds < 10, $"SSE cancellation should be fast, took {elapsed.TotalSeconds} seconds");
    }
}