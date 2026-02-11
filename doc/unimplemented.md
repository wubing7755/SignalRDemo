# 未实现功能列表

本文档列出 `doc/Suggestion.md` 规格说明中尚未实现的功能项。

---

## 1. 首页设计 (第三章)

### 1.1 统计面板 (StatsPanel)
- [ ] 缺少实时更新功能（文档要求每30秒刷新一次）
- [ ] 未连接到实时数据源获取在线房间数和在线人数

### 1.2 快速开始 (QuickStart) - **未创建组件**
- [ ] 进入公共大厅按钮
- [ ] 创建新房间按钮
- [ ] 加入房间按钮

### 1.3 创建房间弹窗 (CreateRoomModal)
- [ ] 缺少房间类型选择（公共/私人单选按钮）
- [ ] 缺少私人房间密码设置功能
- [ ] 缺少房间描述字段

### 1.4 首页整体布局
- [ ] 未实现文档中的实时统计区域
- [ ] 未实现"推荐公共房间"区域（已有 FeaturedRooms，但功能不完整）

---

## 2. 配色方案 (第二章)

### 2.1 全局 CSS 变量
- [ ] 未采用文档指定的宝可梦风格配色方案
- [ ] 当前使用深色主题 (#1a1a2e)，文档要求明亮系背景 (--bg-primary: #f8f9fa)

### 2.2 需要添加的 CSS 变量
```css
/* 以下变量未定义 */
--primary: #6c5ce7
--primary-light: #a29bfe
--primary-dark: #5849be
--accent-red: #ff6b6b
--accent-blue: #4dabf7
--accent-green: #51cf66
--accent-yellow: #ffd43b
--accent-orange: #ff922b
--accent-pink: #f06595
--accent-cyan: #22b8cf
--accent-purple: #be4bdb
--bg-primary: #f8f9fa
--bg-secondary: #ffffff
--bg-tertiary: #e9ecef
--radius-sm: 6px
--radius-md: 12px
--radius-lg: 20px
--radius-xl: 30px
```

---

## 3. 聊天页面设计 (第四章)

### 3.1 输入区域 (InputArea) - **未创建组件**
- [ ] 缺少消息输入框
- [ ] 缺少表情按钮 (😀)
- [ ] 缺少图片按钮 (🖼️)
- [ ] 缺少发送按钮
- [ ] 缺少输入卡片样式

### 3.2 消息气泡样式
- [ ] 未完全实现文档中的消息气泡样式
- [ ] 发送消息未使用渐变色背景
- [ ] 消息时间样式未实现

### 3.3 用户列表 (UserList)
- [ ] 组件存在但未完全按照文档设计实现
- [ ] 缺少在线状态指示器（绿色/黄色/灰色点）
- [ ] 缺少用户头像颜色生成逻辑

### 3.4 房间列表 (RoomList)
- [ ] 组件存在但缺少分组显示
- [ ] 缺少"我的房间"分组
- [ ] 缺少"私人房间"分组
- [ ] 房间图标样式未实现

---

## 4. 服务端接口 (第六章)

### 4.1 API 控制器
- [ ] 缺少 ChatController (文档6.2节推荐的可选功能)
- [ ] 缺少用于用户注册、登录验证的 API 端点

### 4.2 JWT 认证
- [ ] 缺少 JWT Bearer 认证配置
- [ ] 未添加 Microsoft.AspNetCore.Authentication.JwtBearer 包引用

---

## 5. 客户端服务 (第七章)

### 5.1 独立的 AuthService
- [ ] 缺少独立的 AuthService.cs 文件
- [ ] 认证逻辑目前集成在 ChatService 中

### 5.2 ChatService 事件
- [ ] 缺少 UserJoinedRoom 事件 (文档7.2节)
- [ ] 缺少 UserLeftRoom 事件 (文档7.2节)
- [ ] 缺少 RoomMessagesLoaded 事件 (文档7.2节)

---

## 6. 缺失的组件文件

| 组件 | 路径 | 状态 |
|------|------|------|
| QuickStart | Client/Components/QuickStart.razor | ❌ 未创建 |
| InputArea | Client/Components/InputArea.razor | ❌ 未创建 |
| InputArea.razor.css | Client/Components/InputArea.razor.css | ❌ 未创建 |
| QuickStart.razor.css | Client/Components/QuickStart.razor.css | ❌ 未创建 |
| ChatController | Server/Controllers/ChatController.cs | ❌ 未创建 |
| AuthService | Client/Services/AuthService.cs | ❌ 未创建 |

---

## 7. 安全功能

### 7.1 密码存储
- [ ] 当前使用 PBKDF2（已实现）
- [ ] 建议改用 BCrypt 以获得更好的安全性

### 7.2 Token 管理
- [ ] 缺少 JWT Token 生成和验证
- [ ] 缺少 AccessToken 管理（文档7.3节提到）

---

## 8. 测试和优化 (文档第八阶段)

- [ ] 未进行完整的集成测试
- [ ] 未进行性能优化
- [ ] 缺少 Bug 修复记录

---

## 优先级排序

### 高优先级
1. 实现宝可梦风格配色方案
2. 完成 CreateRoomModal 的公共/私人房间选择和密码功能
3. 创建 InputArea 组件
4. 实现消息气泡样式

### 中优先级
1. 实现实时统计更新功能
2. 创建 QuickStart 组件
3. 完善 UserList 和 RoomList 组件样式
4. 实现分组显示功能

### 低优先级
1. 添加 JWT 认证
2. 创建独立的 AuthService
3. 添加 ChatController API

---

*生成时间: 2026年2月11日*
*基于 doc/Suggestion.md 文档版本 2.0*
