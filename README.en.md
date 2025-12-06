# OneDrive Ignore List Editor

A graphical tool for managing OneDrive file synchronization ignore lists. By modifying the Windows Registry, it allows OneDrive to ignore specified files or folders during synchronization.

[中文版本](README.md)

### 📋 Project Introduction

OneDriveIgnoreEditor is a lightweight Windows desktop application that allows users to easily manage OneDrive's ignore list through a graphical interface. The application directly manipulates relevant entries in the Windows Registry, eliminating the need for manual registry editing or complex command-line tools.

### ✨ Key Features

- **Visual Rule Management**: Add, edit, and delete ignore rules through a clean table interface
- **Automatic Registry Operations**: Automatically reads and writes OneDrive ignore list registry entries
- **Rule Import**: Supports importing rules from text files
- **One-Click OneDrive Restart**: No need to manually restart OneDrive process after changes
- **Smart Empty Item Handling**: Automatically cleans up empty rule items with no content
- **Modern UI Interface**: Modern design based on iNKORE.UI.WPF.Modern library
- **Multi-language Support**: Supports Chinese and English interfaces with automatic system language detection
- **Administrator Privilege Management**: Automatically requests administrator privileges for registry access

### 🚀 System Requirements

- **Operating System**: Windows 10 version 18362 or higher
- **.NET Framework**: .NET 8.0 Desktop Runtime
- **OneDrive**: Microsoft OneDrive installed and running
- **Privilege Requirements**: Administrator privileges (automatically requested by the application)

### 📦 Installation Methods

#### Method 1: Download Pre-compiled Version
1. Visit the [Releases page](https://github.com/mrbbbaixue/OneDriveIgnoreEditor/releases)
2. Download the latest version of `OneDriveIgnoreEditor.exe`
3. Run directly (administrator privileges will be automatically requested on first run)

#### Method 2: Compile from Source
```bash
# Clone the project
git clone https://github.com/mrbbbaixue/OneDriveIgnoreEditor.git
cd OneDriveIgnoreEditor

# Restore dependencies and compile
dotnet restore
dotnet build -c Release

# The executable is located at bin/Release/net8.0-windows10.0.18362.0/OneDriveIgnoreEditor.exe
```

### 🎯 Usage

#### Basic Operations
1. **Launch Application**: Run `OneDriveIgnoreEditor.exe`, administrator privileges will be automatically requested
2. **View Existing Rules**: Automatically loads current ignore rules from registry on startup
3. **Add New Rules**: Click the "Add" button, enter the file/folder path to ignore in the table
   - Supports wildcards: e.g., `*.tmp`, `*.log`
   - Supports folders: e.g., `C:\Temp\`
   - Supports relative paths: e.g., `Desktop\backup\`
4. **Delete Rules**: Click the "Delete" button on the right side of the rule row
5. **Save Changes**: Click the "Save" button to write changes to the registry
6. **Restart OneDrive**: Click the "Restart OneDrive" button to apply changes

#### Advanced Features
- **Import from File**: Click the "Import from File" button, select a text file containing ignore rule list (one rule per line)
- **Refresh List**: Click the "Refresh" button to reload rules from registry
- **Modification Detection**: Status bar shows "Modified" prompt after editing rules
- **Empty Item Handling**: If a new item is added but left empty, it will be automatically removed

### ⚙️ Command Line Arguments

The application supports the following command line arguments to specify interface language:

| Argument | Description | Example |
|----------|-------------|---------|
| `-en` or `--english` | Force English interface | `OneDriveIgnoreEditor.exe -en` |
| `-cn`, `--chinese` or `-zh` | Force Chinese interface | `OneDriveIgnoreEditor.exe -cn` |
| No arguments | Automatically select based on system language | `OneDriveIgnoreEditor.exe` |

**Note**: When multiple arguments are provided, the last language argument takes effect. For example, `OneDriveIgnoreEditor.exe -en -cn` will use Chinese interface.