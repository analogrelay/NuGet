using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace NuGet.VisualStudio.UI
{
    internal class RelayCommand : ICommand
    {
        private Action<object> _executeHandler;
        private Func<object, bool> _canExecuteHandler;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action executeHandler) : this(_ => executeHandler(), (Func<object, bool>)null) { }
        public RelayCommand(Action<object> executeHandler) : this(executeHandler, (Func<object, bool>)null) { }
        public RelayCommand(Action executeHandler, Func<object, bool> canExecuteHandler) : this(_ => executeHandler(), canExecuteHandler) { }
        public RelayCommand(Action<object> executeHandler, Func<bool> canExecuteHandler) : this(executeHandler, _ => canExecuteHandler()) { }
        public RelayCommand(Action executeHandler, Func<bool> canExecuteHandler) : this(_ => executeHandler(), _ => canExecuteHandler()) { }
        public RelayCommand(Action<object> executeHandler, Func<object, bool> canExecuteHandler)
        {
            _executeHandler = executeHandler;
            _canExecuteHandler = canExecuteHandler;
        }

        public void FireCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecuteHandler == null ? true : _canExecuteHandler(parameter);
        }

        public void Execute(object parameter)
        {
            if (_executeHandler != null)
            {
                _executeHandler(parameter);
            }
        }
    }
}
