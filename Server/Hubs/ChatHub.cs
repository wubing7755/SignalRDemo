using Microsoft.AspNetCore.SignalR;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Hubs;

public class ChatHub : Hub
{
    public async Task SendMessage(ChatMessage chatMessage)
    {
        await Clients.All.SendAsync("ReceiveMessage", chatMessage);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}