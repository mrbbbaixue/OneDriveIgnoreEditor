using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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

            LoadRegistryEntries();
        }

        private void DeleteRule(object parameter)
        {
            if (parameter is IgnoreRuleItem item)
            {
                IgnoreRules.Remove(item);
                isModified = true;
                StatusTextBlock.Text = "已修改";
            }
        }

        private void AddEmptyIfNeeded()
        {
            if (IgnoreRules.Count == 0 || !string.IsNullOrWhiteSpace(IgnoreRules[^1].Rule))
            {
                var newItem = new IgnoreRuleItem();
                newItem.PropertyChanged += (s, e) => AddEmptyIfNeeded();
                IgnoreRules.Add(newItem);
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

                AddEmptyIfNeeded();
                isModified = false;
                StatusTextBlock.Text = "已加载注册表项";
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取注册表失败: " + ex.Message);
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
                StatusTextBlock.Text = "已保存更改";
            }
            catch (Exception ex)
            {
                MessageBox.Show("写入注册表失败: " + ex.Message);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (isModified)
            {
                var result = MessageBox.Show("有未保存的更改，是否刷新会丢弃它们？", "确认刷新", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;
            }
            LoadRegistryEntries();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择规则文件",
                Filter = "文本文件 (*.txt)|*.txt"
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
                StatusTextBlock.Text = "已导入规则";
            }
        }

        private void RestartOneDriveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查是否有未保存的更改
                if (isModified)
                {
                    var result = MessageBox.Show("有未保存的更改，重启OneDrive前是否保存？", "确认保存", MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes)
                    {
                        SaveButton_Click(sender, e);
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                StatusTextBlock.Text = "正在重启OneDrive...";

                try
                {
                    // 使用新的低权限重启方法
                    App.RestartOneDrive();
                    StatusTextBlock.Text = "OneDrive已重启";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"重启OneDrive失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusTextBlock.Text = "重启OneDrive失败";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重启OneDrive时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusTextBlock.Text = "重启OneDrive失败";
            }
        }

        private void IgnoreDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // 延迟执行以确保编辑值被更新
            Dispatcher.InvokeAsync(() =>
            {
                AddEmptyIfNeeded();
                isModified = true;
                StatusTextBlock.Text = "已修改";
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
}