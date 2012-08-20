using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio.Xaml;
using NuGet.VisualStudio.UI;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio.PropertyPage
{
    [Export]
    public class ProjectPropertyPageViewModel : PropertyPageViewModel
    {
        private bool _createPackage;
        private string _configurationNames;

        public string ConfigurationNames
        {
            get { return _configurationNames; }
            set { SetProperty(ref _configurationNames, value, () => ConfigurationNames); }
        }

        public bool CreatePackage
        {
            get { return _createPackage; }
            set
            {
                SetProperty(ref _createPackage, value, () => CreatePackage);
            }
        }

        // Easier to have a negation and color mapping here than in the view...
        public bool BuildServicesNotInstalled { get { return !BuildServicesInstalled; } }
        public bool BuildServicesInstalled
        {
            get { return BuildServicesManager.AreBuildServicesInstalledForSolution; }
        }

        // Commands
        public ICommand InstallBuildServices { get; private set; }

        // Services
        public IBuildServicesManager BuildServicesManager { get; private set; }
        public IUIService UIService { get; private set; }

        [ImportingConstructor]
        public ProjectPropertyPageViewModel(
            IBuildServicesManager buildServicesManager,
            IUIService uiService)
        {
            BuildServicesManager = buildServicesManager;
            UIService = uiService;

            InstallBuildServices = new RelayCommand(OnInstallBuildServices, () => BuildServicesNotInstalled);
        }

        private void OnInstallBuildServices()
        {
            bool? result = MessageHelper.ShowQueryMessage(
                    VsResources.InstallBuildServicesConfirmation,
                    VsResources.DialogTitle,
                    showCancelButton: false);
            if (result == true)
            {
                using (UIService.ShowWaitDialog(VsResources.DialogTitle, VsResources.InstallBuildServicesWaitMessage))
                {
                    BuildServicesManager.EnsureBuildServicesInstalledInSolution(fromActivation: true);
                }
            }
        }
        
        public override void RefreshConfigurations(Microsoft.VisualStudio.Shell.Interop.IVsHierarchy hierarchy, string[] configurations)
        {
            base.RefreshConfigurations(hierarchy, configurations);

            ConfigurationNames = String.Join(";", configurations);
        }
    }
}
