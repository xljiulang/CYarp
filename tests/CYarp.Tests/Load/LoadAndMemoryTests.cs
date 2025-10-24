using System.Diagnostics;
using System.Text;
using CYarp.Tests.Integration;

namespace CYarp.Tests.Load;

/// <summary>
/// Load testing and memory monitoring tests - CRITICAL for 24/7 operation
/// These tests validate that CYarp can handle high load scenarios without memory leaks
/// </summary>
[Collection("Integration Tests")]
public class LoadAndMemoryTests : RealConnectionTestBase
{
    private const int KB = 1024;
    private const int MB = 1024 * KB;

    [Fact]
    public async Task HighLoad_ConcurrentRequests_ShouldNotLeakMemory()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "site1");
        
        // Capture initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);
        
        // Act - Simulate high load: 100 concurrent requests
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var response = await client.GetAsync("/api/test");
                    response.EnsureSuccessStatusCode();
                    await response.Content.ReadAsStringAsync();
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);
        
        // Assert - Memory growth should be reasonable (< 50MB for 1000 requests)
        var memoryGrowth = (finalMemory - initialMemory) / MB;
        Assert.True(memoryGrowth < 50, 
            $"Memory leak detected: {memoryGrowth}MB growth after 1000 requests. Initial: {initialMemory/MB}MB, Final: {finalMemory/MB}MB");
    }

    [Fact]
    public async Task SSE_LongRunning_ShouldNotLeakMemory()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "site1");
        
        // Capture initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);
        
        // Act - Start SSE stream and run for a while
        var cts = new CancellationTokenSource();
        var sseTask = Task.Run(async () =>
        {
            var response = await client.GetAsync("/sse", HttpCompletionOption.ResponseHeadersRead, cts.Token);
            var stream = await response.Content.ReadAsStreamAsync();
            var reader = new StreamReader(stream);
            
            int eventCount = 0;
            while (!cts.Token.IsCancellationRequested && eventCount < 100)
            {
                var line = await reader.ReadLineAsync();
                if (line != null && line.StartsWith("data:"))
                {
                    eventCount++;
                }
            }
            return eventCount;
        });
        
        // Let it run for ~10 seconds
        await Task.Delay(10000);
        cts.Cancel();
        var events = await sseTask;
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);
        
        // Assert
        Assert.True(events >= 10, $"Expected at least 10 events, got {events}");
        
        var memoryGrowth = (finalMemory - initialMemory) / MB;
        Assert.True(memoryGrowth < 20, 
            $"Memory leak in SSE detected: {memoryGrowth}MB growth. Initial: {initialMemory/MB}MB, Final: {finalMemory/MB}MB");
        
        cts.Dispose();
    }

    [Fact]
    public async Task SignalR_ManyConnections_ShouldNotLeakMemory()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        // Capture initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);
        
        // Act - Create and dispose many SignalR connections
        for (int i = 0; i < 50; i++)
        {
            var hubUrl = $"http://localhost:{ReverseProxyPort}/hubs/test";
            var connection = new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder()
                .WithUrl(hubUrl, options => { options.Headers.Add("Host", "site1"); })
                .Build();
            
            await connection.StartAsync();
            await connection.InvokeAsync("SendMessage", $"Test message {i}");
            await connection.StopAsync();
            await connection.DisposeAsync();
        }
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);
        
        // Assert
        var memoryGrowth = (finalMemory - initialMemory) / MB;
        Assert.True(memoryGrowth < 30, 
            $"Memory leak in SignalR detected: {memoryGrowth}MB growth after 50 connections. Initial: {initialMemory/MB}MB, Final: {finalMemory/MB}MB");
    }

    [Fact]
    public async Task MixedLoad_HTTPandSSEandSignalR_ShouldHandleConcurrently()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var stopwatch = Stopwatch.StartNew();
        
        // Capture initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var initialMemory = GC.GetTotalMemory(false);
        
        var httpClient = CreateProxyClient();
        httpClient.DefaultRequestHeaders.Add("HOST", "site1");
        
        // Act - Run all three types of operations concurrently
        var httpTask = Task.Run(async () =>
        {
            for (int i = 0; i < 100; i++)
            {
                var response = await httpClient.GetAsync("/api/test");
                response.EnsureSuccessStatusCode();
                await response.Content.ReadAsStringAsync();
            }
        });
        
        var sseTask = Task.Run(async () =>
        {
            var client = new HttpClient { BaseAddress = new Uri($"http://localhost:{ReverseProxyPort}") };
            client.DefaultRequestHeaders.Add("HOST", "site1");
            
            var response = await client.GetAsync("/sse", HttpCompletionOption.ResponseHeadersRead);
            var stream = await response.Content.ReadAsStreamAsync();
            var reader = new StreamReader(stream);
            
            int events = 0;
            while (events < 50)
            {
                var line = await reader.ReadLineAsync();
                if (line != null && line.StartsWith("data:"))
                {
                    events++;
                }
            }
            client.Dispose();
            return events;
        });
        
        var signalRTask = Task.Run(async () =>
        {
            var hubUrl = $"http://localhost:{ReverseProxyPort}/hubs/test";
            var connection = new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder()
                .WithUrl(hubUrl, options => { options.Headers.Add("Host", "site1"); })
                .Build();
            
            var messages = new List<string>();
            connection.On<string>("ReceiveMessage", msg => messages.Add(msg));
            
            await connection.StartAsync();
            
            for (int i = 0; i < 20; i++)
            {
                await connection.InvokeAsync("SendMessage", $"Message {i}");
                await Task.Delay(50);
            }
            
            await connection.StopAsync();
            await connection.DisposeAsync();
            return messages.Count;
        });
        
        await Task.WhenAll(httpTask, sseTask, signalRTask);
        stopwatch.Stop();
        
        var sseEvents = await sseTask;
        var signalRMessages = await signalRTask;
        
        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(false);
        
        // Assert - All operations should complete successfully
        Assert.True(sseEvents >= 50, $"Expected at least 50 SSE events, got {sseEvents}");
        Assert.True(signalRMessages >= 20, $"Expected at least 20 SignalR messages, got {signalRMessages}");
        Assert.True(stopwatch.Elapsed.TotalSeconds < 30, 
            $"Mixed load took too long: {stopwatch.Elapsed.TotalSeconds}s");
        
        var memoryGrowth = (finalMemory - initialMemory) / MB;
        Assert.True(memoryGrowth < 50, 
            $"Memory leak in mixed load detected: {memoryGrowth}MB growth. Initial: {initialMemory/MB}MB, Final: {finalMemory/MB}MB");
    }

    [Fact]
    public async Task MemoryMonitoring_UnderLoad_ShouldTrackUsage()
    {
        // Arrange
        await StartReverseProxyAsync();
        await StartBackendSiteAsync("site1", BackendSite1Port);
        await StartClientConnectionAsync("site1", BackendSite1Port);
        
        var client = CreateProxyClient();
        client.DefaultRequestHeaders.Add("HOST", "site1");
        
        var memorySnapshots = new List<(TimeSpan Elapsed, long MemoryMB)>();
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Monitor memory while under load
        var loadTask = Task.Run(async () =>
        {
            for (int i = 0; i < 500; i++)
            {
                var response = await client.GetAsync("/api/test");
                await response.Content.ReadAsStringAsync();
                
                if (i % 50 == 0)
                {
                    GC.Collect();
                    var memory = GC.GetTotalMemory(false);
                    memorySnapshots.Add((stopwatch.Elapsed, memory / MB));
                }
            }
        });
        
        await loadTask;
        stopwatch.Stop();
        
        // Assert - Memory should not grow excessively over time
        var firstSnapshot = memorySnapshots.First().MemoryMB;
        var lastSnapshot = memorySnapshots.Last().MemoryMB;
        var memoryGrowth = lastSnapshot - firstSnapshot;
        
        Assert.True(memorySnapshots.Count >= 5, $"Expected at least 5 memory snapshots, got {memorySnapshots.Count}");
        Assert.True(memoryGrowth < 30, 
            $"Excessive memory growth detected: {memoryGrowth}MB. First: {firstSnapshot}MB, Last: {lastSnapshot}MB");
        
        // Log memory progression for analysis
        foreach (var snapshot in memorySnapshots)
        {
            Console.WriteLine($"[{snapshot.Elapsed.TotalSeconds:F2}s] Memory: {snapshot.MemoryMB}MB");
        }
    }
}
