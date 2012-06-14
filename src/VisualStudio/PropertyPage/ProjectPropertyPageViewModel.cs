using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace NuGet.VisualStudio.PropertyPage
{
    [Export]
    public class ProjectPropertyPageViewModel : PropertyPageViewModel
    {
        private string _configurationNames;

        public string ConfigurationNames
        {
            get { return _configurationNames; }
            set { SetProperty(ref _configurationNames, value, "ConfigurationNames"); }
        }

        public ProjectPropertyPageViewModel()
        {
            
        }

        public override void RefreshConfigurations(Microsoft.VisualStudio.Shell.Interop.IVsHierarchy hierarchy, string[] configurations)
        {
            base.RefreshConfigurations(hierarchy, configurations);

            ConfigurationNames = String.Join(";", configurations);
        }
    }
}
