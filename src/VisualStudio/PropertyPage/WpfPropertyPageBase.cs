using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Microsoft.VisualStudio.Shell.Interop;
using WfControl = System.Windows.Forms.Control;

namespace NuGet.VisualStudio.PropertyPage
{
    public abstract class WpfPropertyPageBase<TViewModel> : PropertyPageBase where TViewModel : PropertyPageViewModel
    {
        private TViewModel _vm;
        private UIElement _view;
        private ElementHost _host;

        protected TViewModel ViewModel
        {
            get { return _vm ?? (_vm = CreateViewModel()); }
        }

        protected UIElement View
        {
            get { return _view ?? (_view = CreateView()); }
        }

        public override bool IsDirty
        {
            get { return ViewModel.IsDirty; }
        }

        public override void Activate(IntPtr hWndParent, Microsoft.VisualStudio.OLE.Interop.RECT[] pRect, int bModal)
        {
            base.Activate(hWndParent, pRect, bModal);

            // Initialize the View model
            IView<TViewModel> theView = View as IView<TViewModel>;
            if (theView != null)
            {
                theView.SetViewModel(ViewModel);
            }

            // Create an element host
            WfControl parent = WfControl.FromHandle(hWndParent);
            _host = new ElementHost();
            _host.Dock = DockStyle.Fill;
            _host.Parent = parent;
            _host.Child = View;
        }

        public override void Apply()
        {
            base.Apply();
            ViewModel.ApplyChanges();
        }

        public override void Deactivate()
        {
            base.Deactivate();
            ViewModel.OnDeactivated();

            _host.Dispose();
        }

        public override void Help(string pszHelpDir)
        {
            base.Help(pszHelpDir);
            ViewModel.ShowHelp();
        }

        public override void SetObjects(uint cObjects, object[] ppunk)
        {
            base.SetObjects(cObjects, ppunk);

            if (ppunk != null)
            {
                // There will be one object per configuration we're editing (Debug|Any CPU, Release|x64, etc.)
                IVsHierarchy hier = null;
                string[] configs = new string[ppunk.Length];
                for (int i = 0; i < ppunk.Length; i++)
                {
                    IVsCfg cfg = null;
                    IVsCfgBrowseObject cfgbrowse = ppunk[i] as IVsCfgBrowseObject;
                    if (cfgbrowse != null)
                    {
                        uint _;

                        // Only one hierarchy
                        if (hier == null)
                        {
                            cfgbrowse.GetProjectItem(out hier, out _);
                        }
                        cfgbrowse.GetCfg(out cfg);
                    }
                    if (cfg != null)
                    {
                        cfg.get_DisplayName(out configs[i]);
                    }
                }
                ViewModel.RefreshConfigurations(hier, configs);
            }
        }

        public override void SetPageSite(Microsoft.VisualStudio.OLE.Interop.IPropertyPageSite pPageSite)
        {
            base.SetPageSite(pPageSite);
        }

        public override void Show(uint nCmdShow)
        {
            base.Show(nCmdShow);
        }

        public override int TranslateAccelerator(Microsoft.VisualStudio.OLE.Interop.MSG[] pMsg)
        {
            return base.TranslateAccelerator(pMsg);
        }

        protected virtual TViewModel CreateViewModel()
        {
            return ServiceLocator.GetInstance<TViewModel>();
        }

        protected abstract UIElement CreateView();
    }

    public abstract class WpfPropertyPageBase<TViewModel, TView> : WpfPropertyPageBase<TViewModel> 
        where TViewModel : PropertyPageViewModel 
        where TView : UIElement, new()
    {
        protected override UIElement CreateView()
        {
            return new TView();
        }
    }
}
