# SignalRDemo 聊天室系统实现方案

本文档描述 SignalRDemo 项目的完整实现方案，作为后续开发的依据。

---

## 目录

- [1. 系统定义](#1-系统定义)
- [2. 配色方案](#2-配色方案)
- [3. 首页设计](#3-首页设计)
- [4. 聊天页面设计](#4-聊天页面设计)
- [5. 数据模型](#5-数据模型)
- [6. 服务端接口](#6-服务端接口)
- [7. 客户端服务](#8-客户端服务)
- [8. 实现步骤](#8-实现步骤)

---

## 1. 系统定义

### 1.1 房间概念

房间是聊天的基础组织单元，可类比为：
- Discord 中的"服务器"或"频道"
- Slack 中的"工作区"或"频道"
- QQ 群、微信群

**房间属性：**
| 属性 | 类型 | 说明 |
|------|------|------|
| Id | string | 房间唯一ID |
| Name | string | 房间名称 |
| Description | string | 房间描述 |
| OwnerId | string | 创建者用户ID |
| IsPublic | bool | 是否公共房间 |
| Password | string? | 私人房间密码 |
| CreatedAt | DateTime | 创建时间 |
| MemberCount | int | 当前成员数 |

### 1.2 房间分类

#### 公共房间
- 无需密码，任何用户可直接加入
- 默认"大厅"为公共房间
- 首页显示所有公共房间列表

#### 私人房间
- 需要密码验证才能加入
- 创建时设置房间密码
- 用户加入时需输入正确密码

### 1.3 用户系统

#### 用户属性
| 属性 | 类型 | 说明 |
|------|------|------|
| Id | string | 用户唯一ID |
| UserName | string | 用户名（登录用） |
| DisplayName | string | 显示昵称 |
| PasswordHash | string | 密码哈希 |
| CreatedAt | DateTime | 注册时间 |
| LastLoginAt | DateTime | 最后登录时间 |

#### 用户流程
```
未登录用户
    ↓ 注册/登录
已登录用户
    ↓ 进入房间
可加入公共房间（无需验证）
    ↓ 输入密码
可加入私人房间（密码验证）
```

---

## 2. 配色方案

### 2.1 宝可梦风格配色

采用明亮、多彩、活泼的配色方案：

```css
:root {
    /* 主背景色 - 明亮系 */
    --bg-primary: #f8f9fa;
    --bg-secondary: #ffffff;
    --bg-tertiary: #e9ecef;
    
    /* 深色背景 - 对框/话侧边栏 */
    --bg-dark: #2d3436;
    --bg-darker: #1e272e;
    
    /* 主色调 - 鲜艳紫 */
    --primary: #6c5ce7;
    --primary-light: #a29bfe;
    --primary-dark: #5849be;
    
    /* 强调色 - 宝可梦风格 */
    --accent-red: #ff6b6b;      /* 红色 */
    --accent-blue: #4dabf7;      /* 蓝色 */
    --accent-green: #51cf66;     /* 绿色 */
    --accent-yellow: #ffd43b;    /* 黄色 */
    --accent-orange: #ff922b;    /* 橙色 */
    --accent-pink: #f06595;      /* 粉色 */
    --accent-cyan: #22b8cf;      /* 青色 */
    --accent-purple: #be4bdb;    /* 紫色 */
    
    /* 文本色 */
    --text-primary: #212529;
    --text-secondary: #495057;
    --text-muted: #868e96;
    --text-light: #f8f9fa;
    
    /* 功能色 */
    --success: #51cf66;
    --warning: #ffd43b;
    --error: #ff6b6b;
    --info: #4dabf7;
    
    /* 边框/分割线 */
    --border-color: #dee2e6;
    --divider-color: #e9ecef;
    
    /* 阴影 */
    --shadow-sm: 0 2px 4px rgba(0,0,0,0.05);
    --shadow-md: 0 4px 12px rgba(0,0,0,0.1);
    --shadow-lg: 0 8px 24px rgba(0,0,0,0.15);
    
    /* 圆角 */
    --radius-sm: 6px;
    --radius-md: 12px;
    --radius-lg: 20px;
    --radius-xl: 30px;
}
```

### 2.2 主题色应用

| 场景 | 使用颜色 |
|------|----------|
| 主要按钮 | `linear-gradient(135deg, var(--primary), var(--primary-dark))` |
| 成功提示 | `var(--accent-green)` |
| 警告提示 | `var(--accent-yellow)` |
| 错误提示 | `var(--accent-red)` |
| 房间分类标签 | 公共: `var(--accent-blue)`, 私人: `var(--accent-orange)` |
| 在线状态 | `var(--accent-green)` |
| 离线状态 | `var(--text-muted)` |

---

## 3. 首页设计

### 3.1 页面布局

```
┌─────────────────────────────────────────────────────────────────┐
│  🏠 SignalChat                              [登录/注册] [关于我们]│
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                     📊 实时统计                           │  │
│  │                                                           │  │
│  │    ┌─────────┐    ┌─────────┐                            │  │
│  │    │  💬    │    │  👥    │                            │  │
│  │    │ 12     │    │  47     │                            │  │
│  │    │ 在线房间│    │ 在线人数│                            │  │
│  │    └─────────┘    └─────────┘                            │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    🚀 快速开始                             │  │
│  │                                                           │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │  [🎮 进入公共大厅]                                  │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  │                                                           │  │
│  │    或者                                                  │  │
│  │                                                           │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │  [➕ 创建新房间]          [🚪 加入房间]             │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                   🏠 推荐公共房间                          │  │
│  │                                                           │  │
│  │  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐         │  │
│  │  │ #大厅   │ │ #游戏   │ │ #音乐   │ │ #技术   │         │  │
│  │  │ 👥 15   │ │ 👥 8    │ │ 👥 5    │ │ 👥 12   │         │  │
│  │  │ 🟢 公开 │ │ 🟢 公开 │ │ 🟢 公开 │ │ 🟢 公开 │         │  │
│  │  └─────────┘ └─────────┘ └─────────┘ └─────────┘         │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  © 2026 SignalChat | GitHub | 联系我们                         │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 首页组件

#### 统计面板 (StatsPanel)
```
- 在线房间数：实时从服务器获取
- 在线人数：当前所有房间用户总数
- 更新频率：每30秒刷新一次
```

#### 快速开始 (QuickStart)
```
- 进入公共大厅：一键加入默认公共房间
- 创建新房间：弹出创建表单
- 加入房间：弹出加入表单（需输入房间名和密码）
```

#### 推荐房间 (FeaturedRooms)
```
- 显示前8个公共房间
- 按在线人数排序
- 点击直接加入
```

### 3.3 首页弹窗

#### 创建房间弹窗
```
┌─────────────────────────────────┐
│  🚪 创建新房间                   │
├─────────────────────────────────┤
│                                 │
│  房间名称 *     [────────────]  │
│                                 │
│  房间描述       [────────────]  │
│                                 │
│  ┌───────────────────────────┐│
│  │ ○ 公共房间                 ││
│  │   无需密码，任何人可加入    ││
│  └───────────────────────────┘│
│                                 │
│  ┌───────────────────────────┐│
│  │ ● 私人房间                 ││
│  │   需要密码才能加入          ││
│  └───────────────────────────┘│
│                                 │
│  房间密码 *     [────────────]  │
│  (私人房间必填)                 │
│                                 │
│  ┌───────────────────────────┐│
│  │ [取消]    [🚀 创建房间]   ││
│  └───────────────────────────┘│
└─────────────────────────────────┘
```

#### 加入房间弹窗
```
┌─────────────────────────────────┐
│  🚪 加入房间                     │
├─────────────────────────────────┤
│                                 │
│  房间名称/ID  [────────────]    │
│                                 │
│  ┌───────────────────────────┐│
│  │ 房间列表（可选）          ││
│  │ ──────────────────────── ││
│  │ #大厅                    ││
│  │ #游戏                    ││
│  │ #音乐                    ││
│  └───────────────────────────┘│
│                                 │
│  房间密码       [────────────]  │
│  (私人房间需要)                 │
│                                 │
│  ┌───────────────────────────┐│
│  │ [取消]    [🚪 加入房间]   ││
│  └───────────────────────────┘│
└─────────────────────────────────┘
```

#### 用户登录/注册弹窗
```
┌─────────────────────────────────┐
│  👤 用户认证                    │
├─────────────────────────────────┤
│                                 │
│  ┌───────────────────────────┐  │
│  │ ● 登录                     │  │
│  │ ○ 注册                     │  │
│  └───────────────────────────┘  │
│                                 │
│  用户名/邮箱  [────────────]    │
│                                 │
│  密码          [────────────]    │
│                                 │
│  ┌───────────────────────────┐│
│  │ [取消]    [🔐 登录/注册]   ││
│  └───────────────────────────┘│
└─────────────────────────────────┘
```

---

## 4. 聊天页面设计

### 4.1 整体布局

```
┌─────────────────────────────────────────────────────────────────────┐
│  🏠 Logo    [📍 大厅 ▼]                           👤 User ▼  [⚙️] │
├──────────┬─────────────────────────────────────┬──────────────────┤
│          │                                     │                  │
│ 💬 房间  │  ┌─────────────────────────────┐   │  👥 在线 (15)    │
│          │  │                             │   │                  │
│ ─────── │  │  ┌───────────────────────┐ │   │  ────────────   │
│ 🏠 大厅  │  │  │ 🔒 欢迎来到 #大厅       │ │   │  👤 张三       │
│ (当前)   │  │  └───────────────────────┘ │   │  👤 李四       │
│          │  │                             │   │  👤 王五       │
│ 🟢 公开  │  │  ┌───────────────────────┐ │   │  👤 赵六       │
│ 👥 12    │  │  │ 👤 张三                 │ │   │  👤 钱七       │
│          │  │  │ 大家好！很高兴认识大家   │ │   │  👤 孙八       │
│ ─────── │  │  │ [10:30]                 │ │   │  👤 周九       │
│ 📁 我的  │  └─────────────────────────────┘   │  ...           │
│          │                                     │                  │
│ 📁 游戏  │  ┌───────────────────────┐ ┌───── │────────────────┐ │
│ 📁 音乐  │  │ 👤 我                   │ │      │  👤 +3 更多    │ │
│ 📁 技术  │  │ 今天天气真好！☀️         │ │      └────────────────┘ │
│ 📁 闲聊  │  │ [10:32]                 │ │                        │
│          │  └───────────────────────┘ │                        │
│          │                             │                        │
│ ─────── │  ┌───────────────────────┐ │                        │
│ 🔐 私人  │  │ 👤 李四                 │ │                        │
│ 📁 私人A │  │ 是啊！一起去玩吗？       │ │                        │
│ 📁 私人B │  │ [10:33]                 │ │                        │
│          │  └───────────────────────┘ │                        │
│ [+] 创建 │                             │                        │
│          │  ┌─────────────────────────────────────────────┐  │
│          │  │ 💬                                            │  │
│          │  │ ┌───────────────────────────────────────────┐│  │
│          │  │ │ 输入消息...                                ││  │
│          │  │ └───────────────────────────────────────────┘│  │
│          │  │              [😀] [🖼️]     [📤 发送]        │  │
│          │  └─────────────────────────────────────────────┘  │
│          │                                     |              │
├──────────┴─────────────────────────────────────┴──────────────┤
│  🔌 已连接    [💬 消息] [👤 私聊] [🚪 房间]                    │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.2 左侧边栏 - 房间列表

#### 分组结构
```
房间列表
├── 🏠 大厅 (公共)
│   ├── #大厅 [👥 12]
│
├── 📁 我的房间
│   ├── #游戏 [👥 8]
│   ├── #音乐 [👥 5]
│   ├── #技术 [👥 12]
│   └── #闲聊 [👥 3]
│
├── 🔐 私人房间
│   ├── #好友聚会 [🔒] [👥 4]
│   ├── #项目讨论 [🔒] [👥 2]
│   └── #生日惊喜 [🔒] [👥 6]
│
└── [+] 创建房间
```

#### 房间项样式
```css
.room-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 10px 12px;
    border-radius: var(--radius-md);
    cursor: pointer;
    transition: all 0.2s;
}

.room-item:hover {
    background: var(--bg-tertiary);
}

.room-item.active {
    background: var(--primary);
    color: white;
}

.room-icon {
    width: 36px;
    height: 36px;
    border-radius: var(--radius-md);
    display: flex;
    align-items: center;
    justify-content: center;
}

.room-icon.public {
    background: linear-gradient(135deg, var(--accent-blue), var(--accent-cyan));
}

.room-icon.private {
    background: linear-gradient(135deg, var(--accent-orange), var(--accent-red));
}

.room-name {
    flex: 1;
    font-weight: 500;
}

.room-count {
    font-size: 0.85rem;
    color: var(--text-muted);
}

.room-item.active .room-count {
    color: rgba(255,255,255,0.8);
}

.room-lock {
    color: var(--accent-orange);
}
```

### 4.3 中间区域 - 消息列表

#### 消息气泡样式

**发送的消息（右侧）**
```css
.message.sent {
    flex-direction: row-reverse;
}

.message.sent .message-bubble {
    background: linear-gradient(135deg, var(--primary), var(--primary-dark));
    color: white;
    border-radius: var(--radius-lg) var(--radius-lg) var(--radius-sm) var(--radius-lg);
}

.message.sent .message-time {
    color: rgba(255,255,255,0.7);
}
```

**接收的消息（左侧）**
```css
.message.received {
    flex-direction: row;
}

.message.received .message-bubble {
    background: var(--bg-tertiary);
    color: var(--text-primary);
    border-radius: var(--radius-lg) var(--radius-lg) var(--radius-lg) var(--radius-sm);
}

.message.received .message-time {
    color: var(--text-muted);
}
```

**系统消息**
```css
.system-message {
    text-align: center;
    padding: 8px;
    color: var(--text-muted);
    font-size: 0.85rem;
}
```

#### 消息内容格式

**文本消息**
```
┌─────────────────────────────────────┐
│ 👤 用户名                        │ 10:30 │
│ 消息内容...                            │
└─────────────────────────────────────┘
```

**带表情的消息**
```
┌─────────────────────────────────────┐
│ 👤 用户名                        │ 10:30 │
│ 今天心情真好！😊☀️                   │
└─────────────────────────────────────┘
```

**图片消息**
```
┌─────────────────────────────────────┐
│ 👤 用户名                        │ 10:30 │
│ [🖼️ 图片预览]                        │
│ 图片标题/描述                       │
└─────────────────────────────────────┘
```

### 4.4 右侧边栏 - 用户列表

#### 样式
```css
.user-item {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 8px 12px;
    border-radius: var(--radius-md);
    cursor: pointer;
    transition: all 0.2s;
}

.user-item:hover {
    background: var(--bg-tertiary);
}

.user-avatar {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-weight: 600;
    font-size: 0.9rem;
}

.user-name {
    flex: 1;
    font-weight: 500;
}

.user-status {
    display: flex;
    align-items: center;
    gap: 4px;
    font-size: 0.75rem;
}

.status-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
}

.status-dot.online {
    background: var(--accent-green);
}

.status-dot.away {
    background: var(--accent-yellow);
}

.status-dot.offline {
    background: var(--text-muted);
}

.online-count {
    font-size: 0.85rem;
    color: var(--accent-green);
    font-weight: 600;
}
```

### 4.5 输入区域 - 卡片样式

```css
.input-card {
    background: var(--bg-secondary);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-md);
    padding: 16px;
    margin: 16px;
}

.input-actions {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 8px;
}

.input-action-btn {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    border: none;
    background: var(--bg-tertiary);
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 1.2rem;
    transition: all 0.2s;
}

.input-action-btn:hover {
    background: var(--primary-light);
    transform: scale(1.1);
}

.message-input-area {
    flex: 1;
    border: 2px solid var(--border-color);
    border-radius: var(--radius-lg);
    padding: 12px 16px;
    font-size: 1rem;
    resize: none;
    min-height: 60px;
    max-height: 120px;
    transition: border-color 0.2s;
}

.message-input-area:focus {
    outline: none;
    border-color: var(--primary);
}

.send-btn {
    padding: 12px 24px;
    background: linear-gradient(135deg, var(--primary), var(--primary-dark));
    color: white;
    border: none;
    border-radius: var(--radius-lg);
    font-weight: 600;
    cursor: pointer;
    transition: all 0.2s;
}

.send-btn:hover:not(:disabled) {
    transform: translateY(-2px);
    box-shadow: var(--shadow-md);
}

.send-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}
```

---

## 5. 数据模型

### 5.1 用户模型 (User.cs)

```csharp
namespace SignalRDemo.Shared.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
```

### 5.2 房间模型 (ChatRoom.cs)

```csharp
namespace SignalRDemo.Shared.Models;

public class ChatRoom
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public bool IsPublic { get; set; } = true;
    public string? Password { get; set; }  // 存储密码哈希
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int MemberCount { get; set; } = 0;
}
```

### 5.3 消息模型 (ChatMessage.cs - 已存在，需扩展)

```csharp
namespace SignalRDemo.Shared.Models;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public string? MediaUrl { get; set; }
    public string? AltText { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum MessageType
{
    Text = 0,
    Image = 1,
    Emoji = 2,
    File = 3
}
```

### 5.4 用户房间关联 (UserRoom.cs)

```csharp
namespace SignalRDemo.Shared.Models;

public class UserRoom
{
    public string UserId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public RoomRole Role { get; set; } = RoomRole.Member;
}

public enum RoomRole
{
    Owner = 0,
    Admin = 1,
    Member = 2
}
```

---

## 6. 服务端接口

### 6.1 SignalR Hub 方法

```csharp
public class ChatHub : Hub
{
    // ========== 用户相关 ==========
    
    /// <summary>用户注册</summary>
    public async Task Register(RegisterRequest request);
    
    /// <summary>用户登录</summary>
    public async Task<LoginResponse> Login(LoginRequest request);
    
    /// <summary>设置显示昵称</summary>
    public async Task SetDisplayName(string displayName);
    
    /// <summary>登出</summary>
    public async Task Logout();
    
    // ========== 房间相关 ==========
    
    /// <summary>创建房间</summary>
    public async Task<ChatRoom> CreateRoom(CreateRoomRequest request);
    
    /// <summary>获取房间列表</summary>
    public async Task<List<ChatRoom>> GetRooms();
    
    /// <summary>获取我的房间列表</summary>
    public async Task<List<ChatRoom>> GetMyRooms();
    
    /// <summary>加入房间</summary>
    public async Task<JoinRoomResponse> JoinRoom(JoinRoomRequest request);
    
    /// <summary>离开房间</summary>
    public async Task LeaveRoom(string roomId);
    
    /// <summary>验证房间密码</summary>
    public async Task<bool> VerifyRoomPassword(string roomId, string password);
    
    // ========== 消息相关 ==========
    
    /// <summary>发送消息到房间</summary>
    public async Task SendMessage(SendMessageRequest request);
    
    /// <summary>获取房间消息历史</summary>
    public async Task<List<ChatMessage>> GetRoomMessages(string roomId, int count = 50);
    
    /// <summary>获取最近的公共消息</summary>
    public async Task<List<ChatMessage>> GetRecentMessages(int count = 50);
}
```

### 6.2 API 控制器 (可选)

```csharp
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    // 可用于用户注册、登录验证等
}
```

### 6.3 服务层接口

```csharp
// 用户服务
public interface IUserService
{
    Task<User?> RegisterAsync(string userName, string password);
    Task<User?> LoginAsync(string userName, string password);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByUserNameAsync(string userName);
}

// 房间服务
public interface IRoomService
{
    Task<ChatRoom> CreateRoomAsync(string name, string? description, string ownerId, bool isPublic, string? password);
    Task<List<ChatRoom>> GetPublicRoomsAsync();
    Task<List<ChatRoom>> GetUserRoomsAsync(string userId);
    Task<ChatRoom?> GetRoomByIdAsync(string roomId);
    Task<bool> VerifyPasswordAsync(string roomId, string password);
    Task<bool> AddUserToRoomAsync(string userId, string roomId);
    Task<bool> RemoveUserFromRoomAsync(string userId, string roomId);
    Task<int> GetRoomMemberCountAsync(string roomId);
}

// 消息服务
public interface IMessageService
{
    Task SaveMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetRoomMessagesAsync(string roomId, int count);
    Task<List<ChatMessage>> GetRecentMessagesAsync(int count);
}
```

---

## 7. 客户端服务

### 7.1 ChatService 扩展方法

```csharp
public partial class ChatService
{
    // ========== 用户认证 ==========
    
    public Task<User?> RegisterAsync(string userName, string password);
    public Task<User?> LoginAsync(string userName, string password);
    public Task LogoutAsync();
    public bool IsLoggedIn => CurrentUser != null;
    
    // ========== 房间管理 ==========
    
    public Task<List<ChatRoom>> GetRoomsAsync();
    public Task<List<ChatRoom>> GetMyRoomsAsync();
    public Task<ChatRoom> CreateRoomAsync(CreateRoomRequest request);
    public Task<JoinRoomResponse> JoinRoomAsync(JoinRoomRequest request);
    public Task LeaveRoomAsync(string roomId);
    
    // ========== 消息 ==========
    
    public Task SendMessageAsync(string roomId, string message, MessageType type = MessageType.Text);
    public Task<List<ChatMessage>> GetRoomMessagesAsync(string roomId, int count = 50);
    
    // ========== 事件 ==========
    
    public event Action<ChatMessage>? RoomMessageReceived;
    public event Action<ChatRoom>? RoomCreated;
    public event Action<string, string>? UserJoinedRoom;  // userName, roomId
    public event Action<string, string>? UserLeftRoom;     // userName, roomId
    public event Action<List<ChatRoom>>? RoomsUpdated;
    public event Action<List<ChatMessage>>? RoomMessagesLoaded;
}
```

### 7.2 认证服务 (AuthService.cs)

```csharp
public class AuthService
{
    private User? _currentUser;
    
    public User? CurrentUser => _currentUser;
    public bool IsLoggedIn => _currentUser != null;
    
    public event Action<User?>? OnAuthStateChanged;
    
    public async Task<User?> LoginAsync(string userName, string password);
    public async Task<User?> RegisterAsync(string userName, string password, string displayName);
    public Task LogoutAsync();
    
    // Token 管理（如果使用 JWT）
    private string? _accessToken;
    public string? AccessToken => _accessToken;
}
```

---

## 8. 实现步骤

### 阶段一：数据模型和服务 (预计 2-3 天)

| 步骤 | 内容 | 文件 |
|------|------|------|
| 1.1 | 创建 User 模型 | `Shared/Models/User.cs` |
| 1.2 | 创建 ChatRoom 模型 | `Shared/Models/ChatRoom.cs` |
| 1.3 | 扩展 ChatMessage | `Shared/Models/ChatMessage.cs` |
| 1.4 | 创建 UserRoom 模型 | `Shared/Models/UserRoom.cs` |
| 1.5 | 实现 IUserService | `Server/Services/UserService.cs` |
| 1.6 | 实现 IRoomService | `Server/Services/RoomService.cs` |
| 1.7 | 扩展 IChatRepository | `Server/Services/IChatRepository.cs` |
| 1.8 | 更新 InMemoryChatRepository | `Server/Services/InMemoryChatRepository.cs` |

### 阶段二：SignalR Hub 扩展 (预计 1-2 天)

| 步骤 | 内容 | 文件 |
|------|------|------|
| 2.1 | 扩展 ChatHub 方法 | `Server/Hubs/ChatHub.cs` |
| 2.2 | 添加房间分组 | `Groups` |
| 2.3 | 实现密码验证 | `VerifyRoomPassword` |
| 2.4 | 实现房间消息广播 | `SendRoomMessage` |

### 阶段三：客户端服务 (预计 1-2 天)

| 步骤 | 内容 | 文件 |
|------|------|------|
| 3.1 | 创建 AuthService | `Client/Services/AuthService.cs` |
| 3.2 | 扩展 ChatService | `Client/Services/ChatService.cs` |
| 3.3 | 添加认证事件 | |
| 3.4 | 添加房间管理事件 | |

### 阶段四：UI 组件 - 首页 (预计 2-3 天)

| 步骤 | 内容 | 文件 |
|------|------|------|
| 4.1 | 创建 StatsPanel | `Client/Components/StatsPanel.razor` |
| 4.2 | 创建 QuickStart | `Client/Components/QuickStart.razor` |
| 4.3 | 创建 FeaturedRooms | `Client/Components/FeaturedRooms.razor` |
| 4.4 | 创建 CreateRoomModal | `Client/Components/CreateRoomModal.razor` |
| 4.5 | 创建 JoinRoomModal | `Client/Components/JoinRoomModal.razor` |
| 4.6 | 创建 AuthModal | `Client/Components/AuthModal.razor` |
| 4.7 | 更新 Index.razor | `Client/Pages/Index.razor` |

### 阶段五：UI 组件 - 聊天页面 (预计 2-3 天)

| 步骤 | 内容 | 文件 |
|------|------|------|
| 5.1 | 更新 RoomList | `Client/Components/RoomList.razor` |
| 5.2 | 更新 MessageArea | `Client/Components/MessageArea.razor` |
| 5.3 | 更新 UserList | `Client/Components/UserList.razor` |
| 5.4 | 创建 InputArea | `Client/Components/InputArea.razor` |
| 5.5 | 更新 ChatRoom.razor | `Client/Pages/ChatRoom.razor` |

### 阶段六：样式优化 (预计 1 天)

| 步骤 | 内容 | 文件 |
|------|------|------|
| 6.1 | 更新全局变量 | `Client/Shared/MainLayout.razor.css` |
| 6.2 | 优化首页样式 | `Client/Pages/Index.razor.css` |
| 6.3 | 优化聊天页面样式 | `Client/Pages/ChatRoom.razor.css` |
| 6.4 | 添加组件样式 | `Client/Components/*.razor.css` |

### 阶段七：测试和修复 (预计 1-2 天)

| 步骤 | 内容 |
|------|------|
| 7.1 | 集成测试所有功能 |
| 7.2 | 修复 Bug 和样式问题 |
| 7.3 | 性能优化 |

---

## 附录

### A. 依赖项

```xml
<!-- 添加到 Server/SignalRDemo.Server.csproj -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="6.0.0" />
```

```xml
<!-- 添加到 Client/SignalRDemo.Client.csproj -->
<!-- Blazor WebAssembly 已内置支持 -->
```

### B. 数据库选择

**开发阶段**：继续使用内存存储 (InMemoryChatRepository)

**生产阶段**：
- SQLite：简单部署，单文件
- PostgreSQL：生产环境推荐
- Redis：缓存和实时状态

### C. 安全考虑

1. **密码存储**：使用 BCrypt 或 PBKDF2
2. **Token**：JWT 认证
3. **输入验证**：服务器端验证所有输入
4. **XSS 防护**：HTML 编码用户输入
5. **连接限制**：单用户最大连接数

---

*文档版本：2.0*
*最后更新：2026年2月11日*
*基于用户反馈优化：宝可梦风格配色、房间权限系统、用户登录注册*
