using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuGet.VisualStudio.UI
{
    public interface IUIService
    {
        IDisposable ShowWaitDialog(string title, string message);
    }
}
