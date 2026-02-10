# SignalR 实时聊天室（Blazor WebAssembly）

一个基于 **Blazor WebAssembly** 和 **ASP.NET Core SignalR** 的实时在线多人聊天室示例项目，采用标准的 **Blazor WASM 托管模型**，包含 Client、Server 和 Shared 三个项目，用于演示实时双向通信的完整实现流程。

---

## ✨ 项目特性

- 💬 实时消息发送与接收
- 👥 多用户在线聊天
- 🟢 在线用户状态显示
- ⏱️ 消息时间戳
- 🧑 简单的用户身份标识
- 🔄 支持 SignalR 实时双向通信

---

## 🧱 技术栈

| 模块 | 技术 |
|---|---|
| 前端 | Blazor WebAssembly (.NET 6) |
| 后端 | ASP.NET Core (.NET 6) |
| 实时通信 | SignalR |
| 共享模型 | .NET 6 Class Library |

---

## 📂 项目结构

```text
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
│   └── Program.cs              # 客户端入口
│
├── Server/                     # ASP.NET Core 服务端
│   ├── Hubs/
│   │   └── ChatHub.cs          # SignalR Hub
│   ├── Pages/
│   ├── Program.cs              # 服务端入口
│   ├── appsettings.json
│   └── SignalRDemo.Server.csproj
│
└── Shared/                     # 共享类库
    ├── Models/
    │   ├── ChatMessage.cs      # 聊天消息模型
    │   └── UserConnection.cs   # 用户连接模型
    └── SignalRDemo.Shared.csproj
````

---

## 🚀 快速开始

### 环境要求

* .NET 6 SDK 或更高版本
* Visual Studio 2022 / VS Code（可选）

### 运行步骤

1. 克隆仓库

```bash
git clone <your-repo-url>
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

* [https://localhost:7002](https://localhost:7002)
* [http://localhost:5293](http://localhost:5293)

---

## 🛠️ 实现步骤说明

项目按照循序渐进的方式实现，适合学习 SignalR 与 Blazor WASM 的完整集成流程。

### 1️⃣ 项目初始化

* 验证 Blazor WebAssembly 托管模型
* 确认 Client / Server / Shared 三个项目结构
* 确保项目可正常构建与运行

### 2️⃣ 添加 SignalR 相关包

* **Server**

  * `Microsoft.AspNetCore.SignalR`
* **Client**

  * `Microsoft.AspNetCore.SignalR.Client`
* **Shared（可选）**

  * `Microsoft.AspNetCore.SignalR.Common`

### 3️⃣ 定义共享模型

* 聊天消息模型（`ChatMessage`）
* 用户连接信息模型（`UserConnection`）
* 统一客户端与服务端数据结构

### 4️⃣ 实现 SignalR Hub

* 创建 `ChatHub`
* 实现：

  * 消息广播
  * 用户加入 / 离开通知
  * 客户端方法调用

### 5️⃣ 服务端配置

* 注册 SignalR 服务
* 映射 Hub 路由
* 配置 CORS，支持 WASM 客户端访问

### 6️⃣ 客户端 SignalR 连接

* 创建 SignalR 连接管理服务
* 管理连接状态
* 配置自动重连策略

### 7️⃣ 聊天室 UI

* 聊天主界面布局
* 消息列表展示
* 输入框与发送按钮
* 在线用户列表

### 8️⃣ 消息收发

* 发送消息至 Hub
* 监听服务器广播
* 实时刷新消息列表

### 9️⃣ 用户状态管理

* 简单用户标识
* 在线用户显示
* 加入 / 离开系统提示
* 连接状态指示

### 🔟 优化与测试

* 消息时间戳格式化
* 消息历史记录
* UI 与交互体验优化
* 端到端功能测试

---

## 📖 适用场景

* 学习 SignalR 实时通信
* Blazor WebAssembly 实战示例
* 即时聊天 / 通知系统原型
* 实时协作应用基础模板

---

## 📄 许可证

本项目基于 **MIT License** 开源，欢迎自由使用与修改。

---

## 🙌 贡献

欢迎提交 Issue 或 Pull Request，一起完善这个示例项目。
