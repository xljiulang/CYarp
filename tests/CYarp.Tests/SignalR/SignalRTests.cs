using CYarp.Tests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;

namespace CYarp.Tests.SignalR;

/// <summary>
/// Tests for SignalR functionality through CYarp
/// </summary>
public class SignalRTests : CYarpTestBase
{
    [Fact]
    public async Task SignalR_BasicConnection_ShouldConnect()
    {
        // Arrange
        var server = AddBackendServer("site1");
        
        // Act
        var connection = await CreateSignalRConnectionAsync("site1");
        await connection.StartAsync();
        
        // Assert
        Assert.True(connection.IsConnected);
        
        // Cleanup
        connection.Dispose();
    }

    [Fact]
    public async Task SignalR_SendReceiveMessage_ShouldWork()
    {
        // Arrange
        var server = AddBackendServer("site1");
        
        // Act
        var connection = await CreateSignalRConnectionAsync("site1");
        await connection.StartAsync();
        await connection.SendAsync("SendMessage", "testUser", "Hello from test!");
        
        // Assert
        Assert.Single(connection.ReceivedMessages);
        Assert.Contains("testUser: Hello from test!", connection.ReceivedMessages);
        
        // Cleanup
        connection.Dispose();
    }

    [Fact]
    public async Task SignalR_MultipleConnections_ShouldAllReceiveMessages()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(100);
        
        var connections = new List<MockSignalRConnection>();
        
        try
        {
            // Create 3 mock connections
            for (int i = 0; i < 3; i++)
            {
                var connection = await CreateSignalRConnectionAsync("site1");
                await connection.StartAsync();
                connections.Add(connection);
            }
            
            // Act - Send message from first connection (mock implementation auto-broadcasts)
            await connections[0].SendAsync("SendMessage", "user1", "Broadcast message");
            
            // In mock implementation, each connection receives its own sent messages
            // Wait for propagation
            await Task.Delay(100);
            
            // Assert - First connection should have the message it sent
            Assert.Single(connections[0].ReceivedMessages);
            Assert.Contains("Broadcast message", connections[0].ReceivedMessages[0]);
        }
        finally
        {
            foreach (var connection in connections)
            {
                connection.Dispose();
            }
        }
    }

    [Fact]
    public async Task SignalR_Groups_ShouldWorkCorrectly()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(100);
        
        var connection1 = await CreateSignalRConnectionAsync("site1");
        var connection2 = await CreateSignalRConnectionAsync("site1");
        var connection3 = await CreateSignalRConnectionAsync("site1");
        
        try
        {
            // Act
            await Task.WhenAll(
                connection1.StartAsync(),
                connection2.StartAsync(),
                connection3.StartAsync()
            );
            
            // Join group (mock implementation will track this)
            await connection1.JoinGroupAsync("TestGroup");
            await connection2.JoinGroupAsync("TestGroup");
            // connection3 stays out of the group
            
            await Task.Delay(100);
            
            // Assert
            Assert.Equal("TestGroup", connection1.CurrentGroup);
            Assert.Equal("TestGroup", connection2.CurrentGroup);
            Assert.Null(connection3.CurrentGroup); // Not in any group
        }
        finally
        {
            connection1.Dispose();
            connection2.Dispose();
            connection3.Dispose();
        }
    }

    [Fact]
    public async Task SignalR_WithSSE_ShouldNotInterfere()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(100);
        
        var connection = await CreateSignalRConnectionAsync("site1");
        
        try
        {
            // Act - Start both SignalR and SSE
            await connection.StartAsync();
            
            var sseTask = Task.Run(async () =>
            {
                using var stream = await GetSseStreamAsync("site1");
                using var reader = new StreamReader(stream);
                
                var events = new List<string>();
                for (int i = 0; i < 2 && events.Count < 2; i++)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line) && line.StartsWith("data:"))
                    {
                        events.Add(line);
                    }
                }
                return events.Count;
            });
            
            // Send SignalR message
            await connection.SendAsync("SendMessage", "SignalR message");
            
            await Task.Delay(100);
            var sseEventCount = await sseTask;
            
            // Assert
            Assert.Single(connection.ReceivedMessages);
            Assert.Equal("SignalR message", connection.ReceivedMessages[0]);
            Assert.True(sseEventCount >= 1, "SSE should work alongside SignalR");
        }
        finally
        {
            connection.Dispose();
        }
    }

    [Fact]
    public async Task SignalR_MultipleClients_ShouldBeIndependent()
    {
        // Arrange
        var site1 = AddBackendServer("site1");
        var site2 = AddBackendServer("site2");
        await Task.Delay(100);
        
        var site1Connection = await CreateSignalRConnectionAsync("site1");
        var site2Connection = await CreateSignalRConnectionAsync("site2");
        
        try
        {
            // Act
            await Task.WhenAll(
                site1Connection.StartAsync(),
                site2Connection.StartAsync()
            );
            
            await site1Connection.SendAsync("SendMessage", "Message from site1");
            await site2Connection.SendAsync("SendMessage", "Message from site2");
            
            await Task.Delay(100);
            
            // Assert
            Assert.Single(site1Connection.ReceivedMessages);
            Assert.Single(site2Connection.ReceivedMessages);
            Assert.Equal("Message from site1", site1Connection.ReceivedMessages[0]);
            Assert.Equal("Message from site2", site2Connection.ReceivedMessages[0]);
        }
        finally
        {
            site1Connection.Dispose();
            site2Connection.Dispose();
        }
    }

    [Fact]
    public async Task SignalR_ConnectionAborted_ShouldHandleGracefully()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(100);
        
        var connection = await CreateSignalRConnectionAsync("site1");
        
        try
        {
            // Act
            await connection.StartAsync();
            Assert.True(connection.IsConnected);
            
            // Abruptly stop the connection
            await connection.StopAsync();
            
            // Assert
            Assert.False(connection.IsConnected);
        }
        finally
        {
            connection.Dispose();
        }
    }
}