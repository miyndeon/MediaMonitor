using System.Windows.Input;

namespace MediaMonitor.Helpers;

class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    // Для команд без условия
    public RelayCommand(Action execute)
        : this(_ => execute(), null) { }

    // Для команд с условием
    public RelayCommand(Action execute, Func<bool> canExecute)
        : this(_ => execute(), _ => canExecute()) { }

    //public RelayCommand(Action execute, Func<bool>? canExecute = null)
    //: this(
    //    _ => execute(),
    //    canExecute == null ? null : _ => canExecute())
    //{ }


    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
}
