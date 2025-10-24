using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CYarp.Tests.Infrastructure;
using Xunit;

namespace CYarp.Tests.Load;

/// <summary>
/// Long-running memory leak detection tests based on UFX.Relay issue #22 pattern
/// These tests run for extended periods (10 minutes to 4 hours) to detect memory leaks
/// that only appear under sustained load
/// </summary>
[Collection("IntegrationTests")]
public class LongRunningMemoryTests
{
    private readonly CYarpTestBase _testBase;

    public LongRunningMemoryTests()
    {
        _testBase = new CYarpTestBase();
    }

    [Fact(Timeout = 660000)] // 11 minutes timeout
    public async Task SSE_LongRunning_10Minutes_MemoryLeak()
    {
        // Simulates: Long-running SSE stream (like stock tickers, chat, monitoring)
        // Detection: Linear memory growth over 10 minutes with 100 snapshots
        
        DiagDumps.WriteFullDump("SSE_10Min_start");
        
        var memorySnapshots = new List<long>();
        var startMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add(startMemory);
        
        Console.WriteLine($"[START] Initial memory: {startMemory / 1024 / 1024}MB");

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var client = HttpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(11);

        var eventCount = 0;
        var snapshotInterval = TimeSpan.FromSeconds(6); // 100 snapshots over 10 minutes
        var lastSnapshot = DateTime.UtcNow;

        try
        {
            var stream = await _testBase.GetSseStreamAsync(client, "site1");
            var reader = new StreamReader(stream);

            while (!cts.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line?.StartsWith("data:") == true)
                {
                    eventCount++;
                }

                // Take memory snapshot every 6 seconds
                if (DateTime.UtcNow - lastSnapshot > snapshotInterval)
                {
                    var currentMemory = GC.GetTotalMemory(false);
                    memorySnapshots.Add(currentMemory);
                    lastSnapshot = DateTime.UtcNow;
                    
                    if (memorySnapshots.Count % 10 == 0) // Log every minute
                    {
                        var growthMB = (currentMemory - startMemory) / 1024 / 1024;
                        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Memory: {currentMemory / 1024 / 1024}MB (+{growthMB}MB), Events: {eventCount}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected - test duration reached
        }

        var endMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add(endMemory);
        
        DiagDumps.WriteFullDump("SSE_10Min_end");
        
        var totalGrowthMB = (endMemory - startMemory) / 1024 / 1024;
        Console.WriteLine($"[END] Final memory: {endMemory / 1024 / 1024}MB, Growth: {totalGrowthMB}MB, Events: {eventCount}");

        // Analyze for linear growth (memory leak indicator)
        var avgGrowthPerSnapshot = memorySnapshots.Skip(10).Take(memorySnapshots.Count - 20)
            .Select((m, i) => i > 0 ? m - memorySnapshots[i + 9] : 0)
            .Where(g => g > 0)
            .Average();
        
        Console.WriteLine($"Average growth per snapshot: {avgGrowthPerSnapshot / 1024}KB");

        // Thresholds
        Assert.True(totalGrowthMB < 100, $"Memory grew by {totalGrowthMB}MB, expected < 100MB");
        Assert.True(avgGrowthPerSnapshot < 1024 * 1024, "Sustained linear growth detected - possible memory leak");
    }

    [Fact(Timeout = 3900000)] // 65 minutes timeout
    public async Task HTTP_LongRunning_ContinuousRequests_1Hour()
    {
        // Simulates: REST API with continuous requests (monitoring, polling, microservices)
        // Detection: Memory leak in request handling/response buffering
        
        DiagDumps.WriteFullDump("HTTP_1Hour_start");
        
        var memorySnapshots = new List<long>();
        var startMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add(startMemory);
        
        Console.WriteLine($"[START] Initial memory: {startMemory / 1024 / 1024}MB");

        var client = HttpClientFactory.CreateClient();
        var requestCount = 0;
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromHours(1);
        var snapshotInterval = TimeSpan.FromMinutes(2); // 30 snapshots
        var lastSnapshot = DateTime.UtcNow;
        var lastHourlyDump = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < duration)
        {
            var response = await _testBase.SendRequestAsync(client, "site1", "/api/weather");
            Assert.Equal(200, (int)response.StatusCode);
            requestCount++;

            // Hourly dumps for tests > 1 hour
            if (DateTime.UtcNow - lastHourlyDump > TimeSpan.FromHours(1))
            {
                var elapsed = DateTime.UtcNow - startTime;
                DiagDumps.WriteFullDump($"HTTP_1Hour_{elapsed.TotalHours:F1}h");
                lastHourlyDump = DateTime.UtcNow;
            }

            if (DateTime.UtcNow - lastSnapshot > snapshotInterval)
            {
                var currentMemory = GC.GetTotalMemory(false);
                memorySnapshots.Add(currentMemory);
                lastSnapshot = DateTime.UtcNow;
                
                var growthMB = (currentMemory - startMemory) / 1024 / 1024;
                var elapsed = DateTime.UtcNow - startTime;
                Console.WriteLine($"[{elapsed:mm\\:ss}] Memory: {currentMemory / 1024 / 1024}MB (+{growthMB}MB), Requests: {requestCount}");
            }

            await Task.Delay(1000); // ~3600 requests over 1 hour
        }

        var endMemory = GC.GetTotalMemory(true);
        var totalGrowthMB = (endMemory - startMemory) / 1024 / 1024;
        
        DiagDumps.WriteFullDump("HTTP_1Hour_end");
        
        Console.WriteLine($"[END] Final memory: {endMemory / 1024 / 1024}MB, Growth: {totalGrowthMB}MB, Requests: {requestCount}");

        Assert.True(totalGrowthMB < 200, $"Memory grew by {totalGrowthMB}MB, expected < 200MB");
        Assert.True(requestCount > 3500, $"Only {requestCount} requests completed");
    }

    [Fact(Timeout = 2100000)] // 35 minutes timeout
    public async Task SignalR_LongRunning_ConnectionChurn_30Minutes()
    {
        // Simulates: SignalR connection pool churn (clients connecting/disconnecting)
        // Detection: Connection handle leaks, socket leaks
        
        var memorySnapshots = new List<long>();
        var startMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add(startMemory);
        
        Console.WriteLine($"[START] Initial memory: {startMemory / 1024 / 1024}MB");

        var connectionCycles = 0;
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromMinutes(30);
        var snapshotInterval = TimeSpan.FromMinutes(1); // 30 snapshots
        var lastSnapshot = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < duration)
        {
            // Create connection
            var connection = await _testBase.CreateSignalRConnectionAsync("site1");
            await connection.InvokeAsync("SendMessage", "user", "test");
            
            // Dispose connection
            await connection.DisposeAsync();
            connectionCycles++;

            if (DateTime.UtcNow - lastSnapshot > snapshotInterval)
            {
                var currentMemory = GC.GetTotalMemory(false);
                memorySnapshots.Add(currentMemory);
                lastSnapshot = DateTime.UtcNow;
                
                var growthMB = (currentMemory - startMemory) / 1024 / 1024;
                var elapsed = DateTime.UtcNow - startTime;
                Console.WriteLine($"[{elapsed:mm\\:ss}] Memory: {currentMemory / 1024 / 1024}MB (+{growthMB}MB), Cycles: {connectionCycles}");
            }

            await Task.Delay(1000); // ~1800 cycles over 30 minutes
        }

        var endMemory = GC.GetTotalMemory(true);
        var totalGrowthMB = (endMemory - startMemory) / 1024 / 1024;
        Console.WriteLine($"[END] Final memory: {endMemory / 1024 / 1024}MB, Growth: {totalGrowthMB}MB, Cycles: {connectionCycles}");

        Assert.True(totalGrowthMB < 150, $"Memory grew by {totalGrowthMB}MB, expected < 150MB");
        Assert.True(connectionCycles > 1700, $"Only {connectionCycles} cycles completed");
    }

    [Fact(Timeout = 7500000)] // 125 minutes timeout
    public async Task Mixed_LongRunning_AllProtocols_2Hours()
    {
        // Simulates: Real production scenario with all protocols active
        // Detection: Protocol interaction memory leaks
        
        DiagDumps.WriteFullDump("Mixed_2Hours_start");
        
        var memorySnapshots = new List<long>();
        var startMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add(startMemory);
        
        Console.WriteLine($"[START] Initial memory: {startMemory / 1024 / 1024}MB");

        var client = HttpClientFactory.CreateClient();
        var stats = new { Http = 0, Sse = 0, SignalR = 0 };
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromHours(2);
        var lastHourlyDump = DateTime.UtcNow;

        using var cts = new CancellationTokenSource(duration);

        // Start concurrent tasks
        var tasks = new[]
        {
            // HTTP requests
            Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await _testBase.SendRequestAsync(client, "site1", "/api/weather");
                    Interlocked.Increment(ref stats.Http);
                    await Task.Delay(5000, cts.Token);
                }
            }),
            
            // SSE stream
            Task.Run(async () =>
            {
                var stream = await _testBase.GetSseStreamAsync(client, "site1");
                var reader = new StreamReader(stream);
                while (!cts.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line?.StartsWith("data:") == true)
                        Interlocked.Increment(ref stats.Sse);
                }
            }),
            
            // SignalR messages
            Task.Run(async () =>
            {
                var connection = await _testBase.CreateSignalRConnectionAsync("site1");
                while (!cts.Token.IsCancellationRequested)
                {
                    await connection.InvokeAsync("SendMessage", "user", "test");
                    Interlocked.Increment(ref stats.SignalR);
                    await Task.Delay(10000, cts.Token);
                }
            }),
            
            // Memory monitoring
            Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), cts.Token);
                    
                    // Hourly dumps for tests > 1 hour
                    if (DateTime.UtcNow - lastHourlyDump > TimeSpan.FromHours(1))
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        DiagDumps.WriteFullDump($"Mixed_2Hours_{elapsed.TotalHours:F1}h");
                        lastHourlyDump = DateTime.UtcNow;
                    }
                    
                    var currentMemory = GC.GetTotalMemory(false);
                    memorySnapshots.Add(currentMemory);
                    var growthMB = (currentMemory - startMemory) / 1024 / 1024;
                    var elapsed = DateTime.UtcNow - startTime;
                    Console.WriteLine($"[{elapsed:hh\\:mm}] Memory: {currentMemory / 1024 / 1024}MB (+{growthMB}MB), HTTP: {stats.Http}, SSE: {stats.Sse}, SignalR: {stats.SignalR}");
                }
            })
        };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        var endMemory = GC.GetTotalMemory(true);
        var totalGrowthMB = (endMemory - startMemory) / 1024 / 1024;
        
        DiagDumps.WriteFullDump("Mixed_2Hours_end");
        
        Console.WriteLine($"[END] Final memory: {endMemory / 1024 / 1024}MB, Growth: {totalGrowthMB}MB");
        Console.WriteLine($"Operations: HTTP: {stats.Http}, SSE: {stats.Sse}, SignalR: {stats.SignalR}");

        Assert.True(totalGrowthMB < 300, $"Memory grew by {totalGrowthMB}MB, expected < 300MB");
    }

    [Fact(Timeout = 2100000)] // 35 minutes timeout
    public async Task SSE_Multiple_LongRunning_Concurrent_30Minutes()
    {
        // Simulates: Multiple SSE streams (multiple browser tabs, multiple users)
        // Detection: Per-stream memory isolation issues
        
        var memorySnapshots = new List<long>();
        var startMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add(startMemory);
        
        Console.WriteLine($"[START] Initial memory: {startMemory / 1024 / 1024}MB");

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        var streamCount = 10;
        var eventCounts = new int[streamCount];

        var tasks = Enumerable.Range(0, streamCount).Select(async i =>
        {
            var client = HttpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(31);
            
            try
            {
                var stream = await _testBase.GetSseStreamAsync(client, "site1");
                var reader = new StreamReader(stream);

                while (!cts.Token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line?.StartsWith("data:") == true)
                    {
                        Interlocked.Increment(ref eventCounts[i]);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }).ToArray();

        // Memory monitoring
        var monitorTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(2), cts.Token);
                var currentMemory = GC.GetTotalMemory(false);
                memorySnapshots.Add(currentMemory);
                var growthMB = (currentMemory - startMemory) / 1024 / 1024;
                var totalEvents = eventCounts.Sum();
                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Memory: {currentMemory / 1024 / 1024}MB (+{growthMB}MB), Total events: {totalEvents}");
            }
        });

        try
        {
            await Task.WhenAll(tasks.Append(monitorTask));
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        var endMemory = GC.GetTotalMemory(true);
        var totalGrowthMB = (endMemory - startMemory) / 1024 / 1024;
        Console.WriteLine($"[END] Final memory: {endMemory / 1024 / 1024}MB, Growth: {totalGrowthMB}MB");

        Assert.True(totalGrowthMB < 200, $"Memory grew by {totalGrowthMB}MB, expected < 200MB");
    }

    [Fact(Timeout = 3900000)] // 65 minutes timeout
    public async Task HTTP_HighRate_Sustained_1Hour()
    {
        // Simulates: High-throughput API (10 req/s sustained)
        // Detection: Request queuing memory leaks
        
        var memorySnapshots = new List<long>();
        var startMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add(startMemory);
        
        Console.WriteLine($"[START] Initial memory: {startMemory / 1024 / 1024}MB");

        var client = HttpClientFactory.CreateClient();
        var requestCount = 0;
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromHours(1);

        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            while (DateTime.UtcNow - startTime < duration)
            {
                await _testBase.SendRequestAsync(client, "site1", "/api/weather");
                Interlocked.Increment(ref requestCount);
                await Task.Delay(1000);
            }
        }).ToList();

        // Memory monitoring
        tasks.Add(Task.Run(async () =>
        {
            while (DateTime.UtcNow - startTime < duration)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                var currentMemory = GC.GetTotalMemory(false);
                memorySnapshots.Add(currentMemory);
                var growthMB = (currentMemory - startMemory) / 1024 / 1024;
                Console.WriteLine($"[{DateTime.UtcNow - startTime:mm\\:ss}] Memory: {currentMemory / 1024 / 1024}MB (+{growthMB}MB), Requests: {requestCount}");
            }
        }));

        await Task.WhenAll(tasks);
        
        DiagDumps.WriteFullDump("MemoryPressure_4Hours_end");

        var endMemory = GC.GetTotalMemory(true);
        var totalGrowthMB = (endMemory - startMemory) / 1024 / 1024;
        Console.WriteLine($"[END] Final memory: {endMemory / 1024 / 1024}MB, Growth: {totalGrowthMB}MB, Requests: {requestCount}");

        Assert.True(totalGrowthMB < 250, $"Memory grew by {totalGrowthMB}MB, expected < 250MB");
        Assert.True(requestCount > 35000, $"Only {requestCount} requests completed");
    }

    [Fact(Timeout = 3900000)] // 65 minutes timeout
    public async Task SignalR_Messages_Continuous_1Hour()
    {
        // Simulates: Chat application with continuous messages
        // Detection: Message buffer leaks
        
        var memorySnapshots = new List<long>();
        var startMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add(startMemory);
        
        Console.WriteLine($"[START] Initial memory: {startMemory / 1024 / 1024}MB");

        var connection = await _testBase.CreateSignalRConnectionAsync("site1");
        var messageCount = 0;
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromHours(1);

        while (DateTime.UtcNow - startTime < duration)
        {
            await connection.InvokeAsync("SendMessage", "user", $"Message {messageCount}");
            messageCount++;

            if (messageCount % 300 == 0) // Every 5 minutes
            {
                var currentMemory = GC.GetTotalMemory(false);
                memorySnapshots.Add(currentMemory);
                var growthMB = (currentMemory - startMemory) / 1024 / 1024;
                Console.WriteLine($"[{DateTime.UtcNow - startTime:mm\\:ss}] Memory: {currentMemory / 1024 / 1024}MB (+{growthMB}MB), Messages: {messageCount}");
            }

            await Task.Delay(1000);
        }

        var endMemory = GC.GetTotalMemory(true);
        var totalGrowthMB = (endMemory - startMemory) / 1024 / 1024;
        Console.WriteLine($"[END] Final memory: {endMemory / 1024 / 1024}MB, Growth: {totalGrowthMB}MB, Messages: {messageCount}");

        Assert.True(totalGrowthMB < 150, $"Memory grew by {totalGrowthMB}MB, expected < 150MB");
    }

    [Fact(Timeout = 7500000)] // 125 minutes timeout
    public async Task Tunnel_LongRunning_CreateDestroy_2Hours()
    {
        // Simulates: Frequent tunnel creation/destruction (connection pooling stress)
        // Detection: Tunnel handle leaks, stream leaks
        
        var memorySnapshots = new List<long>();
        var startMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add(startMemory);
        
        Console.WriteLine($"[START] Initial memory: {startMemory / 1024 / 1024}MB");

        var tunnelCycles = 0;
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromHours(2);

        while (DateTime.UtcNow - startTime < duration)
        {
            // Create client, make request, dispose
            using (var client = HttpClientFactory.CreateClient())
            {
                await _testBase.SendRequestAsync(client, "site1", "/api/weather");
            }
            
            tunnelCycles++;

            if (tunnelCycles % 360 == 0) // Every 10 minutes
            {
                var currentMemory = GC.GetTotalMemory(false);
                memorySnapshots.Add(currentMemory);
                var growthMB = (currentMemory - startMemory) / 1024 / 1024;
                var elapsed = DateTime.UtcNow - startTime;
                Console.WriteLine($"[{elapsed:hh\\:mm}] Memory: {currentMemory / 1024 / 1024}MB (+{growthMB}MB), Tunnels: {tunnelCycles}");
            }

            await Task.Delay(1000);
        }

        var endMemory = GC.GetTotalMemory(true);
        var totalGrowthMB = (endMemory - startMemory) / 1024 / 1024;
        Console.WriteLine($"[END] Final memory: {endMemory / 1024 / 1024}MB, Growth: {totalGrowthMB}MB, Tunnels: {tunnelCycles}");

        Assert.True(totalGrowthMB < 200, $"Memory grew by {totalGrowthMB}MB, expected < 200MB");
    }

    [Fact(Timeout = 3900000)] // 65 minutes timeout
    public async Task Client_Reconnect_Pattern_1Hour()
    {
        // Simulates: Network instability with frequent reconnections
        // Detection: Connection state leaks
        
        var memorySnapshots = new List<long>();
        var startMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add(startMemory);
        
        Console.WriteLine($"[START] Initial memory: {startMemory / 1024 / 1024}MB");

        var reconnectCycles = 0;
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromHours(1);

        while (DateTime.UtcNow - startTime < duration)
        {
            // Simulate reconnection
            await StopBackendServersAsync();
            await Task.Delay(2000);
            await StartBackendServersAsync();
            await Task.Delay(2000);

            // Verify connection works
            using var client = HttpClientFactory.CreateClient();
            var response = await _testBase.SendRequestAsync(client, "site1", "/api/weather");
            Assert.Equal(200, (int)response.StatusCode);
            
            reconnectCycles++;

            if (reconnectCycles % 10 == 0)
            {
                var currentMemory = GC.GetTotalMemory(false);
                memorySnapshots.Add(currentMemory);
                var growthMB = (currentMemory - startMemory) / 1024 / 1024;
                Console.WriteLine($"[{DateTime.UtcNow - startTime:mm\\:ss}] Memory: {currentMemory / 1024 / 1024}MB (+{growthMB}MB), Reconnects: {reconnectCycles}");
            }

            await Task.Delay(90000); // ~360 reconnects over 1 hour
        }

        var endMemory = GC.GetTotalMemory(true);
        var totalGrowthMB = (endMemory - startMemory) / 1024 / 1024;
        Console.WriteLine($"[END] Final memory: {endMemory / 1024 / 1024}MB, Growth: {totalGrowthMB}MB, Reconnects: {reconnectCycles}");

        Assert.True(totalGrowthMB < 150, $"Memory grew by {totalGrowthMB}MB, expected < 150MB");
    }

    [Fact(Timeout = 15000000)] // 250 minutes timeout (4+ hours)
    public async Task MemoryPressure_UnderLoad_Continuous_4Hours()
    {
        // Ultimate soak test - validates system can run 24/7 without memory issues
        // Simulates: Production load for extended period
        // Detection: Any memory leak pattern will show up over 4 hours
        
        DiagDumps.WriteFullDump("MemoryPressure_4Hours_start");
        
        var memorySnapshots = new List<(DateTime Time, long Memory, int Gen0, int Gen1, int Gen2)>();
        var startMemory = GC.GetTotalMemory(true);
        memorySnapshots.Add((DateTime.UtcNow, startMemory, GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)));
        
        Console.WriteLine($"[START] Initial memory: {startMemory / 1024 / 1024}MB");
        Console.WriteLine("This is a 4-hour soak test. Monitor memory externally with:");
        Console.WriteLine("  Windows: perfmon, dotMemory, PerfView");
        Console.WriteLine("  Linux: top, htop, dotnet-dump");

        var stats = new { Http = 0, Sse = 0, SignalR = 0, Errors = 0 };
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromHours(4);
        var lastHourlyDump = DateTime.UtcNow;

        using var cts = new CancellationTokenSource(duration);

        var tasks = new[]
        {
            // HTTP load
            Task.Run(async () =>
            {
                var client = HttpClientFactory.CreateClient();
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await _testBase.SendRequestAsync(client, "site1", "/api/weather");
                        Interlocked.Increment(ref stats.Http);
                        await Task.Delay(5000, cts.Token);
                    }
                    catch { Interlocked.Increment(ref stats.Errors); }
                }
            }),
            
            // SSE stream
            Task.Run(async () =>
            {
                var client = HttpClientFactory.CreateClient();
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var stream = await _testBase.GetSseStreamAsync(client, "site1");
                        var reader = new StreamReader(stream);
                        for (int i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
                        {
                            var line = await reader.ReadLineAsync();
                            if (line?.StartsWith("data:") == true)
                                Interlocked.Increment(ref stats.Sse);
                        }
                        await stream.DisposeAsync();
                    }
                    catch { Interlocked.Increment(ref stats.Errors); }
                    await Task.Delay(60000, cts.Token); // Reconnect every minute
                }
            }),
            
            // SignalR
            Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var connection = await _testBase.CreateSignalRConnectionAsync("site1");
                        for (int i = 0; i < 100 && !cts.Token.IsCancellationRequested; i++)
                        {
                            await connection.InvokeAsync("SendMessage", "user", "test");
                            Interlocked.Increment(ref stats.SignalR);
                            await Task.Delay(10000, cts.Token);
                        }
                        await connection.DisposeAsync();
                    }
                    catch { Interlocked.Increment(ref stats.Errors); }
                }
            }),
            
            // Memory monitoring (every minute)
            Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cts.Token);
                    
                    var currentMemory = GC.GetTotalMemory(false);
                    var gen0 = GC.CollectionCount(0);
                    var gen1 = GC.CollectionCount(1);
                    var gen2 = GC.CollectionCount(2);
                    
                    memorySnapshots.Add((DateTime.UtcNow, currentMemory, gen0, gen1, gen2));
                    
                    var growthMB = (currentMemory - startMemory) / 1024 / 1024;
                    var elapsed = DateTime.UtcNow - startTime;
                    
                    Console.WriteLine($"[{elapsed:hh\\:mm\\:ss}] Memory: {currentMemory / 1024 / 1024}MB (+{growthMB}MB), " +
                                    $"GC(0/1/2): {gen0}/{gen1}/{gen2}, " +
                                    $"HTTP: {stats.Http}, SSE: {stats.Sse}, SignalR: {stats.SignalR}, Errors: {stats.Errors}");
                    
                    // Dump memory every hour
                    if (DateTime.UtcNow - lastHourlyDump > TimeSpan.FromHours(1))
                    {
                        DiagDumps.WriteFullDump($"MemoryPressure_4Hours_hour{(int)elapsed.TotalHours}");
                        Console.WriteLine($"[{elapsed:hh\\:mm\\:ss}] Memory dump captured at hour {(int)elapsed.TotalHours}");
                        lastHourlyDump = DateTime.UtcNow;
                    }
                    
                    // Alert if memory growth is excessive
                    if (growthMB > 400)
                    {
                        Console.WriteLine($"WARNING: Memory growth exceeds 400MB! Possible memory leak detected.");
                    }
                }
            })
        };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        var endMemory = GC.GetTotalMemory(true);
        var totalGrowthMB = (endMemory - startMemory) / 1024 / 1024;
        
        Console.WriteLine($"\n[END] Final memory: {endMemory / 1024 / 1024}MB, Growth: {totalGrowthMB}MB");
        Console.WriteLine($"Operations: HTTP: {stats.Http}, SSE: {stats.Sse}, SignalR: {stats.SignalR}, Errors: {stats.Errors}");
        Console.WriteLine($"GC Collections: Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}");

        // Analyze memory progression
        Console.WriteLine("\nMemory Progression:");
        foreach (var snapshot in memorySnapshots.TakeLast(10))
        {
            var elapsed = snapshot.Time - startTime;
            Console.WriteLine($"  [{elapsed:hh\\:mm}] {snapshot.Memory / 1024 / 1024}MB (GC: {snapshot.Gen0}/{snapshot.Gen1}/{snapshot.Gen2})");
        }

        // Assertions
        Assert.True(totalGrowthMB < 500, $"Memory grew by {totalGrowthMB}MB over 4 hours, expected < 500MB");
        Assert.True(stats.Errors < 50, $"{stats.Errors} errors occurred during test");
        
        // Check for linear growth pattern (indicates leak)
        var recentGrowth = memorySnapshots.TakeLast(30).Select(s => s.Memory).ToList();
        var avgRecentMemory = recentGrowth.Average();
        var isStable = recentGrowth.All(m => Math.Abs(m - avgRecentMemory) < avgRecentMemory * 0.2);
        Assert.True(isStable || totalGrowthMB < 300, "Memory not stabilizing - possible leak pattern");

        Console.WriteLine($"\n✅ Test passed! System stable for 4 hours with < {totalGrowthMB}MB growth");
    }
}
