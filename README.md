# OneDrive忽略列表编辑器

一个用于管理OneDrive文件同步忽略列表的图形化工具。通过修改Windows注册表，让OneDrive在同步时忽略指定的文件或文件夹。

[English Version](README.en.md)


## 中文版本

### 📋 项目简介

OneDriveIgnoreEditor是一个轻量级的Windows桌面应用程序，允许用户通过图形界面轻松管理OneDrive的忽略列表。应用程序直接操作Windows注册表中的相关项，无需手动编辑注册表或使用复杂的命令行工具。

### ✨ 主要功能

- **可视化规则管理**：通过简洁的表格界面添加、编辑、删除忽略规则
- **注册表自动读写**：自动读取和写入OneDrive忽略列表注册表项
- **规则导入**：支持从文本文件导入规则列表
- **一键重启OneDrive**：修改后无需手动重启OneDrive进程
- **智能空项处理**：自动清理未输入内容的空规则项
- **现代化UI界面**：基于iNKORE.UI.WPF.Modern库的现代化界面设计
- **多语言支持**：支持中文和英文界面，自动检测系统语言
- **管理员权限管理**：自动请求管理员权限以访问注册表

### 🚀 系统要求

- **操作系统**：Windows 10 版本 18362 或更高版本
- **.NET框架**：.NET 8.0 桌面运行时
- **OneDrive**：已安装并运行Microsoft OneDrive
- **权限要求**：管理员权限（应用程序会自动请求）

### 📦 安装方法

#### 方法一：下载预编译版本
1. 访问 [Releases页面](https://github.com/mrbbbaixue/OneDriveIgnoreEditor/releases)
2. 下载最新版本的 `OneDriveIgnoreEditor.exe`
3. 直接运行即可（首次运行会自动请求管理员权限）

#### 方法二：从源码编译
```bash
# 克隆项目
git clone https://github.com/mrbbbaixue/OneDriveIgnoreEditor.git
cd OneDriveIgnoreEditor

# 还原依赖并编译
dotnet restore
dotnet build -c Release

# 可执行文件位于 bin/Release/net8.0-windows10.0.18362.0/OneDriveIgnoreEditor.exe
```

### 🎯 使用方法

#### 基本操作
1. **启动应用**：运行 `OneDriveIgnoreEditor.exe`，会自动请求管理员权限
2. **查看现有规则**：启动后自动加载当前注册表中的忽略规则
3. **添加新规则**：点击"新增"按钮，在表格中输入要忽略的文件/文件夹路径
   - 支持通配符：如 `*.tmp`、`*.log`
   - 支持文件夹：如 `C:\Temp\`
   - 支持相对路径：如 `Desktop\backup\`
4. **删除规则**：点击规则行右侧的"删除"按钮
5. **保存更改**：点击"保存"按钮将更改写入注册表
6. **重启OneDrive**：点击"重启OneDrive"按钮使更改生效

#### 高级功能
- **从文件导入**：点击"从文件导入"按钮，选择包含忽略规则列表的文本文件（每行一个规则）
- **刷新列表**：点击"刷新"按钮重新从注册表加载规则列表
- **修改检测**：编辑规则后状态栏会显示"已修改"提示
- **空项处理**：如果添加新项但未输入内容就离开，会自动移除该空项

### ⚙️ 命令行参数

应用程序支持以下命令行参数指定界面语言：

| 参数 | 描述 | 示例 |
|------|------|------|
| `-en` 或 `--english` | 强制使用英文界面 | `OneDriveIgnoreEditor.exe -en` |
| `-cn`、`--chinese` 或 `-zh` | 强制使用中文界面 | `OneDriveIgnoreEditor.exe -cn` |
| 无参数 | 根据系统语言自动选择 | `OneDriveIgnoreEditor.exe` |

**注意**：多个参数时，最后一个语言参数生效。例如 `OneDriveIgnoreEditor.exe -en -cn` 会使用中文界面。

