# MexKeypad

A simple but customizable remote keypad.

一个简单但可定制的远程键盘。

## 子项目

### `MexShared`：共享库

包含一些可重用结构。

### `MexKeypad`：主项目

作为本地软键盘，或远程键盘服务端时，只支持 Windows。\
作为远程键盘客户端时支持 Windows 和 Android。

支持用户导入 XAML 作为键盘。支持 Virtual Key、Scan Code、Unicode 三种输入方式。键盘 XAML 编写方式可参考嵌入键盘 [numpad.xaml](MexKeypad/Resources/Raw/numpad.xaml)、[misc.xaml](MexKeypad/Resources/Raw/misc.xaml) 和解析逻辑 [KeyInfo.cs](MexShared/KeyInfo.cs)。如需投入生产环境，建议对内容进行检查或过滤，防止出现安全问题。

协议本身支持 TCP/UDP，但没有安全传输（如加密和鉴权）的功能，建议仅在本地进行连接。推荐的应用场景：局域网内将手机作为电脑的扩展键盘，临时测试非常见按键的功能等。如需投入生产环境用于公共网络，建议自行添加包装层以保证数据安全和进行权限控制。

#### 支持的按键类型

当前的按键类型完全是 [Win32 SendInput](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-sendinput) 函数使用的 [INPUT](https://learn.microsoft.com/windows/win32/api/winuser/ns-winuser-input) 结构体的简单抽象：

- `u:<Unicode码点|Unicode字符>`：表示一个 UTF-16 字符。超出 BMP 的字符需要使用代理对表示（由于分隔符是分号，以及优先解析数字，所以分号和单个数字需要使用码点模式）；
- `vk:<虚拟按键数值|虚拟按键名称>`：表示一个虚拟按键；
- `sc:<扫描码数值>`：表示一个扫描码按键；
- `vksc:<虚拟按键数值|虚拟按键名称>:<扫描码数值>`：表示一个带有扫描码的虚拟按键；
- `m:<鼠标按键>[:<扩展信息>]`：表示一个鼠标按键（扩展信息可选，用于 X 按钮）；

### `MexForwarder`：端口转发

一个 `MexKeypad` 协议的轻量 UDP 端口转发工具，支持跨平台部署。不适用于通用用途，不提供安全传输，可能存在漏洞。用于测试和内部使用，不建议在生产环境部署。

### `MexServer`：纯服务端

远程键盘服务端，只支持 Windows。

只接受 UDP 协议；除了常规直连协议外，也支持 `MexForwarder` 提供的 `forward` 和 `reverse` 协议。
