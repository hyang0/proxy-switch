# proxy shortcut — Windows 系统代理快捷切换工具

一个轻量的 WinForms 托盘应用，使用全局快捷键一键开启/关闭系统代理，并在托盘与气泡中提示当前状态。支持通过配置文件自定义快捷键。

## 功能
- F7 切换系统代理开启/关闭（可配置）
- F8 退出程序（可配置）
- 托盘图标显示代理状态，切换时弹出气泡通知
- 使用注册表修改系统代理开关：`HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings\ProxyEnable`（0 关闭，1 开启）

## 环境要求
- Windows
- .NET SDK（项目默认 `net10.0-windows`）
  - 若本机不支持 .NET 10，可将 [shortcut.csproj](file:///d:/gitRepo/shortcut/shortcut.csproj) 的 `TargetFramework` 改为你已安装的版本，如 `net8.0-windows`

## 运行
```bash
dotnet run
```
或使用 Visual Studio 直接启动。

首次运行后，程序隐藏到托盘：
- 按快捷键切换代理
- 托盘右键菜单可退出

## 快捷键配置
配置文件：[appsettings.json](file:///d:/gitRepo/shortcut/appsettings.json)
```json
{
  "Hotkeys": {
    "Toggle": "F7",
    "Exit": "F8"
  }
}
```
- 支持组合键：`Ctrl`、`Alt`、`Shift`、`Win`（不区分大小写）
- 以 `+` 分隔，例如：
  - `"Ctrl+Alt+F7"`
  - `"Ctrl+Shift+P"`
  - `"Win+F8"`
- 非修饰部分会作为具体键解析（使用 `System.Windows.Forms.Keys` 枚举），如 `A`、`F7`、`NumPad1` 等
- 解析失败将回退到默认：Toggle=F7，Exit=F8

配置加载与解析逻辑参考：[Program.cs](file:///d:/gitRepo/shortcut/Program.cs) 中的 `HotkeyConfigLoader`。

## 工作原理
- 通过 `RegisterHotKey` 注册系统级热键（F7/F8 默认）
- 读写注册表：
  - 路径：`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings`
  - 值：`ProxyEnable`（REG_DWORD），`0` 表示禁用代理，`1` 表示启用代理
- 某些应用可能需要重新建立网络连接或重启才能立即感知代理变化

相关实现参考：
- 入口与窗体：[Program.cs](file:///d:/gitRepo/shortcut/Program.cs)
- 项目配置：[shortcut.csproj](file:///d:/gitRepo/shortcut/shortcut.csproj)


## 目录结构（关键文件）
- [Program.cs](file:///d:/gitRepo/shortcut/Program.cs)：托盘应用、热键注册、注册表读写、通知提示
- [shortcut.csproj](file:///d:/gitRepo/shortcut/shortcut.csproj)：WinForms 配置，目标框架
- [appsettings.json](file:///d:/gitRepo/shortcut/appsettings.json)：快捷键配置
- [.gitignore](file:///d:/gitRepo/shortcut/.gitignore)、[.gitattributes](file:///d:/gitRepo/shortcut/.gitattributes)

## 常见问题
- 无法使用 `Ctrl+C` 结束：本应用为 `WinExe`（GUI）类型，建议使用配置的退出热键（默认 F8）或托盘右键退出
- 构建报错 NETSDK1136：请将 `TargetFramework` 设置为 `*-windows`，如 `net10.0-windows` 或 `net8.0-windows`

