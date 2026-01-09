using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ShadowverseEvolveCardTracker.ViewModels
{
    // Small async-capable RelayCommand for simple MVVM scenarios.
    public interface IRelayCommand
    {
        void RaiseCanExecuteChanged();
    }

    public sealed class RelayCommand : ICommand, IRelayCommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object? parameter) => await _execute().ConfigureAwait(false);

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    // Generic version that accepts a parameter of type T
    public sealed class RelayCommand<T> : ICommand, IRelayCommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            if (parameter is T t) return _canExecute(t);
            // allow null parameter cast when T is nullable or reference
            return _canExecute(default);
        }

        public async void Execute(object? parameter)
        {
            T? value = parameter is T t ? t : default;
            await _execute(value).ConfigureAwait(false);
        }

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}