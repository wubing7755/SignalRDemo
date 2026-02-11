# SignalR 实时双向通信完整指南

> 基于 SignalRDemo 项目的学习笔记

---

## 一、SignalR 概述

### 1.1 什么是 SignalR？

**SignalR** 是 ASP.NET Core 的一个库，用于实现**实时 Web 功能**。它使得服务器可以将消息即时推送到连接的客户端，而无需客户端轮询服务器。

### 1.2 适用场景

| 场景 | 示例 |
|------|------|
| 实时聊天 | 在线客服、聊天室 |
| 实时通知 | 消息提醒、状态更新 |
| 协作编辑 | 多人文档编辑 |
| 实时数据 | 股票行情、IoT 数据 |
| 游戏 | 多人在线游戏 |

### 1.3 SignalR 的优势

- **自动协议选择**：优先使用 WebSocket，回退到其他传输方式
- **连接管理**：自动处理连接/断开、重连
- **分组广播**：支持向特定组发送消息
- **强类型 Hub**：支持类型安全的调用
- **扩展示性**：可以自定义协议和传输方式

---

## 二、双向通信流程图

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              SignalR 通信架构                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌──────────────┐         HTTP/WebSocket          ┌──────────────┐        │
│   │              │  ───────────────────────────→  │              │        │
│   │   客户端      │                                  │   服务端      │        │
│   │  (Blazor)    │  ←───────────────────────────  │  (ASP.NET)   │        │
│   │              │         推送消息                 │              │        │
│   └──────────────┘                                  └──────────────┘        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 消息流向

```
                    ┌──────────────────────┐
                    │   1. 客户端 → 服务端   │
                    │   (SendAsync)         │
                    └──────────┬───────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────────┐
│                         完整消息流程                                   │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   客户端                                      服务端                  │
│     │                                           │                   │
│     │  1. 调用 SendAsync("Method", data)        │                   │
│     │ ───────────────────────────────────────→  │                   │
│     │                                           │                   │
│     │  2. HubConnection 序列化                  │                   │
│     │     (JSON/MessagePack)                    │                   │
│     │                                           │                   │
│     │  3. 通过 WebSocket 发送                   │                   │
│     │                                           │                   │
│     │                                    4. SignalR Middleware 接收    │
│     │                                    5. HubDispatcher 调度        │
│     │                                    6. 调用 Hub 方法             │
│     │                                           │                   │
│     │                                    7. 处理业务逻辑              │
│     │                                    8. 调用 Clients 广播         │
│     │                                           │                   │
│     │  9. 服务器推送消息 ←────────────────────  │                   │
│     │                                           │                   │
│     │ 10. On<T> 注册的回调触发                  │                   │
│     │                                           │                   │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 三、核心概念详解

### 3.1 Hub（集线器）

**Hub** 是 SignalR 的核心组件，相当于一个"通信中心"。

```csharp
// Server/Hubs/ChatHub.cs
public class ChatHub : Hub
{
    // 可直接注入服务
    private readonly IChatRepository _chatRepository;
    
    public ChatHub(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }
    
    // 客户端可调用的方法
    public async Task SendMessage(ChatMessage message)
    {
        // 处理消息
        await _chatRepository.SaveMessageAsync(message);
        
        // 广播给所有客户端
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
    
    // 生命周期方法
    public override async Task OnConnectedAsync()
    {
        await Clients.All.SendAsync("UserJoined", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}
```

### 3.2 HubConnection（客户端连接）

**HubConnection** 是客户端用于连接服务器的类。

```csharp
// Client/Services/ChatService.cs
public class ChatService
{
    private HubConnection? _hubConnection;
    
    public async Task InitializeAsync(string hubUrl)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)                          // 服务器地址
            .WithAutomaticReconnect();                 // 自动重连
            .AddMessagePackProtocol();                 // MessagePack 协议（可选）
            .Build();
        
        // 注册事件处理
        SetupEventHandlers();
        
        // 启动连接
        await _hubConnection.StartAsync();
    }
}
```

### 3.3 传输协议

SignalR 自动选择最佳传输方式：

| 优先级 | 协议 | 特点 |
|--------|------|------|
| 1 | WebSocket | 全双工、低延迟、首选 |
| 2 | Server-Sent Events | 单向推送 |
| 3 | Long Polling | 兼容性强、适用于所有浏览器 |

```csharp
// 强制使用特定传输方式（可选）
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl, options =>
    {
        options.Transports = HttpTransportType.WebSockets;
    })
    .Build();
```

### 3.4 消息协议

| 协议 | 格式 | 特点 |
|------|------|------|
| JSON | 文本 | 通用、可读性好 |
| MessagePack | 二进制 | 更小更快（推荐） |

```csharp
// 使用 MessagePack
builder.Services.AddSignalR()
    .AddMessagePackProtocol();

客户端：
.AddMessagePackProtocol()
```

---

## 四、核心 API 详解

### 4.1 客户端 → 服务端

#### `SendAsync` - 发送消息（fire-and-forget）

```csharp
// 调用服务器方法，不等待返回值
await _hubConnection.SendAsync("SendMessage", chatMessage);
```

#### `InvokeAsync<T>` - 调用方法并获取结果

```csharp
// 调用服务器方法，等待返回值
var result = await _hubConnection.InvokeAsync<string>("GetUserName", userId);
```

#### `StreamAsync<T>` - 流式调用

```csharp
// 接收流式数据
var stream = _hubConnection.StreamAsync<int>("Counter", 10);
await foreach (var item in stream)
{
    Console.WriteLine(item);
}
```

### 4.2 服务端 → 客户端

#### `Clients.All` - 广播给所有人

```csharp
// 发送给所有连接的客户端
await Clients.All.SendAsync("ReceiveMessage", message);
```

#### `Clients.Caller` - 发送给调用者

```csharp
// 只发送给当前调用的客户端
await Clients.Caller.SendAsync("ConfirmReceipt", true);
```

#### `Clients.Others` - 发送给除调用者外的所有人

```csharp
// 发送给其他所有人
await Clients.Others.SendAsync("UserJoined", userName);
```

#### `Clients.Client(connectionId)` - 发送给特定客户端

```csharp
// 根据 ConnectionId 发送给特定用户
await Clients.Client(connectionId).SendAsync("PrivateMessage", message);
```

#### `Clients.Group(groupName)` - 发送给分组

```csharp
// 发送给特定组
await Clients.Group("Admins").SendAsync("AdminMessage", message);
```

### 4.3 客户端注册事件

#### `On<T>` - 注册回调处理程序

```csharp
// 注册接收消息的回调
_hubConnection.On<ChatMessage>("ReceiveMessage", message =>
{
    // 处理接收到的消息
    Console.WriteLine($"收到消息: {message.User}: {message.Message}");
});

// 支持多个参数
_hubConnection.On<string, int>("ProcessOrder", (orderId, amount) =>
{
    // 处理
});
```

#### 取消注册

```csharp
// 移除特定事件的处理程序
_hubConnection.Remove("ReceiveMessage");

// 或重新创建连接
_hubConnection.Dispose();
_hubConnection = new HubConnectionBuilder().Build();
```

### 4.4 服务端 Hub 生命周期

```csharp
public class ChatHub : Hub
{
    // 连接建立时
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        await base.OnConnectedAsync();
    }
    
    // 连接断开时
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // exception 可能包含断开原因
        await base.OnDisconnectedAsync(exception);
    }
}
```

---

## 五、连接管理

### 5.1 启动连接

```csharp
public async Task ConnectAsync()
{
    try
    {
        await _hubConnection.StartAsync();
        Console.WriteLine("连接成功！");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"连接失败: {ex.Message}");
    }
}
```

### 5.2 断开连接

```csharp
public async Task DisconnectAsync()
{
    await _hubConnection.StopAsync();
}
```

### 5.3 自动重连

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect(new[] 
    {
        TimeSpan.Zero,           // 立即重试
        TimeSpan.FromSeconds(2), // 第2秒重试
        TimeSpan.FromSeconds(10),// 第10秒重试
        TimeSpan.FromSeconds(30)  // 第30秒重试
    })
    .Build();

// 监听重连事件
connection.Reconnecting += error =>
{
    Console.WriteLine("重连中...");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine($"已重连！ID: {connectionId}");
    return Task.CompletedTask;
};
```

### 5.4 连接状态

```csharp
// 获取当前状态
var state = _hubConnection.State;

// 状态枚举
enum HubConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Disconnecting
}
```

---

## 六、本项目代码详解

### 6.1 服务端 ChatHub

```csharp
public class ChatHub : Hub
{
    private readonly IChatRepository _chatRepository;
    private readonly IUserConnectionManager _connectionManager;
    
    // 1. 客户端发送消息
    public async Task SendMessage(ChatMessage message)
    {
        // 验证消息
        if (string.IsNullOrWhiteSpace(message.Message)) return;
        
        // 服务器设置时间戳（防止客户端篡改）
        message.Timestamp = DateTime.UtcNow;
        
        // 保存消息
        await _chatRepository.SaveMessageAsync(message);
        
        // 2. 广播给所有客户端
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
    
    // 3. 客户端设置用户名
    public async Task SetUserName(string userName)
    {
        _connectionManager.UpdateUserName(Context.ConnectionId, userName);
        await BroadcastUserListAsync();
    }
    
    // 4. 客户端连接时
    public override async Task OnConnectedAsync()
    {
        // 生成默认用户名
        var defaultUserName = $"User_{Guid.NewGuid().ToString()[..4]}";
        
        // 添加到连接管理器
        _connectionManager.AddConnection(Context.ConnectionId, defaultUserName);
        
        // 通知客户端默认用户名
        await Clients.Caller.SendAsync("SetDefaultUserName", defaultUserName);
        
        // 广播用户列表更新
        await BroadcastUserListAsync();
        
        await base.OnConnectedAsync();
    }
    
    // 5. 客户端断开时
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _connectionManager.RemoveConnection(Context.ConnectionId);
        await BroadcastUserListAsync();
        
        await base.OnDisconnectedAsync(exception);
    }
    
    private async Task BroadcastUserListAsync()
    {
        var connections = _connectionManager.GetAllConnections();
        var userNames = connections.Select(c => c.UserName).ToList();
        await Clients.All.SendAsync("UpdateUserList", userNames);
    }
}
```

### 6.2 客户端 ChatService

```csharp
public class ChatService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    
    // 事件（供 UI 层订阅）
    public event Action<ChatMessage>? MessageReceived;
    public event Action<string>? UserJoined;
    public event Action<string>? UserLeft;
    public event Action<IReadOnlyList<string>>? UserListUpdated;
    
    public async Task InitializeAsync(string hubUrl)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .AddMessagePackProtocol()
            .Build();
        
        // 注册事件处理
        SetupEventHandlers();
        
        // 监听连接状态
        _hubConnection.Reconnecting += ...
        _hubConnection.Reconnected += ...
        _hubConnection.Closed += ...
        
        // 启动连接
        await _hubConnection.StartAsync();
    }
    
    private void SetupEventHandlers()
    {
        // 接收消息
        _hubConnection.On<ChatMessage>("ReceiveMessage", message =>
        {
            MessageReceived?.Invoke(message);
        });
        
        // 用户加入
        _hubConnection.On<string>("UserJoined", userName =>
        {
            UserJoined?.Invoke(userName);
        });
        
        // 用户离开
        _hubConnection.On<string>("UserLeft", userName =>
        {
            UserLeft?.Invoke(userName);
        });
        
        // 用户列表更新
        _hubConnection.On<IReadOnlyList<string>>("UpdateUserList", userNames =>
        {
            UserListUpdated?.Invoke(userNames);
        });
    }
    
    // 发送消息
    public async Task SendMessageAsync(string text)
    {
        if (_hubConnection?.State != HubConnectionState.Connected) return;
        
        var message = new ChatMessage
        {
            User = CurrentUser,
            Message = text,
            Timestamp = DateTime.UtcNow
        };
        
        await _hubConnection.SendAsync("SendMessage", message);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
```

---

## 七、最佳实践

### 7.1 错误处理

```csharp
public async Task SendMessageAsync(string message)
{
    try
    {
        await _hubConnection.SendAsync("SendMessage", message);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"发送失败: {ex.Message}");
        // 重试或显示错误
    }
}
```

### 7.2 连接状态管理

```csharp
public class ChatService
{
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    
    public async Task EnsureConnectedAsync()
    {
        if (_hubConnection?.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
        }
    }
}
```

### 7.3 防抖处理

```csharp
// 防止用户快速点击发送按钮
private readonly DebounceService _debounceService = new();

public async Task SendMessageAsync(string text)
{
    await _debounceService.DebounceAsync(async () =>
    {
        await _hubConnection.SendAsync("SendMessage", text);
    }, delayMilliseconds: 300);
}
```

### 7.4 安全考虑

```csharp
// 服务端：认证
public override async Task OnConnectedAsync()
{
    var userId = Context.UserIdentifier;
    // 验证用户权限
    await base.OnConnectedAsync();
}

// 限制消息大小
public async Task SendMessage(ChatMessage message)
{
    if (message.Message.Length > 5000)
    {
        throw new HubException("消息过长");
    }
}
```

---

## 八、常见问题

### Q1: WebSocket 连接失败？

检查：
1. 服务器是否配置了 WebSocket 中间件
2. CORS 设置是否正确
3. 防火墙是否阻止了端口

### Q2: 消息顺序不一致？

SignalR 不保证消息顺序，建议在消息中添加时间戳，由客户端排序。

### Q3: 如何调试 SignalR？

使用浏览器开发者工具：
- Network 标签：查看 WebSocket 连接
- Console 标签：查看 SignalR 日志

### Q4: 大规模部署？

- 使用 Redis 后端实现多服务器部署
- 启用横向扩展

---

## 九、学习路线

```
入门
├── 1. 理解 Hub 概念
├── 2. 实现简单消息收发
├── 3. 学习连接管理
└── 进阶
    ├── 4. 分组通信
    ├── 5. 认证授权
    ├── 6. 性能优化
    └── 7. 分布式部署
```

---

## 参考资料

- [SignalR 官方文档](https://docs.microsoft.com/aspnet/core/signalr)
- [SignalR GitHub](https://github.com/aspnet/AspNetCore/tree/main/src/SignalR)
