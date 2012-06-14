using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;

namespace NuGet.VisualStudio.PropertyPage
{
    public abstract class PropertyPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual bool IsDirty { get; set; }

        public virtual void ApplyChanges() { }
        public virtual void OnDeactivated() { }
        public virtual void ShowHelp() { }
        public virtual void RefreshConfigurations(IVsHierarchy hierarchy, string[] configurations) { }

        protected void SetProperty<T>(ref T backingField, T value, string name)
        {
            if (!Equals(backingField, value))
            {
                backingField = value;
                OnPropertyChanged(new PropertyChangedEventArgs(name));
            }
        }

        private void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }
    }
}
