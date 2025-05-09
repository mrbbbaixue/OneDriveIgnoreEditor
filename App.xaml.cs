using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Input;

namespace OneDriveIgnoreEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
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