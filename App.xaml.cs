using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;

namespace OneDriveIgnoreEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 正常启动应用程序
            if (!IsRunAsAdmin())
            {
                RelaunchAsAdmin();
                Current.Shutdown();
                return;
            }
        }

        private static bool IsRunAsAdmin()
        {
            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);
            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RelaunchAsAdmin()
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe))
            {
                MessageBox.Show("无法确定当前可执行文件路径。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var startInfo = new ProcessStartInfo(exe)
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            try { Process.Start(startInfo); }
            catch { MessageBox.Show("需要管理员权限来运行此程序。"); }
        }

        /// <summary>
        /// 通过适当的权限重启OneDrive
        /// </summary>
        public static void RestartOneDrive()
        {
            try
            {
                // 关闭所有OneDrive进程
                foreach (var process in Process.GetProcessesByName("OneDrive"))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(3000); // 等待进程完全退出，最多等待3秒
                    }
                    catch (Exception)
                    {
                        // 忽略任何杀进程时的错误，继续
                    }
                }

                // 查找OneDrive可执行文件路径
                var possiblePaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive", "OneDrive.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft OneDrive", "OneDrive.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft OneDrive", "OneDrive.exe")
                };

                bool launched = false;
                Exception? lastException = null;

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        try
                        {
                            Microsoft.NodejsTools.SharedProject.SystemUtility.ExecuteProcessUnElevated(path, string.Empty);
                            launched = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            lastException = ex;
                        }
                    }
                }

                if (!launched)
                {
                    if (lastException != null)
                        MessageBox.Show($"启动OneDrive失败: {lastException.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        MessageBox.Show("无法找到OneDrive可执行文件", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重启OneDrive时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class RelayCommand(Action<object?> execute) : ICommand
    {
        private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);
    }

    public class IgnoreRuleItem : INotifyPropertyChanged
    {
        private string _rule = string.Empty;

        public string Rule
        {
            get => _rule;
            set
            {
                if (_rule != value)
                {
                    _rule = value;
                    OnPropertyChanged(nameof(Rule));
                }
            }
        }

        // Fix for CS8612 and CS8618:
        // 1. Mark the PropertyChanged event as nullable to match the nullability of the interface.
        // 2. Initialize the event to null to satisfy the non-nullable requirement.
        public event PropertyChangedEventHandler? PropertyChanged = null;

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

// Copied from
// https://github.com/microsoft/nodejstools/blob/main/Nodejs/Product/Nodejs/SharedProject/SystemUtilities.cs
// Modified by lanyi
// Original license: https://github.com/microsoft/nodejstools/blob/main/LICENSE

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.

namespace Microsoft.NodejsTools.SharedProject
{
    /// <summary>
    /// Utility for accessing window IShell* interfaces in order to use them to launch a process unelevated
    /// </summary>
    internal class SystemUtility
    {
        /// <summary>
        /// We are elevated and should launch the process unelevated. We can't create the
        /// process directly without it becoming elevated. So to workaround this, we have
        /// explorer do the process creation (explorer is typically running unelevated).
        /// </summary>
        internal static void ExecuteProcessUnElevated(string process, string args, string currentDirectory = "")
        {
            var info = new ProcessStartInfo
            {
                FileName = process,
                Arguments = args,
                WorkingDirectory = currentDirectory,
                Verb = string.Empty
            };
            ShellExecuteUnElevated(info);
        }

        public static void ShellExecuteUnElevated(ProcessStartInfo info)
        {
            var shellWindows = (IShellWindows)new CShellWindows();

            // Get the desktop window
            object loc = CSIDL_Desktop;
            object unused = new object();
            int hwnd;
            var serviceProvider = (IServiceProvider)shellWindows.FindWindowSW(ref loc, ref unused, SWC_DESKTOP, out hwnd, SWFO_NEEDDISPATCH);

            // Get the shell browser
            var serviceGuid = SID_STopLevelBrowser;
            var interfaceGuid = typeof(IShellBrowser).GUID;
            var shellBrowser = (IShellBrowser)serviceProvider.QueryService(ref serviceGuid, ref interfaceGuid);

            // Get the shell dispatch
            var dispatch = typeof(IDispatch).GUID;
            var folderView = (IShellFolderViewDual)shellBrowser.QueryActiveShellView().GetItemObject(SVGIO_BACKGROUND, ref dispatch);
            var shellDispatch = (IShellDispatch2)folderView.Application;

            // Use the dispatch (which is unelevated) to launch the process for us
            shellDispatch.ShellExecute(info.FileName, info.Arguments, info.WorkingDirectory, info.Verb, SW_SHOWNORMAL);
        }

        /// <summary>
        /// Interop definitions
        /// </summary>
        private const int CSIDL_Desktop = 0;

        private const int SWC_DESKTOP = 8;
        private const int SWFO_NEEDDISPATCH = 1;
        private const int SW_SHOWNORMAL = 1;
        private const int SVGIO_BACKGROUND = 0;
        private static readonly Guid SID_STopLevelBrowser = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");

        [ComImport]
        [Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39")]
        [ClassInterfaceAttribute(ClassInterfaceType.None)]
        private class CShellWindows
        {
        }

        [ComImport]
        [Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IShellWindows
        {
            [return: MarshalAs(UnmanagedType.IDispatch)]
            object FindWindowSW([MarshalAs(UnmanagedType.Struct)] ref object pvarloc, [MarshalAs(UnmanagedType.Struct)] ref object pvarlocRoot, int swClass, out int pHWND, int swfwOptions);
        }

        [ComImport]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            object QueryService(ref Guid guidService, ref Guid riid);
        }

        [ComImport]
        [Guid("000214E2-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellBrowser
        {
            void VTableGap01(); // GetWindow

            void VTableGap02(); // ContextSensitiveHelp

            void VTableGap03(); // InsertMenusSB

            void VTableGap04(); // SetMenuSB

            void VTableGap05(); // RemoveMenusSB

            void VTableGap06(); // SetStatusTextSB

            void VTableGap07(); // EnableModelessSB

            void VTableGap08(); // TranslateAcceleratorSB

            void VTableGap09(); // BrowseObject

            void VTableGap10(); // GetViewStateStream

            void VTableGap11(); // GetControlWindow

            void VTableGap12(); // SendControlMsg

            IShellView QueryActiveShellView();
        }

        [ComImport]
        [Guid("000214E3-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellView
        {
            void VTableGap01(); // GetWindow

            void VTableGap02(); // ContextSensitiveHelp

            void VTableGap03(); // TranslateAcceleratorA

            void VTableGap04(); // EnableModeless

            void VTableGap05(); // UIActivate

            void VTableGap06(); // Refresh

            void VTableGap07(); // CreateViewWindow

            void VTableGap08(); // DestroyViewWindow

            void VTableGap09(); // GetCurrentInfo

            void VTableGap10(); // AddPropertySheetPages

            void VTableGap11(); // SaveViewState

            void VTableGap12(); // SelectItem

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetItemObject(UInt32 aspectOfView, ref Guid riid);
        }

        [ComImport]
        [Guid("00020400-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IDispatch
        {
        }

        [ComImport]
        [Guid("E7A1AF80-4D96-11CF-960C-0080C7F4EE85")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        private interface IShellFolderViewDual
        {
            object Application { [return: MarshalAs(UnmanagedType.IDispatch)] get; }
        }

        [ComImport]
        [Guid("A4C6892C-3BA9-11D2-9DEA-00C04FB16162")]
        [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        public interface IShellDispatch2
        {
            void ShellExecute([MarshalAs(UnmanagedType.BStr)] string File, [MarshalAs(UnmanagedType.Struct)] object vArgs, [MarshalAs(UnmanagedType.Struct)] object vDir, [MarshalAs(UnmanagedType.Struct)] object vOperation, [MarshalAs(UnmanagedType.Struct)] object vShow);
        }
    }
}