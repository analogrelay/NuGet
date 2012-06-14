using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;
using NuGet.VisualStudio.PropertyPage;
using NuGet.VisualStudio.Resources;
using System.Windows;
using System.Windows.Controls;

namespace NuGet.VisualStudio
{
    [Guid(GuidList.guidNuGetPropertyPageString)]
    public class ProjectPropertyPage : WpfPropertyPageBase<ProjectPropertyPageViewModel, ProjectPropertyPageView>
    {
        public override string Title
        {
            get { return VsResources.PropertyPage_Title; }
        }
    }
}
