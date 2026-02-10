# SignalR Real-Time Chat Room (Blazor WebAssembly)

![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly-blueviolet)
![.NET](https://img.shields.io/badge/.NET-6.0%2B-blue)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![EN](https://img.shields.io/badge/Language-English-blue)](README.en-US.md)
[![CN](https://img.shields.io/badge/语言-中文-red)](README.md)

A real-time multiplayer chat room example project based on **Blazor WebAssembly** and **ASP.NET Core SignalR**, using the standard **Blazor WASM hosting model** with Client, Server, and Shared projects to demonstrate a complete real-time bidirectional communication implementation.

---

## ✨ Project Features

- 💬 Real-time message sending and receiving
- 👥 Multi-user online chat
- 🟢 Online user status display
- ⏱️ Message timestamps
- 🧑 Simple user identification
- 🔄 SignalR real-time bidirectional communication
- 🔌 Automatic reconnection mechanism
- 📡 Real-time connection status indicator

---

## 🧱 Tech Stack

| Module | Technology |
|--------|------------|
| Frontend | Blazor WebAssembly (.NET 6.0.36) |
| Backend | ASP.NET Core (.NET 6.0) |
| Real-time Communication | SignalR |
| Shared Models | .NET 6.0 Class Library |

---

## 📂 Project Structure

```
SignalRDemo/
├── Client/                     # Blazor WebAssembly Client
│   ├── Pages/
│   │   ├── ChatRoom.razor      # Chat room main page
│   │   └── Index.razor         # Home page
│   ├── Services/
│   │   └── ChatService.cs      # SignalR connection and communication service
│   ├── Shared/
│   │   ├── MainLayout.razor
│   │   ├── NavMenu.razor
│   │   └── SurveyPrompt.razor
│   ├── wwwroot/
│   ├── App.razor
│   ├── _Imports.razor
│   ├── Program.cs              # Client entry point
│   └── SignalRDemo.Client.csproj
│
├── Server/                     # ASP.NET Core Server
│   ├── Hubs/
│   │   └── ChatHub.cs          # SignalR Hub
│   ├── Pages/
│   │   ├── Error.cshtml        # Error page
│   │   └── Error.cshtml.cs
│   ├── Properties/
│   │   └── launchSettings.json # Launch configuration
│   ├── Program.cs              # Server entry point
│   ├── appsettings.json
│   └── SignalRDemo.Server.csproj
│
└── Shared/                     # Shared Class Library
    ├── Models/
    │   ├── ChatMessage.cs      # Chat message model
    │   └── UserConnection.cs   # User connection model
    └── SignalRDemo.Shared.csproj
```

---

## 🚀 Quick Start

### Environment Requirements

- .NET 6.0 SDK or higher
- Visual Studio 2022 / VS Code (optional)

### Running Steps

1. Clone the repository

```bash
git clone https://github.com/wubing7755/SignalRDemo.git
cd SignalRDemo
```

2. Restore dependencies

```bash
dotnet restore
```

3. Start the server

```bash
dotnet run --project Server/SignalRDemo.Server.csproj
```

4. Browser access

- [https://localhost:7002](https://localhost:7002)
- [http://localhost:5293](http://localhost:5293)

---

## 🛠️ Implementation Steps

The project is implemented step by step, suitable for learning the complete integration process of SignalR with Blazor WASM.

### 1️⃣ Project Initialization

- Verify Blazor WebAssembly hosting model
- Confirm Client / Server / Shared project structure
- Ensure project builds and runs successfully

### 2️⃣ Add SignalR Packages

**Server**
- `Microsoft.AspNetCore.SignalR` (v1.1.0)
- `Microsoft.AspNetCore.Components.WebAssembly.Server` (v6.0.36)

**Client**
- `Microsoft.AspNetCore.SignalR.Client` (v6.0.36)
- `Microsoft.AspNetCore.Components.WebAssembly` (v6.0.36)

### 3️⃣ Define Shared Models

| Model | Description |
|-------|-------------|
| `ChatMessage` | Chat message containing user, message content, timestamp |
| `UserConnection` | User connection info containing user ID, username, connection time |

### 4️⃣ Implement SignalR Hub

**ChatHub.cs** core functionality:

```csharp
public class ChatHub : Hub
{
    // Message broadcast
    public async Task SendMessage(ChatMessage chatMessage)
    {
        await Clients.All.SendAsync("ReceiveMessage", chatMessage);
    }

    // User connection notification
    public override async Task OnConnectedAsync()
    {
        await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    // User disconnection notification
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
```

### 5️⃣ Server Configuration

**Program.cs** key configuration:

- Register SignalR service: `services.AddSignalR()`
- Map Hub route: `app.MapHub<ChatHub>("/chathub")`
- Configure CORS to support WASM client access
- Enable Blazor file serving: `app.UseBlazorFrameworkFiles()`

### 6️⃣ Client SignalR Connection

**ChatService.cs** core functionality:

- Create `HubConnection` instance
- Configure Hub URL connection
- Register message handlers (ReceiveMessage, UserConnected, UserDisconnected)
- Implement automatic reconnection mechanism
- Provide `SendMessageAsync` for sending messages

### 7️⃣ Chat Room UI

- Chat main interface layout
- Message list display (with timestamp formatting)
- Input box and send button
- Online user list (based on ConnectionId)
- Connection status indicator

### 8️⃣ Message Sending/Receiving Mechanism

```
Client sends → Hub.SendMessage → Server broadcast → All clients receive
```

### 9️⃣ User Status Management

- User identification: Auto-generated `User_XXXX` format username
- Custom username setting supported
- Real-time online/offline status display
- Connection status indicator (Connected/Disconnected/Connecting)

### 🔟 Optimization & Testing

- Message timestamp formatting (UTC conversion)
- Exception handling and error messages
- Automatic reconnection strategy
- UI and interaction experience optimization

---

## 📖 Use Cases

- 🎓 Learning SignalR real-time communication
- ⚡ Blazor WebAssembly practical example
- 💬 Instant chat/notification system prototype
- 🤝 Real-time collaboration application foundation

---

## 📄 License

This project is open source under the **MIT License**, feel free to use and modify.

---

## 🙌 Contributions

Issues and Pull Requests are welcome to improve this example project together.

---

## 📞 Contact

- GitHub: [wubing7755/SignalRDemo](https://github.com/wubing7755/SignalRDemo)
