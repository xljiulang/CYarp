using System.Text.Json;

namespace CYarp.Tests.Simple;

/// <summary>
/// Simple tests that validate basic functionality without complex infrastructure
/// </summary>
public class SimpleConnectionTests
{
    [Fact]
    public void Basic_JsonSerialization_ShouldWork()
    {
        // Basic test to ensure test infrastructure can handle JSON serialization
        var testObject = new { Message = "Hello World", Timestamp = DateTime.UtcNow };
        var json = JsonSerializer.Serialize(testObject);
        
        Assert.NotNull(json);
        Assert.Contains("Hello World", json);
    }

    [Fact]
    public async Task Task_Delay_WithCancellation_ShouldWork()
    {
        // Test basic cancellation token functionality that the SSE implementation relies on
        using var cts = new CancellationTokenSource();
        
        var task = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(5000, cts.Token);
                return false; // Should not reach here
            }
            catch (OperationCanceledException)
            {
                return true; // Expected cancellation
            }
        });
        
        // Cancel after a short delay
        cts.CancelAfter(100);
        
        var result = await task;
        Assert.True(result); // Should have been cancelled
    }

    [Fact]
    public void CancellationToken_Basics_ShouldWork()
    {
        // Test basic cancellation token functionality
        using var cts = new CancellationTokenSource();
        
        Assert.False(cts.Token.IsCancellationRequested);
        
        cts.Cancel();
        
        Assert.True(cts.Token.IsCancellationRequested);
    }

    [Fact]
    public void HttpContext_RequestAborted_Concept_ShouldWork()
    {
        // Test the concept that HttpContext.RequestAborted should work with cancellation tokens
        using var cts = new CancellationTokenSource();
        
        // Simulate what HttpContext.RequestAborted does
        var requestAborted = cts.Token;
        
        Assert.False(requestAborted.IsCancellationRequested);
        
        // Simulate client disconnection
        cts.Cancel();
        
        Assert.True(requestAborted.IsCancellationRequested);
    }
}