using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfAppCLient.Commands.Base
{
    internal class Command:ICommand
    {
        private readonly Action<object> _Execute;
        private readonly Predicate<object> _CanExecute;


        public Command(Action<object> execute, Predicate<object> canExecute = null)
        {
            _Execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _CanExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }


        public bool CanExecute(object parameter) => _CanExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _Execute(parameter);
    }
}
