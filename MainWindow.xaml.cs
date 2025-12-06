using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace OneDriveIgnoreEditor
{
    public partial class MainWindow : Window
    {
        private const string RegistryPath = @"SOFTWARE\Policies\Microsoft\OneDrive\EnableODIgnoreListFromGPO";

        // No changes needed in this file unless there is sorting logic implemented.
        // If sorting logic exists, it should be reviewed and removed if necessary.
        public ObservableCollection<IgnoreRuleItem> IgnoreRules { get; set; }

        public ICommand DeleteCommand { get; set; }

        private bool isModified = false;

        public MainWindow()
        {
            InitializeComponent();
            DeleteCommand = new RelayCommand(DeleteRule);

            IgnoreRules = [];
            DataContext = this;

            // 应用翻译
            ApplyTranslations();

            LoadRegistryEntries();
        }

        /// <summary>
        /// 应用翻译到UI控件
        /// </summary>
        private void ApplyTranslations()
        {
            // 窗口标题
            this.Title = TranslationService.GetText("WindowTitle");

            // 主标题
            if (MainTitleTextBlock != null)
                MainTitleTextBlock.Text = TranslationService.GetText("MainTitle");

            // 按钮文本
            if (AddButton != null)
                AddButton.Content = TranslationService.GetText("AddButtonText");
            if (SaveButton != null)
                SaveButton.Content = TranslationService.GetText("SaveButtonText");
            if (RefreshButton != null)
                RefreshButton.Content = TranslationService.GetText("RefreshButtonText");
            if (ImportButton != null)
                ImportButton.Content = TranslationService.GetText("ImportButtonText");
            if (RestartOneDriveButton != null)
                RestartOneDriveButton.Content = TranslationService.GetText("RestartOneDriveButtonText");

            // DataGrid列标题
            if (IgnoreDataGrid != null && IgnoreDataGrid.Columns.Count >= 2)
            {
                IgnoreDataGrid.Columns[0].Header = TranslationService.GetText("RuleColumnHeader");
                IgnoreDataGrid.Columns[1].Header = TranslationService.GetText("ActionColumnHeader");
            }

            // 状态栏初始文本
            if (StatusTextBlock != null)
                StatusTextBlock.Text = TranslationService.GetText("ReadyStatus");

            // 更新删除按钮文本（在DataTemplate中）
            UpdateDeleteButtonText();
        }

        private void UpdateDeleteButtonText()
        {
            if (IgnoreDataGrid == null || IgnoreDataGrid.Columns.Count < 2 || !(IgnoreDataGrid.Columns[1] is DataGridTemplateColumn templateColumn))
                return;

            // 从资源获取转换器
            var converter = this.Resources["StringNotEmptyToVisibilityConverter"] as StringNotEmptyToVisibilityConverter;
            if (converter == null) return;

            // 创建新的DataTemplate
            var dataTemplate = new DataTemplate();

            // 创建Button的FrameworkElementFactory
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetBinding(Button.CommandProperty, new Binding("DataContext.DeleteCommand")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1)
            });
            buttonFactory.SetBinding(Button.CommandParameterProperty, new Binding("."));
            buttonFactory.SetValue(Button.ContentProperty, TranslationService.GetText("DeleteButtonText"));
            buttonFactory.SetBinding(Button.VisibilityProperty, new Binding("Rule")
            {
                Converter = converter
            });

            dataTemplate.VisualTree = buttonFactory;
            templateColumn.CellTemplate = dataTemplate;
        }

        private void DeleteRule(object parameter)
        {
            if (parameter is IgnoreRuleItem item)
            {
                IgnoreRules.Remove(item);
                isModified = true;
                StatusTextBlock.Text = TranslationService.GetText("ModifiedStatus");
            }
        }

        private void LoadRegistryEntries()
        {
            IgnoreRules.Clear();
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegistryPath, false))
                {
                    if (key != null)
                    {
                        foreach (string name in key.GetValueNames())
                        {
                            string value = key.GetValue(name).ToString();
                            IgnoreRules.Add(new IgnoreRuleItem { Rule = value });
                        }
                    }
                }

                isModified = false;
                StatusTextBlock.Text = TranslationService.GetText("RegistryLoadedStatus");
            }
            catch (Exception ex)
            {
                MessageBox.Show(TranslationService.GetFormattedText("ReadRegistryFailed", ex.Message));
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(RegistryPath))
                {
                    foreach (string name in key.GetValueNames())
                        key.DeleteValue(name);

                    int count = 1;
                    foreach (var item in IgnoreRules)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Rule))
                        {
                            key.SetValue(count.ToString(), item.Rule, RegistryValueKind.String);
                            count++;
                        }
                    }
                }

                isModified = false;
                StatusTextBlock.Text = TranslationService.GetText("ChangesSavedStatus");
            }
            catch (Exception ex)
            {
                MessageBox.Show(TranslationService.GetFormattedText("WriteRegistryFailed", ex.Message));
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (isModified)
            {
                var result = MessageBox.Show(TranslationService.GetText("UnsavedChangesRefreshPrompt"), TranslationService.GetText("ConfirmRefreshTitle"), MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;
            }
            LoadRegistryEntries();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = TranslationService.GetText("SelectRuleFileTitle"),
                Filter = TranslationService.GetText("TextFilesFilter")
            };

            if (dialog.ShowDialog() == true)
            {
                var lines = File.ReadAllLines(dialog.FileName);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        IgnoreRules.Insert(IgnoreRules.Count - 1, new IgnoreRuleItem { Rule = line.Trim() });
                }

                isModified = true;
                StatusTextBlock.Text = TranslationService.GetText("RulesImportedStatus");
            }
        }

        private void RestartOneDriveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否有未保存的更改
                if (isModified)
                {
                    var result = MessageBox.Show(TranslationService.GetText("UnsavedChangesRestartPrompt"), TranslationService.GetText("ConfirmSaveTitle"), MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes)
                    {
                        SaveButton_Click(sender, e);
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                StatusTextBlock.Text = TranslationService.GetText("RestartingOneDriveStatus");

                try
                {
                    // 使用新的低权限重启方法
                    App.RestartOneDrive();
                    StatusTextBlock.Text = TranslationService.GetText("OneDriveRestartedStatus");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(TranslationService.GetFormattedText("OneDriveRestartFailed", ex.Message), TranslationService.GetText("ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusTextBlock.Text = TranslationService.GetText("OneDriveRestartFailedStatus");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(TranslationService.GetFormattedText("RestartOneDriveError", ex.Message), TranslationService.GetText("ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = TranslationService.GetText("OneDriveRestartFailedStatus");
            }
        }

        private void AddNewButton_Click(object sender, RoutedEventArgs e)
        {
            // 添加新的空规则项
            var newItem = new IgnoreRuleItem { Rule = string.Empty };
            IgnoreRules.Add(newItem);

            // 设置修改标志
            isModified = true;
            StatusTextBlock.Text = TranslationService.GetText("NewRuleAddedStatus");

            // 可选：滚动到新项并开始编辑
            // 延迟执行以确保UI已更新
            Dispatcher.InvokeAsync(() =>
            {
                // 选择新添加的项
                IgnoreDataGrid.SelectedItem = newItem;
                IgnoreDataGrid.ScrollIntoView(newItem);

                // 尝试开始编辑（可能需要用户手动点击单元格）
                // 或者可以设置焦点到DataGrid
                IgnoreDataGrid.Focus();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // 获取鼠标点击位置
            Point mousePosition = e.GetPosition(this);

            // 进行命中测试
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(this, mousePosition);
            if (hitTestResult != null)
            {
                // 检查命中点是否在编辑区域内（单元格、行、列标题等）
                DependencyObject hitObject = hitTestResult.VisualHit;
                bool isClickInsideEditingArea = false;

                // 向上遍历视觉树，检查是否是编辑相关元素
                while (hitObject != null)
                {
                    // 如果是DataGridCell、DataGridRow、DataGridColumnHeader等编辑相关元素，视为在编辑区域内
                    if (hitObject is DataGridCell ||
                        hitObject is DataGridRow ||
                        hitObject is DataGridColumnHeader ||
                        hitObject is TextBox) // 编辑控件
                    {
                        isClickInsideEditingArea = true;
                        break;
                    }

                    // 如果是DataGrid本身（背景区域），不在编辑区域内
                    if (hitObject is DataGrid)
                    {
                        // 直接点击DataGrid背景，不在编辑区域内
                        isClickInsideEditingArea = false;
                        break;
                    }

                    hitObject = VisualTreeHelper.GetParent(hitObject);
                }

                // 如果点击不在编辑区域内，尝试取消DataGrid的编辑
                if (!isClickInsideEditingArea)
                {
                    // 尝试取消编辑
                    IgnoreDataGrid.CancelEdit();
                }
            }
        }

        private void IgnoreDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // 如果用户取消编辑，不进行处理
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            // 获取编辑后的值
            if (e.EditingElement is TextBox textBox)
            {
                string newValue = textBox.Text?.Trim() ?? string.Empty;

                // 如果编辑后的值为空，并且该项是新添加的行（规则为空）
                if (string.IsNullOrWhiteSpace(newValue) && e.Row.Item is IgnoreRuleItem item)
                {
                    // 延迟执行以确保UI更新完成
                    Dispatcher.InvokeAsync(() =>
                    {
                        // 检查规则是否仍然为空（用户可能没有输入任何内容）
                        if (string.IsNullOrWhiteSpace(item.Rule))
                        {
                            // 从集合中移除空项
                            IgnoreRules.Remove(item);
                            isModified = true;
                            StatusTextBlock.Text = TranslationService.GetText("EmptyRuleRemovedStatus");
                        }
                        else
                        {
                            isModified = true;
                            StatusTextBlock.Text = TranslationService.GetText("ModifiedStatus");
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);
                    return;
                }
            }

            // 延迟执行以确保编辑值被更新
            Dispatcher.InvokeAsync(() =>
            {
                isModified = true;
                StatusTextBlock.Text = TranslationService.GetText("ModifiedStatus");
            });
        }
    }

    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value as string;
            return !string.IsNullOrWhiteSpace(str) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 简单的硬编码翻译服务
    /// </summary>
    public static class TranslationService
    {
        /// <summary>
        /// 语言偏好设置
        /// </summary>
        public enum LanguagePreference
        {
            /// <summary>
            /// 自动检测系统语言
            /// </summary>
            Auto,
            /// <summary>
            /// 强制使用中文
            /// </summary>
            Chinese,
            /// <summary>
            /// 强制使用英文
            /// </summary>
            English
        }

        /// <summary>
        /// 获取或设置语言偏好（默认自动检测）
        /// </summary>
        public static LanguagePreference PreferredLanguage { get; set; } = LanguagePreference.Auto;

        private static readonly Dictionary<string, string> ChineseTexts = new Dictionary<string, string>
        {
            // 主窗口标题
            {"WindowTitle", "OneDrive忽略列表编辑器"},
            {"MainTitle", "OneDrive文件忽略列表"},

            // DataGrid列标题
            {"RuleColumnHeader", "规则"},
            {"ActionColumnHeader", "操作"},
            {"DeleteButtonText", "删除"},

            // 按钮文本
            {"AddButtonText", "新增"},
            {"SaveButtonText", "保存"},
            {"RefreshButtonText", "刷新"},
            {"ImportButtonText", "从文件导入"},
            {"RestartOneDriveButtonText", "重启OneDrive"},

            // 状态文本
            {"ReadyStatus", "准备就绪"},
            {"ModifiedStatus", "已修改"},
            {"RegistryLoadedStatus", "已加载注册表项"},
            {"ChangesSavedStatus", "已保存更改"},
            {"RulesImportedStatus", "已导入规则"},
            {"RestartingOneDriveStatus", "正在重启OneDrive..."},
            {"OneDriveRestartedStatus", "OneDrive已重启"},
            {"OneDriveRestartFailedStatus", "重启OneDrive失败"},
            {"OneDriveRestartFailed", "重启OneDrive失败: {0}"},
            {"NewRuleAddedStatus", "已添加新规则项"},
            {"EmptyRuleRemovedStatus", "已移除空项"},

            // 对话框文本
            {"ReadRegistryFailed", "读取注册表失败: {0}"},
            {"WriteRegistryFailed", "写入注册表失败: {0}"},
            {"UnsavedChangesRefreshPrompt", "有未保存的更改，是否刷新会丢弃它们？"},
            {"ConfirmRefreshTitle", "确认刷新"},
            {"UnsavedChangesRestartPrompt", "有未保存的更改，重启OneDrive前是否保存？"},
            {"ConfirmSaveTitle", "确认保存"},
            {"ErrorTitle", "错误"},
            {"SelectRuleFileTitle", "选择规则文件"},
            {"TextFilesFilter", "文本文件 (*.txt)|*.txt"},

            // App.xaml.cs中的文本
            {"CannotDetermineExePath", "无法确定当前可执行文件路径。"},
            {"AdminRightsRequired", "需要管理员权限来运行此程序。"},
            {"OneDriveStartFailed", "启动OneDrive失败: {0}"},
            {"OneDriveExeNotFound", "无法找到OneDrive可执行文件"},
            {"RestartOneDriveError", "重启OneDrive时发生错误: {0}"}
        };

        private static readonly Dictionary<string, string> EnglishTexts = new Dictionary<string, string>
        {
            // 主窗口标题
            {"WindowTitle", "OneDrive Ignore List Editor"},
            {"MainTitle", "OneDrive File Ignore List"},

            // DataGrid列标题
            {"RuleColumnHeader", "Rule"},
            {"ActionColumnHeader", "Action"},
            {"DeleteButtonText", "Delete"},

            // 按钮文本
            {"AddButtonText", "Add"},
            {"SaveButtonText", "Save"},
            {"RefreshButtonText", "Refresh"},
            {"ImportButtonText", "Import from File"},
            {"RestartOneDriveButtonText", "Restart OneDrive"},

            // 状态文本
            {"ReadyStatus", "Ready"},
            {"ModifiedStatus", "Modified"},
            {"RegistryLoadedStatus", "Registry entries loaded"},
            {"ChangesSavedStatus", "Changes saved"},
            {"RulesImportedStatus", "Rules imported"},
            {"RestartingOneDriveStatus", "Restarting OneDrive..."},
            {"OneDriveRestartedStatus", "OneDrive restarted"},
            {"OneDriveRestartFailedStatus", "OneDrive restart failed"},
            {"OneDriveRestartFailed", "OneDrive restart failed: {0}"},
            {"NewRuleAddedStatus", "New rule item added"},
            {"EmptyRuleRemovedStatus", "Empty item removed"},

            // 对话框文本
            {"ReadRegistryFailed", "Failed to read registry"},
            {"WriteRegistryFailed", "Failed to write to registry: {0}"},
            {"UnsavedChangesRefreshPrompt", "There are unsaved changes. Refresh will discard them. Continue?"},
            {"ConfirmRefreshTitle", "Confirm Refresh"},
            {"UnsavedChangesRestartPrompt", "There are unsaved changes. Save before restarting OneDrive?"},
            {"ConfirmSaveTitle", "Confirm Save"},
            {"ErrorTitle", "Error"},
            {"SelectRuleFileTitle", "Select Rule File"},
            {"TextFilesFilter", "Text files (*.txt)|*.txt"},

            // App.xaml.cs中的文本
            {"CannotDetermineExePath", "Cannot determine current executable path."},
            {"AdminRightsRequired", "Administrator privileges are required to run this program."},
            {"OneDriveStartFailed", "Failed to start OneDrive: {0}"},
            {"OneDriveExeNotFound", "Unable to find OneDrive executable"},
            {"RestartOneDriveError", "Error occurred while restarting OneDrive: {0}"}
        };

        /// <summary>
        /// 获取当前系统语言是否为中文（考虑语言偏好设置）
        /// </summary>
        public static bool IsChinese
        {
            get
            {
                switch (PreferredLanguage)
                {
                    case LanguagePreference.Chinese:
                        return true;
                    case LanguagePreference.English:
                        return false;
                    case LanguagePreference.Auto:
                    default:
                        return CultureInfo.CurrentCulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ||
                               CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        /// <summary>
        /// 获取翻译文本
        /// </summary>
        public static string GetText(string key)
        {
            if (IsChinese)
            {
                return ChineseTexts.TryGetValue(key, out var chineseText) ? chineseText : key;
            }
            else
            {
                return EnglishTexts.TryGetValue(key, out var englishText) ? englishText : key;
            }
        }

        /// <summary>
        /// 获取格式化文本（支持字符串插值）
        /// </summary>
        public static string GetFormattedText(string key, params object[] args)
        {
            var format = GetText(key);
            return string.Format(format, args);
        }
    }
}