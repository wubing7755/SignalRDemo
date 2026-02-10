# SignalR 实时聊天室（Blazor WebAssembly）

![Blazor WASM](https://img.shields.io/badge/Blazor-WebAssembly-blueviolet)
![.NET](https://img.shields.io/badge/.NET-6.0%2B-blue)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![EN](https://img.shields.io/badge/Language-English-blue)](README.en-US.md)
[![CN](https://img.shields.io/badge/语言-中文-red)](README.md)

一个基于 **Blazor WebAssembly** 和 **ASP.NET Core SignalR** 的实时在线多人聊天室示例项目，采用标准的 **Blazor WASM 托管模型**，包含 Client、Server 和 Shared 三个项目，用于演示实时双向通信的完整实现流程。

---

## ✨ 项目特性

- 💬 实时消息发送与接收
- 👥 多用户在线聊天
- 🟢 在线用户状态显示
- ⏱️ 消息时间戳
- 🧑 简单的用户身份标识
- 🔄 SignalR 实时双向通信
- 🔌 自动重连机制
- 📡 连接状态实时指示

---

## 🧱 技术栈

| 模块 | 技术 |
|------|------|
| 前端 | Blazor WebAssembly (.NET 6.0.36) |
| 后端 | ASP.NET Core (.NET 6.0) |
| 实时通信 | SignalR |
| 共享模型 | .NET 6.0 Class Library |

---

## 📂 项目结构

```
SignalRDemo/
├── Client/                     # Blazor WebAssembly 客户端
│   ├── Pages/
│   │   ├── ChatRoom.razor      # 聊天室主页面
│   │   └── Index.razor         # 主页
│   ├── Services/
│   │   └── ChatService.cs      # SignalR 连接与通信服务
│   ├── Shared/
│   │   ├── MainLayout.razor
│   │   ├── NavMenu.razor
│   │   └── SurveyPrompt.razor
│   ├── wwwroot/
│   ├── App.razor
│   ├── _Imports.razor
│   ├── Program.cs              # 客户端入口
│   └── SignalRDemo.Client.csproj
│
├── Server/                     # ASP.NET Core 服务端
│   ├── Hubs/
│   │   └── ChatHub.cs          # SignalR Hub
│   ├── Pages/
│   │   ├── Error.cshtml        # 错误页面
│   │   └── Error.cshtml.cs
│   ├── Properties/
│   │   └── launchSettings.json # 启动配置
│   ├── Program.cs              # 服务端入口
│   ├── appsettings.json
│   └── SignalRDemo.Server.csproj
│
└── Shared/                     # 共享类库
    ├── Models/
    │   ├── ChatMessage.cs      # 聊天消息模型
    │   └── UserConnection.cs   # 用户连接模型
    └── SignalRDemo.Shared.csproj
```

---

## 🚀 快速开始

### 环境要求

- .NET 6.0 SDK 或更高版本
- Visual Studio 2022 / VS Code（可选）

### 运行步骤

1. 克隆仓库

```bash
git clone https://github.com/wubing7755/SignalRDemo.git
cd SignalRDemo
```

2. 还原依赖

```bash
dotnet restore
```

3. 启动服务器

```bash
dotnet run --project Server/SignalRDemo.Server.csproj
```

4. 浏览器访问

- [https://localhost:7002](https://localhost:7002)
- [http://localhost:5293](http://localhost:5293)

---

## 🛠️ 实现步骤说明

项目按照循序渐进的方式实现，适合学习 SignalR 与 Blazor WASM 的完整集成流程。

### 1️⃣ 项目初始化

- 验证 Blazor WebAssembly 托管模型
- 确认 Client / Server / Shared 三个项目结构
- 确保项目可正常构建与运行

### 2️⃣ 添加 SignalR 相关包

**Server**
- `Microsoft.AspNetCore.SignalR` (v1.1.0)
- `Microsoft.AspNetCore.Components.WebAssembly.Server` (v6.0.36)

**Client**
- `Microsoft.AspNetCore.SignalR.Client` (v6.0.36)
- `Microsoft.AspNetCore.Components.WebAssembly` (v6.0.36)

### 3️⃣ 定义共享模型

| 模型 | 说明 |
|------|------|
| `ChatMessage` | 聊天消息，包含用户、消息内容、时间戳 |
| `UserConnection` | 用户连接信息，包含用户ID、用户名、连接时间 |

### 4️⃣ 实现 SignalR Hub

**ChatHub.cs** 核心功能：

```csharp
public class ChatHub : Hub
{
    // 消息广播
    public async Task SendMessage(ChatMessage chatMessage)
    {
        await Clients.All.SendAsync("ReceiveMessage", chatMessage);
    }

    // 用户连接通知
    public override async Task OnConnectedAsync()
    {
        await Clients.All.SendAsync("UserConnected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    // 用户断开通知
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Clients.All.SendAsync("UserDisconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
```

### 5️⃣ 服务端配置

**Program.cs 关键配置：**

- 注册 SignalR 服务：`services.AddSignalR()`
- 映射 Hub 路由：`app.MapHub<ChatHub>("/chathub")`
- 配置 CORS，支持 WASM 客户端访问
- 启用 Blazor 文件服务：`app.UseBlazorFrameworkFiles()`

### 6️⃣ 客户端 SignalR 连接

**ChatService.cs 核心功能：**

- 创建 `HubConnection` 实例
- 配置 Hub URL 连接
- 注册消息处理程序（ReceiveMessage、UserConnected、UserDisconnected）
- 实现自动重连机制
- 提供 `SendMessageAsync` 发送消息

### 7️⃣ 聊天室 UI

- 聊天主界面布局
- 消息列表展示（支持时间戳格式化）
- 输入框与发送按钮
- 在线用户列表（基于 ConnectionId）
- 连接状态指示器

### 8️⃣ 消息收发机制

```
客户端发送 → Hub.SendMessage → 服务器广播 → 所有客户端接收
```

### 9️⃣ 用户状态管理

- 用户标识：自动生成 `User_XXXX` 格式用户名
- 可自定义设置用户名
- 在线/离线状态实时显示
- 连接状态指示（Connected/Disconnected/Connecting）

### 🔟 优化与测试

- 消息时间戳格式化（UTC 转换）
- 异常处理与错误提示
- 自动重连策略
- UI 与交互体验优化

---

## 📖 适用场景

- 🎓 学习 SignalR 实时通信
- ⚡ Blazor WebAssembly 实战示例
- 💬 即时聊天/通知系统原型
- 🤝 实时协作应用基础模板

---

## 📄 许可证

本项目基于 **MIT License** 开源，欢迎自由使用与修改。

---

## 🙌 贡献

欢迎提交 Issue 或 Pull Request，一起完善这个示例项目。

---

## 📞 联系方式

- GitHub: [wubing7755/SignalRDemo](https://github.com/wubing7755/SignalRDemo)
