using System;
using System.Windows.Input;

namespace SaciGestao.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _executar;
        private readonly Func<object?, bool>? _podeExecutar;

        public RelayCommand(Action<object?> executar, Func<object?, bool>? podeExecutar = null)
        {
            _executar = executar ?? throw new ArgumentNullException(nameof(executar));
            _podeExecutar = podeExecutar;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parametro)
        {
            return _podeExecutar == null || _podeExecutar(parametro);
        }

        public void Execute(object? parametro)
        {
            _executar(parametro);
        }
    }
}