using Microsoft.AspNetCore.SignalR.Client;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Client.Services;

public class ChatService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private string _currentUser = $"User_{Guid.NewGuid().ToString()[..4]}";

    public event Action<ChatMessage>? MessageReceived;
    public event Action<string>? UserConnected;
    public event Action<string>? UserDisconnected;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public async Task InitializeAsync(string hubUrl)
    {
        try
        {
            // Use absolute minimal configuration - no options at all
            // This avoids any HttpConnectionOptions configuration that might trigger X509 operations
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();

            // Setup message handlers
            _hubConnection.On<ChatMessage>("ReceiveMessage", (message) =>
            {
                MessageReceived?.Invoke(message);
            });

            _hubConnection.On<string>("UserConnected", (connectionId) =>
            {
                UserConnected?.Invoke(connectionId);
            });

            _hubConnection.On<string>("UserDisconnected", (connectionId) =>
            {
                UserDisconnected?.Invoke(connectionId);
            });

            // Start the connection
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize SignalR connection: {ex.Message}", ex);
        }
    }

    public async Task SendMessageAsync(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return;
        }

        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            Console.WriteLine("SignalR connection is not established. Message not sent.");
            return;
        }

        try
        {
            var chatMessage = new ChatMessage
            {
                User = _currentUser,
                Message = messageText,
                Timestamp = DateTime.UtcNow
            };

            await _hubConnection.SendAsync("SendMessage", chatMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send message: {ex.Message}");
        }
    }

    public string GetCurrentUser() => _currentUser;

    public void SetCurrentUser(string userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
        {
            _currentUser = userName;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.DisposeAsync();
            }
            finally
            {
                _hubConnection = null;
            }
        }
    }
}
