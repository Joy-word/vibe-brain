# 🧠 vibe-brain

> 一杯咖啡，N 个 AI，一颗不够用的大脑。

---

## 这是什么？

某天喝完一杯咖啡，大脑过载，灵感喷涌，于是开了 4 个 Cursor 窗口同时跑 4 个 AI 任务。

然后……窗口 1 通知说需要你介入，你刚切过去准备看，窗口 2 也弹出来了。还没等你处理完，窗口 3、窗口 4 排队催你。

问题是：coding agent 的并发是真并发，而你这个**人类 agent，是单线程的**。多开之后，很容易陷入一种错觉——觉得自己也在"多线程"运行，实则只是在被 AI 的节律拖着走：它催你，你就跳；它卡住，你就焦虑。

**vibe-brain 就是为这个瓶颈而生的。**

它蹲在桌面角落，把每一条"AI 在等你"的通知变成一张任务卡，让你**按自己的节律逐一处理**，而不是被弹窗牵着鼻子走。决定什么时候去看结果，按自己的顺序来，在 AI 还在跑的间隙喝水、散步、摸鱼——你是在用 AI，不是在服务 AI。

---

## 核心功能

- **🔔 通知捕获** — 监听 Windows 系统通知中心，自动抓取 Cursor / VSCode / Windsurf / Claude 等 AI 编辑器的完成通知
- **📋 任务队列** — 每条通知变成一张任务卡，状态分为「待处理 → 进行中 → 已完成」
- **🎯 一键跳转** — 点击任务卡直接激活对应的编辑器窗口（精确到哪个项目的哪个窗口）
- **📌 窗口置顶** — 随时悬浮在所有窗口上方，不会被淹没
- **🧹 自动清理** — 标记完成后自动从 Windows 通知中心清除，保持干净

---

## 截图

![vibe-brain 截图](docs/screenshot.png)

---

## 使用场景

```
你：启动 Task A（Cursor 窗口 1）
你：启动 Task B（Cursor 窗口 2）
你：启动 Task C（VSCode 窗口）
你：去喝咖啡 ☕

vibe-brain：[Task B 完成] [Task A 完成] [Task C 报错]

你：按顺序处理，一个一个来，不慌。
```

---

## 安装 & 运行

### 环境要求

- Windows 10 (19041+) 或 Windows 11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022（含 .NET MAUI 工作负载）

### 运行

```bash
git clone https://github.com/your-username/vibe-brain.git
cd vibe-brain
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0
```

首次启动会弹出 Windows 权限请求，需要允许"通知访问"才能正常监听。

路径：**Windows 设置 → 系统 → 通知 → 允许应用访问通知**

---

## 支持的 AI 编辑器

| 编辑器 | 状态 |
|--------|------|
| Cursor | ✅ |
| VS Code | ✅ |
| Windsurf | ✅ |
| Claude Desktop | ✅ |
| GitHub Copilot | ✅ |
| Codex CLI | ✅ |

> 其他应用可在代码中手动添加到监听列表。

---

## 技术栈

- **.NET MAUI** — 跨平台 UI 框架（本项目仅用 Windows 目标）
- **WinRT UserNotificationListener** — Windows 系统通知 API
- **Win32 API (user32.dll)** — 窗口激活 / 置顶

---

## License

MIT — 随便用，随便改，记得喝咖啡。
