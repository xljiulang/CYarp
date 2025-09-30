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
        await Task.Delay(1000);
        
        // Act
        var hubUrl = "http://site1.test.com/signalr";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => new HttpClientHandler();
            })
            .Build();
        
        try
        {
            await connection.StartAsync();
            
            // Assert
            Assert.Equal(HubConnectionState.Connected, connection.State);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SignalR_SendReceiveMessage_ShouldWork()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        var hubUrl = "http://site1.test.com/signalr";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();
        
        var receivedMessages = new List<(string connectionId, string message)>();
        
        connection.On<string, string>("ReceiveMessage", (connectionId, message) =>
        {
            receivedMessages.Add((connectionId, message));
        });
        
        try
        {
            // Act
            await connection.StartAsync();
            await connection.InvokeAsync("SendMessage", "Hello from test!");
            
            // Wait for message to be received
            await Task.Delay(1000);
            
            // Assert
            Assert.Single(receivedMessages);
            Assert.Equal("Hello from test!", receivedMessages[0].message);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SignalR_MultipleConnections_ShouldAllReceiveMessages()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        var hubUrl = "http://site1.test.com/signalr";
        var connections = new List<HubConnection>();
        var allReceivedMessages = new List<List<(string connectionId, string message)>>();
        
        try
        {
            // Create 3 connections
            for (int i = 0; i < 3; i++)
            {
                var connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .Build();
                
                var receivedMessages = new List<(string connectionId, string message)>();
                allReceivedMessages.Add(receivedMessages);
                
                connection.On<string, string>("ReceiveMessage", (connectionId, message) =>
                {
                    receivedMessages.Add((connectionId, message));
                });
                
                await connection.StartAsync();
                connections.Add(connection);
            }
            
            // Act - Send message from first connection
            await connections[0].InvokeAsync("SendMessage", "Broadcast message");
            
            // Wait for all connections to receive the message
            await Task.Delay(2000);
            
            // Assert
            foreach (var receivedMessages in allReceivedMessages)
            {
                Assert.Single(receivedMessages);
                Assert.Equal("Broadcast message", receivedMessages[0].message);
            }
        }
        finally
        {
            foreach (var connection in connections)
            {
                await connection.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task SignalR_Groups_ShouldWorkCorrectly()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        var hubUrl = "http://site1.test.com/signalr";
        var connection1 = new HubConnectionBuilder().WithUrl(hubUrl).Build();
        var connection2 = new HubConnectionBuilder().WithUrl(hubUrl).Build();
        var connection3 = new HubConnectionBuilder().WithUrl(hubUrl).Build();
        
        var group1Messages = new List<string>();
        var group2Messages = new List<string>();
        var noGroupMessages = new List<string>();
        
        connection1.On<string>("UserJoined", connectionId => group1Messages.Add($"Joined: {connectionId}"));
        connection2.On<string>("UserJoined", connectionId => group1Messages.Add($"Joined: {connectionId}"));
        connection3.On<string>("UserJoined", connectionId => noGroupMessages.Add($"Joined: {connectionId}"));
        
        try
        {
            // Act
            await Task.WhenAll(
                connection1.StartAsync(),
                connection2.StartAsync(),
                connection3.StartAsync()
            );
            
            // Join group
            await connection1.InvokeAsync("JoinGroup", "TestGroup");
            await connection2.InvokeAsync("JoinGroup", "TestGroup");
            // connection3 stays out of the group
            
            await Task.Delay(1000);
            
            // Assert
            Assert.True(group1Messages.Count >= 2, "Group members should receive join notifications");
            Assert.Empty(noGroupMessages); // Connection3 should not receive group messages
        }
        finally
        {
            await Task.WhenAll(
                connection1.DisposeAsync().AsTask(),
                connection2.DisposeAsync().AsTask(),
                connection3.DisposeAsync().AsTask()
            );
        }
    }

    [Fact]
    public async Task SignalR_WithSSE_ShouldNotInterfere()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        var hubUrl = "http://site1.test.com/signalr";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();
        
        var signalRMessages = new List<string>();
        connection.On<string, string>("ReceiveMessage", (connectionId, message) =>
        {
            signalRMessages.Add(message);
        });
        
        try
        {
            // Act - Start both SignalR and SSE
            await connection.StartAsync();
            
            var sseTask = Task.Run(async () =>
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
                catch (OperationCanceledException) { }
                return events.Count;
            });
            
            // Send SignalR message
            await connection.InvokeAsync("SendMessage", "SignalR message");
            
            await Task.Delay(1000);
            var sseEventCount = await sseTask;
            
            // Assert
            Assert.Single(signalRMessages);
            Assert.Equal("SignalR message", signalRMessages[0]);
            Assert.True(sseEventCount >= 1, "SSE should work alongside SignalR");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SignalR_MultipleClients_ShouldBeIndependent()
    {
        // Arrange
        var site1 = AddBackendServer("site1");
        var site2 = AddBackendServer("site2");
        await Task.Delay(2000);
        
        var site1Connection = new HubConnectionBuilder()
            .WithUrl("http://site1.test.com/signalr")
            .Build();
        
        var site2Connection = new HubConnectionBuilder()
            .WithUrl("http://site2.test.com/signalr")
            .Build();
        
        var site1Messages = new List<string>();
        var site2Messages = new List<string>();
        
        site1Connection.On<string, string>("ReceiveMessage", (connectionId, message) =>
        {
            site1Messages.Add(message);
        });
        
        site2Connection.On<string, string>("ReceiveMessage", (connectionId, message) =>
        {
            site2Messages.Add(message);
        });
        
        try
        {
            // Act
            await Task.WhenAll(
                site1Connection.StartAsync(),
                site2Connection.StartAsync()
            );
            
            await site1Connection.InvokeAsync("SendMessage", "Message from site1");
            await site2Connection.InvokeAsync("SendMessage", "Message from site2");
            
            await Task.Delay(1000);
            
            // Assert
            Assert.Single(site1Messages);
            Assert.Single(site2Messages);
            Assert.Equal("Message from site1", site1Messages[0]);
            Assert.Equal("Message from site2", site2Messages[0]);
        }
        finally
        {
            await Task.WhenAll(
                site1Connection.DisposeAsync().AsTask(),
                site2Connection.DisposeAsync().AsTask()
            );
        }
    }

    [Fact]
    public async Task SignalR_ConnectionAborted_ShouldHandleGracefully()
    {
        // Arrange
        var server = AddBackendServer("site1");
        await Task.Delay(1000);
        
        var hubUrl = "http://site1.test.com/signalr";
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();
        
        var connectionStateChanges = new List<HubConnectionState>();
        connection.Closed += exception =>
        {
            connectionStateChanges.Add(HubConnectionState.Disconnected);
            return Task.CompletedTask;
        };
        
        try
        {
            // Act
            await connection.StartAsync();
            Assert.Equal(HubConnectionState.Connected, connection.State);
            
            // Abruptly stop the connection
            await connection.StopAsync();
            
            // Assert
            Assert.Equal(HubConnectionState.Disconnected, connection.State);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}