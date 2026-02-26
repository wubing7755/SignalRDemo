# SignalR 实时聊天室（Blazor WebAssembly）

![Blazor WASM](https://img.shields.io/badge/Blazor-Web-assembly-blueviolet)
![.NET](https://img.shields.io/badge/.NET-6.0%2B-blue)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![EN](https://img.shields.io/badge/Language-English-blue)](README.en-US.md)
[![CN](https://img.shields.io/badge/语言-中文-red)](README.md)

一个基于 **Blazor WebAssembly** 和 **ASP.NET Core SignalR** 的实时在线多人聊天室示例项目，采用 **DDD（领域驱动设计）架构**，包含完整的CQRS实现。

---

## ✨ 项目特性

- 💬 实时消息发送与接收
- 👥 多用户在线聊天
- 🟢 在线用户状态显示
- ⏱️ 消息时间戳
- 🧑 用户身份系统（注册/登录）
- 🔐 私人房间密码保护
- 🔄 SignalR 实时双向通信
- 🔌 自动重连机制
- 📡 连接状态实时指示
- 🏥 健康检查端点

---

## 🧱 技术栈

| 模块 | 技术 |
|------|------|
| 前端 | Blazor WebAssembly (.NET 6.0.36) |
| 后端 | ASP.NET Core (.NET 6.0) |
| 实时通信 | SignalR |
| 架构模式 | DDD + CQRS (MediatR) |
| 认证 | JWT |
| 共享模型 | .NET 6.0 Class Library |

---

## 📂 项目结构

```
SignalRDemo/
├── Client/                           # Blazor WebAssembly 客户端
│   ├── Pages/
│   │   ├── ChatRoom.razor           # 聊天室主页面
│   │   └── Index.razor              # 主页
│   ├── Services/
│   │   ├── ChatService.cs            # SignalR 连接服务
│   │   ├── AuthService.cs            # 认证服务
│   │   └── RoomService.cs            # 房间服务
│   ├── Components/                   # Blazor 组件
│   ├── Shared/
│   └── wwwroot/
│
├── Server/                           # ASP.NET Core 服务端
│   ├── Hubs/
│   │   └── ChatHub.cs               # SignalR Hub
│   ├── Controllers/
│   │   ├── AuthController.cs         # 认证API
│   │   └── StatsController.cs        # 统计API
│   ├── Services/
│   │   └── SignalRHealthCheck.cs     # 健康检查
│   └── Program.cs
│
├── Shared/                           # 共享类库
│   └── Models/
│       ├── ChatMessage.cs            # 聊天消息模型
│       ├── ChatRoom.cs               # 聊天室模型
│       ├── User.cs                   # 用户模型
│       ├── Requests.cs                # 请求DTO
│       ├── Responses.cs              # 响应DTO
│       └── MessageType.cs            # 消息类型枚举
│
├── SignalRDemo.Application/          # 应用层 (CQRS)
│   ├── Commands/                     # 命令
│   │   ├── Messages/
│   │   ├── Rooms/
│   │   └── Users/
│   ├── Handlers/                     # 命令处理器
│   ├── DTOs/                         # 数据传输对象
│   └── Results/                      # 结果封装
│
├── SignalRDemo.Domain/               # 领域层
│   ├── Aggregates/                    # 聚合根
│   │   ├── ChatRoom.cs
│   │   └── User.cs
│   ├── Entities/                     # 实体
│   │   └── ChatMessage.cs
│   ├── ValueObjects/                 # 值对象
│   │   ├── EntityId.cs
│   │   ├── RoomName.cs
│   │   ├── UserName.cs
│   │   └── ...
│   ├── Events/                       # 领域事件
│   ├── Exceptions/                   # 领域异常
│   └── Repositories/                 # 仓储接口
│
└── SignalRDemo.Infrastructure/       # 基础设施层
    ├── Services/                      # 服务实现
    │   ├── ChatRepository.cs
    │   ├── RoomService.cs
    │   ├── UserService.cs
    │   └── UserConnectionManager.cs
    └── Repositories/                  # 仓储实现
        ├── InMemoryMessageRepository.cs
        ├── InMemoryRoomRepository.cs
        └── InMemoryUserRepository.cs
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
dotnet run --project src/Server/SignalRDemo.Server.csproj
```

4. 浏览器访问

- [https://localhost:7002](https://localhost:7002)
- [http://localhost:5293](http://localhost:5293)

---

## 🏗️ 架构说明

本项目采用 **DDD（领域驱动设计）** 架构，结合 **CQRS** 模式实现。

### 领域层 (Domain)

包含核心业务逻辑：
- **聚合根**：ChatRoom, User
- **实体**：ChatMessage
- **值对象**：EntityId, RoomName, UserName, etc.
- **仓储接口**：定义数据访问契约

### 应用层 (Application)

使用 MediatR 实现 CQRS：
- **命令 (Commands)**：SendMessageCommand, CreateRoomCommand, JoinRoomCommand, LoginCommand, etc.
- **处理器 (Handlers)**：处理命令并返回结果

### 基础设施层 (Infrastructure)

实现领域层定义的接口：
- **服务**：UserService, RoomService, ChatRepository, UserConnectionManager
- **仓储**：InMemoryUserRepository, InMemoryRoomRepository, InMemoryMessageRepository

### 服务端 (Server)

- **SignalR Hub**：处理实时通信
- **Controllers**：提供REST API（认证、统计）
- **健康检查**：监控服务状态

---

## 📖 适用场景

- 🎓 学习 DDD 架构设计
- ⚡ SignalR 实时通信实战
- 💬 即时聊天/通知系统原型
- 🤝 CQRS 模式实践

---

## 📄 许可证

本项目基于 **MIT License** 开源，欢迎自由使用与修改。

---

## 🙌 贡献

欢迎提交 Issue 或 Pull Request，一起完善这个示例项目。

---

## 📞 联系方式

- GitHub: [wubing7755/SignalRDemo](https://github.com/wubing7755/SignalRDemo)
