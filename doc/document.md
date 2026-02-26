# SignalRDemo 项目学习指南

> 本指南帮助开发者循序渐进地学习这个采用DDD架构的SignalR实时聊天室项目

---

## 📚 学习前置知识

在开始学习之前，建议您具备以下基础知识：

| 知识领域 | 推荐程度 | 说明 |
|----------|----------|------|
| C# 基础 | 必需 | 熟悉面向对象编程 |
| ASP.NET Core | 推荐 | 了解Web API开发 |
| Blazor | 推荐 | 了解Razor组件开发 |
| SignalR | 必需 | 了解实时通信基础 |
| DDD/CQRS | 加分 | 了解领域驱动设计模式 |

---

## 🗺️ 学习路线图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            项目学习路线                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  阶段一：基础篇                                                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │  1. 项目结构 │→ │ 2. SignalR  │→ │ 3. Blazor   │→ │ 4. 共享模型  │         │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘         │
│                                                                             │
│  阶段二：客户端篇                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │  
│  │ 5. ChatHub  │→ │ 6. 客户端    │→ │ 7. 认证系统  │→ │ 8. 房间管理  │         │
│  │   服务端     │  │  连接服务    │  │             │  │             │         │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘         │
│                                                                             │
│  阶段三：架构篇                                                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ 9. Domain   │→ │10. Application│→│11. 基础     │→│12. MediatR  │         │
│  │   领域层     │  │   应用层     │  │   设施层    │  │   集成      │          │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📖 详细学习内容

### 阶段一：基础篇

#### 1. 理解项目结构

首先阅读项目根目录的 `README.md`，了解：
- 项目特性
- 快速开始指南

然后
- 技术栈浏览 `src/` 目录结构：

```
src/
├── Client/                      # Blazor WebAssembly 前端
├── Server/                      # ASP.NET Core 后端
├── Shared/                      # 共享模型
├── SignalRDemo.Application/    # 应用层 (CQRS)
├── SignalRDemo.Domain/        # 领域层 (DDD)
└── SignalRDemo.Infrastructure/ # 基础设施层
```

**学习要点**：每个项目的职责是什么？它们之间如何引用？

---

#### 2. SignalR 实时通信

阅读 `doc/SignalR_Tutorial.md`，重点理解：

- **Hub概念**：SignalR的核心组件，作为通信中心
- **双向通信**：客户端↔服务端的实时消息推送
- **连接管理**：连接、断开、重连机制
- **分组广播**：向特定用户组发送消息

**实践建议**：运行项目，打开两个浏览器窗口，观察消息实时同步

---

#### 3. Blazor WebAssembly

关键文件：

| 文件 | 说明 |
|------|------|
| `src/Client/Pages/Index.razor` | 首页/登录页 |
| `src/Client/Pages/ChatRoom.razor` | 聊天室页面 |
| `src/Client/Program.cs` | 客户端入口 |
| `src/Client/_Imports.razor` | 全局using |

**学习要点**：
- Blazor组件生命周期 (`OnInitializedAsync`, `OnAfterRenderAsync`)
- 依赖注入 (`@inject`)
- 数据绑定 (`@bind`, `@onclick`)
- 状态管理

---

#### 4. 共享模型

查看 `src/Shared/Models/` 目录：

```csharp
// ChatMessage.cs - 聊天消息
public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public MessageType Type { get; set; } = MessageType.Text;
    public string? RoomId { get; set; }
}

// User.cs - 用户
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ChatRoom.cs - 聊天室
public class ChatRoom
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
    public string? Password { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**学习要点**：模型设计原则、数据流转

---

### 阶段二：客户端篇

#### 5. SignalR Hub 服务端开发

查看 `src/Server/Hubs/ChatHub.cs`：

```csharp
public class ChatHub : Hub
{
    private readonly IChatRepository _chatRepository;
    private readonly IUserConnectionManager _connectionManager;
    
    // 客户端调用：发送消息
    public async Task SendMessage(ChatMessage message)
    {
        message.Timestamp = DateTime.UtcNow;
        await _chatRepository.SaveMessageAsync(message);
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
    
    // 生命周期：连接建立
    public override async Task OnConnectedAsync()
    {
        // 生成默认用户名
        var userName = $"User_{Guid.NewGuid().ToString()[..4]}";
        _connectionManager.AddConnection(Context.ConnectionId, userName);
        await Clients.Caller.SendAsync("SetDefaultUserName", userName);
        await BroadcastUserListAsync();
    }
    
    // 生命周期：连接断开
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionManager.RemoveConnection(Context.ConnectionId);
        await BroadcastUserListAsync();
    }
}
```

**相关文件**：
- `src/Server/Services/IChatRepository.cs` - 消息仓储接口
- `src/Server/Services/ChatRepository.cs` - 消息仓储实现
- `src/Server/Services/UserConnectionManager.cs` - 连接管理

---

#### 6. 客户端连接服务

查看 `src/Client/Services/ChatService.cs`：

```csharp
public class ChatService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    
    public event Action<ChatMessage>? MessageReceived;
    public event Action<string>? UserJoined;
    public event Action<string>? UserLeft;
    public event Action<IReadOnlyList<string>>? UserListUpdated;
    
    public async Task InitializeAsync(string hubUrl, string? token = null)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                if (!string.IsNullOrEmpty(token))
                {
                    options.AccessTokenProvider = () => Task.FromResult(token);
                }
            })
            .WithAutomaticReconnect()
            .AddMessagePackProtocol()
            .Build();
        
        RegisterHandlers();
        await _hubConnection.StartAsync();
    }
    
    private void RegisterHandlers()
    {
        _hubConnection.On<ChatMessage>("ReceiveMessage", message =>
        {
            MessageReceived?.Invoke(message);
        });
        // ... 其他事件处理
    }
    
    public async Task SendMessageAsync(string text, string roomId)
    {
        var message = new ChatMessage
        {
            Message = text,
            RoomId = roomId,
            Timestamp = DateTime.UtcNow
        };
        await _hubConnection.SendAsync("SendMessage", message);
    }
}
```

**相关文件**：
- `src/Client/Services/SignalRConnectionService.cs` - 独立的连接服务
- `src/Client/Services/UserStateService.cs` - 用户状态管理

---

#### 7. 认证系统

查看认证相关文件：

| 文件 | 说明 |
|------|------|
| `src/Server/Controllers/AuthController.cs` | 认证API |
| `src/Client/Services/AuthService.cs` | 客户端认证服务 |
| `src/Shared/Models/Requests.cs` | 认证请求模型 |
| `src/Shared/Models/Responses.cs` | 认证响应模型 |

**认证流程**：
```
1. 用户注册 → /api/auth/register
2. 用户登录 → /api/auth/login → 返回JWT Token
3. 客户端保存Token到localStorage
4. SignalR连接时携带Token
5. 服务端验证Token
```

---

#### 8. 房间管理系统

查看房间相关文件：

| 文件 | 说明 |
|------|------|
| `src/Server/Services/RoomService.cs` | 房间服务 |
| `src/Client/Services/RoomService.cs` | 客户端房间服务 |
| `src/SignalRDemo.Application/Commands/Rooms/` | 房间命令 |

**房间功能**：
- 创建房间（公开/私人）
- 加入房间（需要密码验证）
- 离开房间
- 房间用户列表

---

### 阶段三：架构篇

#### 9. Domain 领域层

查看 `src/SignalRDemo.Domain/` 目录：

```
SignalRDemo.Domain/
├── Aggregates/
│   ├── AggregateRoot.cs     # 聚合根基类
│   ├── ChatRoom.cs          # 聊天室聚合
│   └── User.cs              # 用户聚合
├── Entities/
│   └── ChatMessage.cs       # 消息实体
├── ValueObjects/
│   ├── EntityId.cs          # 实体ID基类
│   ├── RoomName.cs          # 房间名值对象
│   ├── UserName.cs          # 用户名值对象
│   └── Password.cs          # 密码值对象
├── Events/
│   └── DomainEvents.cs      # 领域事件
├── Exceptions/
│   └── DomainException.cs    # 领域异常
└── Repositories/
    ├── IUserRepository.cs    # 用户仓储接口
    ├── IRoomRepository.cs   # 房间仓储接口
    └── IMessageRepository.cs# 消息仓储接口
```

**核心概念**：

```csharp
// 聚合根示例 - ChatRoom
public class ChatRoom : AggregateRoot
{
    public RoomName Name { get; private set; }
    public bool IsPublic { get; private set; }
    private string? _password;
    
    public void Join(User user, string? password)
    {
        if (!IsPublic && _password != password)
        {
            throw new DomainException("密码错误");
        }
        // 业务逻辑
    }
}

// 值对象示例 - RoomName
public class RoomName : ValueObject
{
    public string Value { get; }
    
    private RoomName(string value)
    {
        Value = value;
    }
    
    public static RoomName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("房间名不能为空");
        return new RoomName(value);
    }
}
```

---

#### 10. Application 应用层

查看 `src/SignalRDemo.Application/` 目录：

```
SignalRDemo.Application/
├── Commands/
│   ├── Messages/
│   │   └── SendMessageCommand.cs
│   ├── Rooms/
│   │   ├── CreateRoomCommand.cs
│   │   ├── JoinRoomCommand.cs
│   │   └── LeaveRoomCommand.cs
│   └── Users/
│       ├── LoginCommand.cs
│       └── RegisterUserCommand.cs
├── Handlers/
│   ├── SendMessageHandler.cs
│   ├── CreateRoomHandler.cs
│   ├── LoginHandler.cs
│   └── RegisterUserHandler.cs
├── DTOs/
│   ├── ChatMessageDto.cs
│   ├── RoomDto.cs
│   └── UserDto.cs
└── Results/
    └── Result.cs
```

**CQRS模式**：

```csharp
// 命令 - CreateRoomCommand.cs
public class CreateRoomCommand : IRequest<Result<RoomDto>>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = true;
    public string? Password { get; set; }
}

// 处理器 - CreateRoomHandler.cs
public class CreateRoomHandler : IRequestHandler<CreateRoomCommand, Result<RoomDto>>
{
    private readonly IRoomRepository _roomRepository;
    
    public async Task<Result<RoomDto>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var room = ChatRoom.Create(request.Name, request.IsPublic, request.Password);
        await _roomRepository.AddAsync(room);
        
        return Result<RoomDto>.Success(RoomDto.FromEntity(room));
    }
}
```

---

#### 11. Infrastructure 基础设施层

查看 `src/SignalRDemo.Infrastructure/` 目录：

```
SignalRDemo.Infrastructure/
├── Services/
│   ├── ChatRepository.cs       # 消息仓储实现
│   ├── RoomService.cs          # 房间服务实现
│   ├── UserService.cs          # 用户服务实现
│   └── UserConnectionManager.cs# SignalR连接管理
└── Repositories/
    ├── InMemoryMessageRepository.cs
    ├── InMemoryRoomRepository.cs
    └── InMemoryUserRepository.cs
```

**仓储模式**：

```csharp
// 仓储接口 - Domain层定义
public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByUserNameAsync(string userName);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}

// 仓储实现 - Infrastructure层
public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    
    public Task<User?> GetByIdAsync(string id)
    {
        return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    }
    
    public Task AddAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }
    // ...
}
```

---

#### 12. MediatR 集成

查看 `src/Server/Program.cs`：

```csharp
// 注册MediatR
builder.Services.AddMediatR(typeof(RegisterUserHandler).Assembly);

// 在Hub中使用MediatR
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    
    public ChatHub(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task SendMessage(ChatMessage message)
    {
        var command = new SendMessageCommand
        {
            UserId = message.UserId,
            UserName = message.UserName,
            Content = message.Message,
            RoomId = message.RoomId
        };
        
        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            await Clients.All.SendAsync("ReceiveMessage", result.Data);
        }
    }
}
```

---

## 🔧 实践练习

### 练习1：运行项目

```bash
# 1. 克隆项目
git clone https://github.com/wubing7755/SignalRDemo.git
cd SignalRDemo

# 2. 还原依赖
dotnet restore

# 3. 运行项目
dotnet run --project src/Server/SignalRDemo.Server.csproj

# 4. 浏览器访问
# https://localhost:7002
```

### 练习2：添加新功能

尝试添加"消息撤回"功能：

1. **Domain层**：在ChatMessage添加IsDeleted属性
2. **Application层**：创建RevokeMessageCommand和Handler
3. **Infrastructure层**：实现消息撤回逻辑
4. **Server层**：在ChatHub中添加RevokeMessage方法
5. **Client层**：添加撤回按钮UI

### 练习3：重构代码

将现有的ChatService拆分为多个单一职责服务：
- SignalRConnectionService - 连接管理
- MessageService - 消息业务
- RoomService - 房间业务

---

## 📚 参考资料

| 资源 | 链接 |
|------|------|
| SignalR官方文档 | https://docs.microsoft.com/aspnet/core/signalr |
| Blazor官方文档 | https://docs.microsoft.com/aspnet/core/blazor |
| DDD参考书籍 | 《领域驱动设计》- Eric Evans |
| CQRS参考 | https://docs.microsoft.com/aspnet/core/tutorials/first-mvc-app |

---

## ❓ 常见问题

### Q1: 项目无法编译？

检查：
1. .NET SDK版本是否6.0+
2. 是否执行了`dotnet restore`
3. 参照错误信息安装缺失的NuGet包

### Q2: SignalR连接失败？

检查：
1. 服务器是否启动
2. CORS配置是否正确
3. 防火墙是否阻止端口

### Q3: 如何调试？

- 服务端：在Visual Studio中设置断点
- 客户端：使用浏览器开发者工具查看Network和Console

---

## 📝 学习检查清单

- [ ] 项目能成功运行
- [ ] 理解SignalR双向通信原理
- [ ] 理解Blazor组件生命周期
- [ ] 理解DDD各层职责
- [ ] 能修改现有功能
- [ ] 能添加新功能

---

## 🚀 下一步

完成基础学习后，可以：

1. **深入DDD**：学习更多DDD模式（领域服务、应用服务、工厂等）
2. **性能优化**：学习SignalR性能调优
3. **分布式部署**：使用Redis后端实现多服务器部署
4. **持久化**：将内存存储替换为数据库（SQLite/PostgreSQL）

---

## 📞 获取帮助

- 提交Issue: https://github.com/wubing7755/SignalRDemo/issues
- 参与讨论: https://github.com/wubing7755/SignalRDemo/discussions
