# SignalR 聊天室进阶功能需求文档

本文档描述 SignalRDemo 项目的功能需求，作为后续开发的参考依据。

---

## 目录

- [SignalR 聊天室进阶功能需求文档](#signalr-聊天室进阶功能需求文档)
  - [目录](#目录)
  - [1. 项目背景](#1-项目背景)
  - [2. 功能需求](#2-功能需求)
    - [2.1 私聊功能](#21-私聊功能)
    - [2.2 房间功能](#22-房间功能)
    - [2.3 消息类型](#23-消息类型)
    - [2.4 消息回执](#24-消息回执)
    - [2.5 @提及](#25-提及)
  - [3. 基础设施](#3-基础设施)
    - [3.1 消息持久化](#31-消息持久化)
    - [3.2 用户认证](#32-用户认证)
    - [3.3 水平扩展](#33-水平扩展)
  - [4. 功能优先级](#4-功能优先级)
    - [P0 - 核心功能](#p0---核心功能)
    - [P1 - 重要功能](#p1---重要功能)
    - [P2 - 增强功能](#p2---增强功能)
    - [P3 - 运维功能](#p3---运维功能)
  - [开发说明](#开发说明)

---

## 1. 项目背景

**当前状态**：
- 实时消息发送与接收 ✅
- 多用户在线管理 ✅
- 连接状态显示 ✅
- 消息历史（内存存储）✅
- 防抖/节流发送机制 ✅

**目标**：扩展为功能完善的实时聊天应用

---

## 2. 功能需求

### 2.1 私聊功能

**需求描述**：
- 用户可选择在线用户发起一对一私聊
- 私聊消息仅发送方和接收方可见
- 支持消息状态追踪

**数据结构**：

| 字段 | 类型 | 说明 |
|------|------|------|
| FromUser | string | 发送方用户名 |
| ToUser | string | 接收方用户名 |
| Message | string | 消息内容 |
| Timestamp | DateTime | 发送时间 |
| Status | enum | 状态：Sent/Delivered/Read |

**服务端接口**：

| 方法 | 参数 | 返回 | 说明 |
|------|------|------|------|
| SendPrivateMessage | PrivateChatMessage | void | 发送私聊 |
| MarkMessageAsRead | messageId, fromUser | void | 标记已读 |

**客户端接口**：

| 方法 | 参数 | 说明 |
|------|------|------|
| SendPrivateMessageAsync | toUser, messageText | 发送私聊 |
| MarkMessageAsReadAsync | messageId, fromUser | 标记已读 |

**客户端事件**：

| 事件 | 参数 | 触发时机 |
|------|------|----------|
| PrivateMessageReceived | PrivateChatMessage | 收到私聊 |
| PrivateMessageStatusUpdated | PrivateChatMessage | 状态更新 |
| MessageRead | messageId | 对方已读 |

---

### 2.2 房间功能

**需求描述**：
- 支持创建多个聊天室
- 每个房间独立管理用户和消息
- 用户可加入/离开房间

**数据结构**：

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | string | 房间唯一ID |
| Name | string | 房间名称 |
| Description | string | 房间描述 |
| CreatedBy | string | 创建者 |
| CreatedAt | DateTime | 创建时间 |
| IsPublic | bool | 是否公开 |

| 字段 | 类型 | 说明 |
|------|------|------|
| RoomId | string | 所属房间 |
| User | string | 发送者 |
| Message | string | 消息内容 |
| Timestamp | DateTime | 发送时间 |

**服务端接口**：

| 方法 | 参数 | 说明 |
|------|------|------|
| CreateRoom | ChatRoom | 创建房间 |
| JoinRoom | roomId | 加入房间 |
| SendRoomMessage | roomId, message | 发送房间消息 |
| LeaveRoom | roomId | 离开房间 |
| GetRooms | void | 获取房间列表 |

**客户端事件**：

| 事件 | 参数 | 触发时机 |
|------|------|----------|
| UserJoinedRoom | userName, roomId | 用户加入房间 |
| UserLeftRoom | userName, roomId | 用户离开房间 |
| ReceiveRoomMessage | RoomMessage | 收到房间消息 |

---

### 2.3 消息类型

**需求描述**：
- 支持多种消息格式：文本、图片、文件、表情

**数据结构**：

| 字段 | 类型 | 说明 |
|------|------|------|
| User | string | 发送者 |
| Message | string | 消息内容 |
| Timestamp | DateTime | 发送时间 |
| Type | enum | 类型：Text/Image/File/Emoji |
| MediaUrl | string? | 媒体URL |
| AltText | string? | 替代文本 |

---

### 2.4 消息回执

**需求描述**：
- 追踪消息状态：Sent → Delivered → Read
- 支持批量标记已读

**状态枚举**：

| 状态 | 值 | 说明 |
|------|------|------|
| Sent | 0 | 已发送 |
| Delivered | 1 | 已送达 |
| Read | 2 | 已读 |

---

### 2.5 @提及

**需求描述**：
- 消息中可 @其他用户
- 被提及用户收到通知

---

## 3. 基础设施

### 3.1 消息持久化

**需求**：服务重启后消息不丢失

**方案选择**：
- SQLite：轻量级，无需额外服务
- PostgreSQL：生产环境推荐

**验收标准**：
- 消息持久化到数据库
- 支持历史消息查询

---

### 3.2 用户认证

**需求**：
- 用户注册/登录
- JWT Token 认证
- SignalR Hub 授权

**验收标准**：
- 未登录用户无法使用聊天功能
- Token 过期自动登出

---

### 3.3 水平扩展

**需求**：支持多服务器部署

**方案**：
- Redis Backplane：跨实例消息同步

**验收标准**：
- 多实例部署时消息正确广播

---

## 4. 功能优先级

### P0 - 核心功能

| 功能 | 预估工时 |
|------|----------|
| 消息持久化 | 4-6h |
| 私聊功能 | 3-4h |

### P1 - 重要功能

| 功能 | 预估工时 |
|------|----------|
| 房间功能 | 6-8h |
| 用户认证 | 8-12h |
| 消息类型 | 4-6h |

### P2 - 增强功能

| 功能 | 预估工时 |
|------|----------|
| 消息回执 | 2-3h |
| @提及 | 2-3h |

### P3 - 运维功能

| 功能 | 预估工时 |
|------|----------|
| Redis Backplane | 4-6h |
| 消息搜索 | 4-8h |
| Web Push | 6-8h |

---

## 开发说明

1. **迭代方式**：按功能模块逐步实现
2. **测试要求**：每个功能需有验收标准
3. **文档更新**：实现后更新本文档状态

---

*文档版本：1.0*
*最后更新：2026年2月*
