using System;
using System.Windows.Input;

namespace TreeViewFileExplorer.Model;

/// <summary>
/// A command whose sole purpose is to relay its functionality to other objects.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Predicate<object> _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    public RelayCommand(Action<object> execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic.</param>
    public RelayCommand(Action<object> execute, Predicate<object> canExecute)
    {
        if (execute == null)
            throw new ArgumentNullException(nameof(execute));
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc/>
    public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <inheritdoc/>
    public void Execute(object parameter) => _execute(parameter);

    /// <inheritdoc/>
    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}