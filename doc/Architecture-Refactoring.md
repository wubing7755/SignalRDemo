# SignalR 聊天室架构重构文档

## 📋 重构概述

本次重构解决了原代码中的以下问题：

1. **双重认证机制混乱** - 统一使用 JWT Token 认证
2. **用户状态管理分散** - 添加用户状态持久化到 localStorage
3. **ChatService 职责过重** - 拆分为多个单一职责服务
4. **房间切换流程不清晰** - 完善房间生命周期管理
5. **事件命名不一致** - 规范化事件命名

---

## 🏗️ 新架构设计

### 服务职责划分

```
┌─────────────────────────────────────────────────────────────────┐
│                         客户端架构                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  UserStateService (IUserStateService)                   │   │
│  │  ├─ 用户登录状态管理                                      │   │
│  │  ├─ 用户信息持久化 (localStorage)                        │   │
│  │  └─ 认证状态变化事件                                      │   │
│  └─────────────────────────────────────────────────────────┘   │
│                           │                                      │
│  ┌────────────────────────┼─────────────────────────────────┐   │
│  │                        ▼                                 │   │
│  │  ┌─────────────────────────────────────────────────┐    │   │
│  │  │  SignalRConnectionService (ISignalRConnectionService) │   │
│  │  │  ├─ SignalR 连接管理                              │    │   │
│  │  │  ├─ 连接状态监控                                  │    │   │
│  │  │  └─ 原始消息收发                                  │    │   │
│  │  └─────────────────────────────────────────────────┘    │   │
│  │                           │                              │   │
│  │           ┌───────────────┼───────────────┐              │   │
│  │           ▼               ▼               ▼              │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐    │   │
│  │  │ RoomService  │ │MessageService│ │ 其他业务服务  │    │   │
│  │  │  (IRoomService)│ │(IMessageService)│ │              │    │   │
│  │  │  房间业务逻辑  │ │  消息业务逻辑  │ │              │    │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘    │   │
│  │                                                         │   │
│  └─────────────────────────────────────────────────────────┘   │
│                           │                                      │
│                           ▼                                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                     Blazor 组件                         │   │
│  │  (Index.razor, ChatRoom.razor, AuthDialog.razor...)     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📦 新增/修改文件

### 接口定义 (Abstractions)

| 文件 | 说明 |
|------|------|
| `ISignalRConnectionService.cs` | SignalR 连接服务接口 |
| `IUserStateService.cs` | 用户状态服务接口 |
| `IRoomService.cs` | 房间服务接口 |
| `IMessageService.cs` | 消息服务接口 |

### 服务实现

| 文件 | 说明 |
|------|------|
| `SignalRConnectionService.cs` | SignalR 连接管理（纯连接，无业务逻辑） |
| `UserStateService.cs` | 用户状态管理（支持持久化） |
| `RoomService.cs` | 房间业务逻辑 |
| `MessageService.cs` | 消息业务逻辑 |

### 修改的文件

| 文件 | 修改内容 |
|------|----------|
| `Program.cs` | 注册新服务，初始化用户状态 |
| `AuthService.cs` | 集成 UserStateService，添加新 API |

---

## 🔄 新架构使用指南

### 1. 获取当前用户信息

```csharp
@inject IUserStateService UserStateService

// 方式1：直接访问属性
@if (UserStateService.IsLoggedIn)
{
    <span>@UserStateService.CurrentUser?.DisplayName</span>
}

// 方式2：订阅状态变化
protected override void OnInitialized()
{
    UserStateService.AuthStateChanged += OnAuthStateChanged;
}

private void OnAuthStateChanged(User? user)
{
    // 用户登录/登出时触发
    InvokeAsync(StateHasChanged);
}
```

### 2. 登录流程

```csharp
@inject AuthService AuthService
@inject ISignalRConnectionService ConnectionService

private async Task HandleLogin(string userName, string password)
{
    // 新推荐方法：登录并设置用户状态
    var response = await AuthService.LoginAndSetUserAsync(userName, password);
    
    if (response.Success)
    {
        // 初始化 SignalR 连接（自动携带 JWT Token）
        var token = await AuthService.GetTokenAsync();
        await ConnectionService.InitializeAsync("/chathub", token);
    }
}
```

### 3. 房间管理

```csharp
@inject IRoomService RoomService

protected override void OnInitialized()
{
    // 订阅房间事件
    RoomService.RoomJoined += OnRoomJoined;
    RoomService.CurrentRoomChanged += OnCurrentRoomChanged;
    RoomService.PublicRoomsUpdated += OnPublicRoomsUpdated;
}

private async Task CreateRoom()
{
    await RoomService.CreateRoomAsync(
        name: "新房间",
        description: "房间描述",
        isPublic: true,
        password: null);
}

private async Task JoinRoom(string roomId)
{
    var response = await RoomService.JoinRoomAsync(roomId);
    if (response.Success)
    {
        // 成功加入房间
        Navigation.NavigateTo($"/chat?room={roomId}");
    }
}

private async Task SwitchRoom(string newRoomId)
{
    // 完整的房间切换流程
    // 1. 离开旧房间
    // 2. 加入新房间
    await RoomService.SwitchRoomAsync(newRoomId);
}
```

### 4. 消息管理

```csharp
@inject IMessageService MessageService
@inject IRoomService RoomService

protected override void OnInitialized()
{
    MessageService.MessageReceived += OnMessageReceived;
    MessageService.MessageHistoryLoaded += OnMessageHistoryLoaded;
}

private async Task SendMessage(string message)
{
    if (RoomService.CurrentRoomId != null)
    {
        await MessageService.SendMessageAsync(
            RoomService.CurrentRoomId, 
            message);
    }
}

private async Task LoadHistory()
{
    if (RoomService.CurrentRoomId != null)
    {
        await MessageService.LoadMessageHistoryAsync(
            RoomService.CurrentRoomId, 
            count: 50);
    }
}

private void OnMessageReceived(ChatMessage message)
{
    // 新消息到达
    _messages.Add(message);
    InvokeAsync(StateHasChanged);
}

private void OnMessageHistoryLoaded(IReadOnlyList<ChatMessage> messages)
{
    // 历史消息加载完成
    _messages = messages.ToList();
    InvokeAsync(StateHasChanged);
}
```

### 5. 完整的聊天室页面初始化

```csharp
@inject IUserStateService UserStateService
@inject ISignalRConnectionService ConnectionService
@inject IRoomService RoomService
@inject IMessageService MessageService
@inject AuthService AuthService
@inject NavigationManager Navigation

protected override async Task OnInitializedAsync()
{
    // 1. 检查登录状态
    if (!UserStateService.IsLoggedIn)
    {
        Navigation.NavigateTo("/");
        return;
    }

    // 2. 初始化 SignalR 连接
    var token = await AuthService.GetTokenAsync();
    var hubUrl = $"{Navigation.BaseUri.TrimEnd('/')}/chathub";
    await ConnectionService.InitializeAsync(hubUrl, token);

    // 3. 初始化房间服务
    await RoomService.InitializeAsync();
    
    // 4. 初始化消息服务
    await MessageService.InitializeAsync();

    // 5. 从 URL 获取房间ID并加入
    var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
    var roomId = Microsoft.AspNetCore.WebUtilities.QueryHelpers
        .ParseQuery(uri.Query)
        .TryGetValue("room", out var roomIdValue) 
        ? roomIdValue.ToString() 
        : "lobby";

    // 6. 加入房间并加载消息
    await RoomService.JoinRoomAsync(roomId);
    await MessageService.SwitchRoomAsync(roomId);
}
```

---

## 📊 新旧架构对比

### 原架构问题

```csharp
// ❌ 原代码：用户状态分散
// Index.razor
ChatService.SetCurrentUser(user);  // 设置到 ChatService
await AuthService.StoreTokenAsync(token);  // Token 存到 localStorage
// 用户信息没有持久化！

// ❌ 原代码：ChatService 职责过重
public class ChatService
{
    // 500+ 行代码，混合了：
    // - 连接管理
    // - 用户认证
    // - 房间管理
    // - 消息发送
    // - 10+ 事件
}
```

### 新架构优势

```csharp
// ✅ 新代码：用户状态集中管理
// 页面刷新后自动恢复
await AuthService.LoginAndSetUserAsync(userName, password);
// 用户信息自动持久化到 localStorage

// ✅ 新代码：职责分离
// 每个服务单一职责，代码清晰
public class SignalRConnectionService { /* 纯连接管理 */ }
public class RoomService { /* 房间业务 */ }
public class MessageService { /* 消息业务 */ }
public class UserStateService { /* 用户状态 */ }
```

---

## 🚀 迁移步骤

### 阶段1：使用新服务（并行运行）

1. 新页面可以使用新服务
2. 旧页面继续使用 ChatService
3. 两个体系可以共存

### 阶段2：逐步迁移

1. 逐个页面迁移到新架构
2. 测试验证每个页面
3. 删除旧 ChatService 的依赖

### 阶段3：清理旧代码

1. 删除旧 ChatService
2. 清理未使用的代码
3. 统一使用新接口

---

## 📁 持久化数据结构

```javascript
// localStorage 存储结构
{
    "signalchat_user": {
        "id": "user-guid",
        "userName": "username",
        "displayName": "显示名称",
        "createdAt": "2024-01-01T00:00:00Z",
        "lastLoginAt": "2024-01-01T12:00:00Z"
    },
    "AccessToken": "jwt-token-string",
    "RefreshToken": "refresh-token-string"
}
```

---

## ⚠️ 注意事项

1. **新服务与旧 ChatService 可以共存** - 不会破坏现有功能
2. **UserStateService 在 Program.cs 中自动初始化** - 页面刷新后自动恢复用户状态
3. **SignalRConnectionService 需要手动初始化** - 建议在页面初始化时调用
4. **RoomService 和 MessageService 需要初始化** - 注册 SignalR 事件处理器

---

## 🔧 扩展建议

1. **添加服务端用户状态接口** - `/api/auth/me` 获取当前用户信息
2. **添加连接状态监控组件** - 显示 SignalR 连接状态
3. **添加自动 Token 刷新** - Token 过期前自动刷新
4. **添加消息缓存清理** - 防止内存泄漏
