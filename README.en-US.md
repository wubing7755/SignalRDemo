# SignalR Real-Time Chat Room (Blazor WebAssembly)

![Blazor WASM](https://img.shields.io/badge/Blazor-Web-assembly-blueviolet)
![.NET](https://img.shields.io/badge/.NET-6.0%2B-blue)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![EN](https://img.shields.io/badge/Language-English-blue)](README.en-US.md)
[![CN](https://img.shields.io/badge/语言-中文-red)](README.md)

A real-time multiplayer chat room example project based on **Blazor WebAssembly** and **ASP.NET Core SignalR**, using **DDD (Domain-Driven Design)** architecture with complete CQRS implementation.

---

## ✨ Project Features

- 💬 Real-time message sending and receiving
- 👥 Multi-user online chat
- 🟢 Online user status display
- ⏱️ Message timestamps
- 🧑 User authentication system (registration/login)
- 🔐 Private room password protection
- 🔄 SignalR real-time bidirectional communication
- 🔌 Automatic reconnection mechanism
- 📡 Real-time connection status indicator
- 🏥 Health check endpoint

---

## 🧱 Tech Stack

| Module | Technology |
|--------|------------|
| Frontend | Blazor WebAssembly (.NET 6.0.36) |
| Backend | ASP.NET Core (.NET 6.0) |
| Real-time Communication | SignalR |
| Architecture | DDD + CQRS (MediatR) |
| Authentication | JWT |
| Shared Models | .NET 6.0 Class Library |

---

## 📂 Project Structure

```
SignalRDemo/
├── Client/                           # Blazor WebAssembly Client
│   ├── Pages/
│   │   ├── ChatRoom.razor          # Chat room main page
│   │   └── Index.razor             # Home page
│   ├── Services/
│   │   ├── ChatService.cs          # SignalR connection service
│   │   ├── AuthService.cs          # Authentication service
│   │   └── RoomService.cs         # Room service
│   ├── Components/                  # Blazor components
│   ├── Shared/
│   └── wwwroot/
│
├── Server/                           # ASP.NET Core Server
│   ├── Hubs/
│   │   └── ChatHub.cs              # SignalR Hub
│   ├── Controllers/
│   │   ├── AuthController.cs       # Authentication API
│   │   └── StatsController.cs      # Stats API
│   ├── Services/
│   │   └── SignalRHealthCheck.cs  # Health check
│   └── Program.cs
│
├── Shared/                          # Shared Class Library
│   └── Models/
│       ├── ChatMessage.cs           # Chat message model
│       ├── ChatRoom.cs             # Chat room model
│       ├── User.cs                 # User model
│       ├── Requests.cs             # Request DTOs
│       ├── Responses.cs            # Response DTOs
│       └── MessageType.cs          # Message type enum
│
├── SignalRDemo.Application/        # Application Layer (CQRS)
│   ├── Commands/                    # Commands
│   │   ├── Messages/
│   │   ├── Rooms/
│   │   └── Users/
│   ├── Handlers/                    # Command handlers
│   ├── DTOs/                       # Data transfer objects
│   └── Results/                    # Result wrapper
│
├── SignalRDemo.Domain/             # Domain Layer
│   ├── Aggregates/                  # Aggregate roots
│   │   ├── ChatRoom.cs
│   │   └── User.cs
│   ├── Entities/                    # Entities
│   │   └── ChatMessage.cs
│   ├── ValueObjects/                # Value objects
│   │   ├── EntityId.cs
│   │   ├── RoomName.cs
│   │   ├── UserName.cs
│   │   └── ...
│   ├── Events/                     # Domain events
│   ├── Exceptions/                 # Domain exceptions
│   └── Repositories/               # Repository interfaces
│
└── SignalRDemo.Infrastructure/    # Infrastructure Layer
    ├── Services/                   # Service implementations
    │   ├── ChatRepository.cs
    │   ├── RoomService.cs
    │   ├── UserService.cs
    │   └── UserConnectionManager.cs
    └── Repositories/                # Repository implementations
        ├── InMemoryMessageRepository.cs
        ├── InMemoryRoomRepository.cs
        └── InMemoryUserRepository.cs
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
dotnet run --project src/Server/SignalRDemo.Server.csproj
```

4. Browser access

- [https://localhost:7002](https://localhost:7002)
- [http://localhost:5293](http://localhost:5293)

---

## 🏗️ Architecture

This project uses **DDD (Domain-Driven Design)** architecture combined with **CQRS** pattern.

### Domain Layer

Contains core business logic:
- **Aggregate Roots**: ChatRoom, User
- **Entities**: ChatMessage
- **Value Objects**: EntityId, RoomName, UserName, etc.
- **Repository Interfaces**: Define data access contracts

### Application Layer

Uses MediatR for CQRS:
- **Commands**: SendMessageCommand, CreateRoomCommand, JoinRoomCommand, LoginCommand, etc.
- **Handlers**: Process commands and return results

### Infrastructure Layer

Implements interfaces defined in Domain layer:
- **Services**: UserService, RoomService, ChatRepository, UserConnectionManager
- **Repositories**: InMemoryUserRepository, InMemoryRoomRepository, InMemoryMessageRepository

### Server Layer

- **SignalR Hub**: Handles real-time communication
- **Controllers**: Provides REST API (authentication, stats)
- **Health Check**: Monitors service status

---

## 📖 Use Cases

- 🎓 Learning DDD architecture design
- ⚡ SignalR real-time communication practice
- 💬 Instant chat/notification system prototype
- 🤝 CQRS pattern practice

---

## 📄 License

This project is open source under the **MIT License**, feel free to use and modify.

---

## 🙌 Contributions

Issues and Pull Requests are welcome to improve this example project together.

---

## 📞 Contact

- GitHub: [wubing7755/SignalRDemo](https://github.com/wubing7755/SignalRDemo)
