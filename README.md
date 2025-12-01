# MexKeypad

A simple but customizable remote keypad.

一个简单但可定制的远程键盘。

## 子项目

### `MexShared`：共享库

包含一些可重用结构。

### `MexKeypad`：主项目

作为本地软键盘，或远程键盘服务端时，只支持支持 Windows。\
作为远程键盘客户端时支持 Windows 和 Android。

支持用户导入 XAML 作为键盘。支持 Virtual Key、Scan Code、Unicode 三种输入方式。键盘 XAML 编写方式可参考嵌入键盘 [numpad.xaml](MexKeypad/Resources/Raw/numpad.xaml)、[f13-24.xaml](MexKeypad/Resources/Raw/f13-24.xaml) 和解析逻辑 [KeyInfo.cs](MexShared/KeyInfo.cs)。如需投入生产环境，建议对内容进行检查或过滤，防止出现安全问题。

协议本身支持 TCP/UDP，但没有安全传输（如加密和鉴权）的功能，建议仅在本地进行连接。如需投入生产环境，建议自行添加包装以保证数据安全和进行权限控制。

### `MexForwarder`：端口转发

一个 `MexKeypad` 协议的轻量 UDP 端口转发工具，支持跨平台部署。不适用于通用用途，不提供安全传输，可能存在漏洞。用于测试和内部使用，不建议在生产环境部署。
