# MY Port Assistant

MY Port Assistant 是一个面向嵌入式调试场景的 Windows 串口助手。

## 技术栈

- C#
- .NET 8
- WPF

## v0.1.0 功能

- 串口列表扫描与刷新
- 波特率、数据位、停止位、校验位配置
- 打开和关闭串口
- 接收区显示
- ASCII 和 HEX 显示切换
- 文本发送和 HEX 发送
- 清空接收区
- 接收字节数统计
- 发送字节数统计
- 当前串口状态显示

## 本地环境

本项目优先使用 E 盘环境：

```powershell
$env:DOTNET_CLI_HOME='E:\DevTools\dotnet-home'
$env:NUGET_PACKAGES='E:\NuGetPackages'
E:\DevTools\dotnet\dotnet.exe build .\src\MYPortAssistant\MYPortAssistant.csproj
```

## 目录结构

```text
MY_port/
  README.md
  LICENSE
  .gitignore
  NuGet.Config
  docs/
    updates/
  src/
    MYPortAssistant/
  assets/
  scripts/
```

## 更新记录

每次更新都会在 `docs/updates/` 下新增一个独立 MD 文件，不覆盖旧记录。
